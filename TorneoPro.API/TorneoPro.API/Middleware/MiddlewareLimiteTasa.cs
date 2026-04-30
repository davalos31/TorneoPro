using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.Middleware
{
    public class MiddlewareLimiteTasa
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MiddlewareLimiteTasa> _logger;

        private const int DefaultRequestLimit = 100;
        private const int DefaultTimeWindowSeconds = 60;
        private const int DefaultBlockTimeMinutes = 5;

        public MiddlewareLimiteTasa(RequestDelegate next, IMemoryCache cache, ILogger<MiddlewareLimiteTasa> logger)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            if (IsExcludedEndpoint(path))
            {
                await _next(context);
                return;
            }

            var clientId = GetClientIdentifier(context);
            var blockKey = $"blocked_{clientId}";

            // Verificar si el cliente está bloqueado
            if (_cache.TryGetValue(blockKey, out DateTime blockExpiry))
            {
                var remaining = (int)Math.Ceiling((blockExpiry - DateTime.UtcNow).TotalSeconds);
                if (remaining > 0)
                {
                    _logger.LogWarning("Cliente bloqueado {ClientId} intentó acceder a {Path}", clientId, path);
                    await HandleRateLimitExceeded(context, remaining, isBlocked: true);
                    return;
                }
                // El bloqueo expiró — limpiarlo
                _cache.Remove(blockKey);
            }

            var (limit, timeWindow) = GetEndpointConfig(path);
            var rateLimitKey = $"rate_{clientId}_{path}";

            // ✅ Fix: GetOrCreate puede retornar null en versiones recientes de IMemoryCache.
            // Usar patrón seguro con TryGetValue + Set explícito.
            if (!_cache.TryGetValue(rateLimitKey, out RateLimitInfo? info) || info is null)
            {
                info = new RateLimitInfo { Count = 0, FirstRequest = DateTime.UtcNow };
            }

            info.Count++;

            if (info.Count > limit)
            {
                // Bloquear cliente
                var blockExpiration = DateTime.UtcNow.AddMinutes(DefaultBlockTimeMinutes);
                _cache.Set(blockKey, blockExpiration, TimeSpan.FromMinutes(DefaultBlockTimeMinutes));

                _logger.LogWarning(
                    "Rate limit excedido para {ClientId} en {Path}. Solicitudes: {Count}/{Limit}",
                    clientId, path, info.Count, limit);

                var retryAfter = (int)Math.Ceiling((info.FirstRequest.AddSeconds(timeWindow) - DateTime.UtcNow).TotalSeconds);
                await HandleRateLimitExceeded(context, Math.Max(retryAfter, 1), isBlocked: false);
                return;
            }

            // Guardar contador actualizado
            _cache.Set(rateLimitKey, info, TimeSpan.FromSeconds(timeWindow));

            // ✅ Headers estándar de rate limiting (RFC 6585)
            var remaining2 = limit - info.Count;
            var resetTime = ((DateTimeOffset)info.FirstRequest.AddSeconds(timeWindow)).ToUnixTimeSeconds();

            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remaining2.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = resetTime.ToString();

            await _next(context);
        }

        private static string GetClientIdentifier(HttpContext context)
        {
            // Prioridad 1: usuario autenticado (más estable que IP)
            var userId = context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
                return $"user_{userId}";

            // Prioridad 2: IP real (considerando proxies)
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                  ?? context.Connection.RemoteIpAddress?.ToString();

            // ✅ Fix: X-Forwarded-For puede tener múltiples IPs — tomar solo la primera (cliente real)
            if (!string.IsNullOrEmpty(ip))
                return $"ip_{ip.Split(',')[0].Trim()}";

            // Fallback: hash de User-Agent (poco confiable pero mejor que nada)
            return $"agent_{context.Request.Headers.UserAgent.ToString().GetHashCode()}";
        }

        private static (int limit, int timeWindowSeconds) GetEndpointConfig(string path)
        {
            // ⚠️ Estas rutas deben coincidir exactamente con las del AuthController
            if (path.Contains("/auth/iniciar-sesion")) return (5, 60);   // 5 intentos/min
            if (path.Contains("/auth/registrar")) return (3, 300);  // 3 registros/5min
            if (path.Contains("/auth/olvide-contrasena")) return (3, 300);
            if (path.Contains("/auth/restablecer-contrasena")) return (3, 300);
            if (path.Contains("/api/ia")) return (10, 300);  // 10 req IA/5min
            if (path.Contains("/api/reportes")) return (20, 60);
            if (path.Contains("/api/export")) return (20, 60);
            if (path.Contains("/enlaces") &&
                path.Contains("/usar")) return (20, 60);
            if (path.Contains("/partidos") &&
               (path.Contains("/eventos") ||
                path.Contains("/finalizar"))) return (50, 60);

            return (DefaultRequestLimit, DefaultTimeWindowSeconds);
        }

        private static bool IsExcludedEndpoint(string path)
        {
            // ✅ Fix: usar StartsWith consistente — antes mezclaba == y StartsWith
            // causando que "/health?check=1" no matcheara con ==
            return path.StartsWith("/health") ||
                   path.StartsWith("/swagger") ||
                   path.StartsWith("/scalar") ||
                   path.StartsWith("/openapi") ||
                   path == "/favicon.ico" ||
                   path == "/";
        }

        private static async Task HandleRateLimitExceeded(HttpContext context, int secondsRemaining, bool isBlocked)
        {
            if (context.Response.HasStarted) return;

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Retry-After"] = Math.Max(secondsRemaining, 1).ToString();

            var mensaje = isBlocked
                ? $"Demasiados intentos. Bloqueado por {Math.Ceiling(secondsRemaining / 60.0):0} minuto(s)."
                : $"Límite de solicitudes excedido. Espera {secondsRemaining} segundo(s).";

            var response = new ApiRespuesta<object>
            {
                Exitoso = false,
                Mensaje = mensaje,
                Errores = new List<string> { $"Retry-After: {secondsRemaining}s" }
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
        }

        private sealed class RateLimitInfo
        {
            public int Count { get; set; }
            public DateTime FirstRequest { get; set; }
        }
    }
}