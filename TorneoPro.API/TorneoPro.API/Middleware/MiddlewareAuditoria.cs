using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Middleware
{
    public class MiddlewareAuditoria
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MiddlewareAuditoria> _logger;

        public MiddlewareAuditoria(RequestDelegate next, ILogger<MiddlewareAuditoria> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, TorneoProContext dbContext)
        {
            var request = context.Request;

            // Solo auditar endpoints de API
            if (request.Path.Value?.StartsWith("/api", StringComparison.OrdinalIgnoreCase) != true)
            {
                await _next(context);
                return;
            }

            string? requestBody = null;
            if (IsModifyingMethod(request.Method))
            {
                requestBody = await ReadRequestBodyAsync(request);
            }

            // ✅ Fix: interceptar el body de la respuesta para capturarlo.
            // Se reemplaza response.Body por un MemoryStream temporal,
            // se ejecuta el pipeline, y luego se copia al stream original.
            var originalBodyStream = context.Response.Body;
            await using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            var startTime = DateTime.UtcNow;
            var userId = GetUserId(context);

            try
            {
                await _next(context);
            }
            finally
            {
                // ✅ Fix: restaurar el stream SIEMPRE (en finally), incluso si hubo excepción.
                // Si se restaura solo en el try, un error deja el response.Body roto
                // y el cliente no recibe nada.

                var statusCode = context.Response.StatusCode;
                var responseBody = await ReadResponseBodyAsync(responseBodyStream);

                // Restaurar y enviar respuesta al cliente
                context.Response.Body = originalBodyStream;

                if (responseBodyStream.Length > 0)
                {
                    responseBodyStream.Position = 0;
                    await responseBodyStream.CopyToAsync(originalBodyStream);
                }

                // ✅ Fix: Task.Run con DbContext es PELIGROSO — el DbContext es Scoped
                // y puede ser disposed antes de que Task.Run lo use.
                // Se usa IServiceScopeFactory para crear un scope independiente y seguro.
                // La auditoría no debe bloquear la respuesta al cliente, pero tampoco
                // puede usar el DbContext del request que ya fue liberado.
                _ = LogAuditSafeAsync(context, userId, requestBody, responseBody, startTime, statusCode);
            }
        }

        private async Task LogAuditSafeAsync(
            HttpContext context,
            int? userId,
            string? requestBody,
            string? responseBody,
            DateTime startTime,
            int statusCode)
        {
            // Capturar datos del request ANTES del await (el HttpContext puede ser
            // reciclado por ASP.NET Core después de enviar la respuesta)
            var method = context.Request.Method;
            var path = context.Request.Path;
            var ip = GetClientIp(context);
            var userAgent = context.Request.Headers.UserAgent.ToString();

            try
            {
                // Obtener un scope de DI propio para el DbContext — seguro en background
                var scopeFactory = context.RequestServices.GetRequiredService<IServiceScopeFactory>();
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<TorneoProContext>();

                await LogAuditAsync(db, userId, method, path, requestBody, responseBody,
                    startTime, statusCode, ip, userAgent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar auditoría para {Method} {Path}", method, path);
            }
        }

        private static bool IsModifyingMethod(string method) =>
            method is "POST" or "PUT" or "PATCH" or "DELETE";

        private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            return SanitizeSensitiveData(body);
        }

        private static async Task<string> ReadResponseBodyAsync(MemoryStream responseBodyStream)
        {
            responseBodyStream.Position = 0;
            var body = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Position = 0;
            return body;
        }

        private static int? GetUserId(HttpContext context)
        {
            // Prioridad 1: Claims del User (vía MiddlewareJwt que setea context.User)
            var claim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? context.User?.FindFirst("sub")?.Value;

            if (claim != null && int.TryParse(claim, out var id))
                return id;

            // Prioridad 2: Items del contexto (fallback)
            if (context.Items.TryGetValue("UserId", out var itemId) &&
                itemId is string idStr &&
                int.TryParse(idStr, out var itemIdInt))
                return itemIdInt;

            return null;
        }

        private static string? SanitizeSensitiveData(string? data)
        {
            if (string.IsNullOrWhiteSpace(data)) return data;

            try
            {
                using var jsonDoc = JsonDocument.Parse(data);
                var root = jsonDoc.RootElement;

                if (root.ValueKind != JsonValueKind.Object) return data;

                var sanitized = new Dictionary<string, object?>();
                foreach (var prop in root.EnumerateObject())
                {
                    var name = prop.Name.ToLowerInvariant();
                    sanitized[prop.Name] = name.Contains("password") ||
                                           name.Contains("token") ||
                                           name.Contains("secret") ||
                                           name.Contains("salt")
                        ? "********"
                        : (object?)prop.Value.Clone();
                }
                return JsonSerializer.Serialize(sanitized);
            }
            catch
            {
                return data; // No es JSON — devolver tal cual
            }
        }

        private static async Task LogAuditAsync(
            TorneoProContext dbContext,
            int? userId,
            string method,
            PathString path,
            string? requestBody,
            string? responseBody,
            DateTime startTime,
            int statusCode,
            string ip,
            string userAgent)
        {
            var auditoria = new auditoria_acceso
            {
                id_usuario = userId,
                accion = GetActionName(method, path),
                tabla_afectada = GetAffectedTable(path),
                id_registro = ExtractRecordId(path),
                datos_anteriores = null,
                datos_nuevos = IsModifyingMethod(method) ? requestBody : null,
                ip_address = ip,
                user_agent = userAgent,
                fecha_accion = startTime,
                exitoso = statusCode is >= 200 and < 300,
                detalle_error = statusCode >= 400 ? $"HTTP {statusCode}" : null
            };

            await dbContext.auditoria_accesos.AddAsync(auditoria);
            await dbContext.SaveChangesAsync();
        }

        private static string GetActionName(string method, PathString path)
        {
            var p = path.Value?.ToLowerInvariant() ?? "";
            if (p.Contains("/login")) return "INICIO_SESION";
            if (p.Contains("/logout")) return "CIERRE_SESION";
            if (p.Contains("/refresh-token")) return "REFRESH_TOKEN";

            return method switch
            {
                "GET" => "CONSULTA",
                "POST" => "CREACION",
                "PUT" => "ACTUALIZACION",
                "PATCH" => "ACTUALIZACION_PARCIAL",
                "DELETE" => "ELIMINACION",
                _ => "DESCONOCIDO"
            };
        }

        private static string? GetAffectedTable(PathString path)
        {
            var p = path.Value?.ToLowerInvariant() ?? "";
            if (p.Contains("/auth")) return "usuarios";
            if (p.Contains("/usuarios")) return "usuarios";
            if (p.Contains("/torneos")) return "torneos";
            if (p.Contains("/equipos")) return "equipos";
            if (p.Contains("/canchas")) return "canchas";
            if (p.Contains("/partidos")) return "partidos";
            if (p.Contains("/estadisticas")) return "estadisticas";
            if (p.Contains("/multas")) return "multas";
            if (p.Contains("/notificaciones")) return "notificaciones";
            if (p.Contains("/enlaces")) return "enlaces_compartidos";
            if (p.Contains("/reportes")) return "reportes";
            if (p.Contains("/ia")) return "ia_recomendaciones";
            return null;
        }

        private static int? ExtractRecordId(PathString path)
        {
            var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments == null) return null;

            // Buscar el primer segmento numérico (típicamente el ID del recurso)
            foreach (var segment in segments)
                if (int.TryParse(segment, out var id))
                    return id;

            return null;
        }

        private static string GetClientIp(HttpContext context)
        {
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return string.IsNullOrEmpty(ip)
                ? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                : ip.Split(',')[0].Trim(); // ✅ Fix: X-Forwarded-For puede tener múltiples IPs separadas por coma
        }
    }
}