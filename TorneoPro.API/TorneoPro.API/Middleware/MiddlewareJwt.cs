using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json;
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.Middleware
{
    public class MiddlewareJwt
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MiddlewareJwt> _logger;

        private static readonly HashSet<string> _rutasPublicasExactas = new(StringComparer.OrdinalIgnoreCase)
        {
            // AuthController
            "/api/auth/iniciar-sesion",
            "/api/auth/renovar-token",
            "/api/auth/olvide-contrasena",
            "/api/auth/restablecer-contrasena",
            "/api/auth/verificar-email",

            // ArchivosController
            "/api/archivos/validar-credencial",

            // AccesoTemporalController
            "/api/acceso-temporal/usar",
            "/api/acceso-temporal/info",
            "/api/acceso-temporal/validar-credencial",

            // Health checks y otros
            "/health",
            "/favicon.ico",
            "/"
        };

        private static readonly string[] _prefijosPublicos = new[]
          {
            "/api/torneos",
            "/api/estadisticas",
            "/swagger",
            "/scalar",
            "/openapi",
            "/invite",
            "/api/invite",
            "/login"   // ← AGREGAR
        };

        public MiddlewareJwt(RequestDelegate next, IConfiguration configuration, ILogger<MiddlewareJwt> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsPublicEndpoint(context.Request))
            {
                await _next(context);
                return;
            }

            var token = ExtractToken(context.Request);

            if (string.IsNullOrEmpty(token))
            {
                await HandleUnauthorizedAsync(context, "Token no proporcionado");
                return;
            }

            try
            {
                var principal = ValidateToken(token);

                context.Items["Token"] = token;
                context.Items["UserId"] = principal.FindFirst("sub")?.Value
                                       ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                context.Items["Email"] = principal.FindFirst("email")?.Value
                                       ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
                context.Items["Roles"] = principal.FindFirst("role")?.Value;

                context.User = principal;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token JWT expirado en {Path}", context.Request.Path);
                await HandleUnauthorizedAsync(context, "Token expirado");
                return;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Token JWT inválido: {Message}", ex.Message);
                await HandleUnauthorizedAsync(context, "Token inválido");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al validar token JWT");
                await HandleUnauthorizedAsync(context, "Error al validar autenticación");
                return;
            }

            await _next(context);
        }

        private static string? ExtractToken(HttpRequest request)
        {
            var authHeader = request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authHeader["Bearer ".Length..].Trim();

            if (request.Path.StartsWithSegments("/hubs") &&
                request.Query.TryGetValue("access_token", out var qsToken))
                return qsToken.ToString();

            if (request.Cookies.TryGetValue("access_token", out var cookieToken))
                return cookieToken;

            return null;
        }

        private System.Security.Claims.ClaimsPrincipal ValidateToken(string token)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey no configurada");

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, parameters, out _);
        }

        private static bool IsPublicEndpoint(HttpRequest request)
        {
            var path = request.Path.Value ?? "";

            // Rutas exactas
            if (_rutasPublicasExactas.Contains(path))
                return true;

            // Prefijos públicos
            foreach (var prefijo in _prefijosPublicos)
                if (path.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
                    return true;

            // ✅ GET /api/auth/verificar-email/{token}  ← EL FIX
            if (request.Method == "GET" &&
                path.StartsWith("/api/auth/verificar-email/", StringComparison.OrdinalIgnoreCase))
                return true;


            // GET /api/enlaces/{codigoEnlace}/info
            if (request.Method == "GET" &&
                path.StartsWith("/api/enlaces/", StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith("/info", StringComparison.OrdinalIgnoreCase))
                return true;

            // AccesoTemporalController con token variable
            if (path.StartsWith("/api/acceso-temporal/usar/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/acceso-temporal/info/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/acceso-temporal/validar-credencial/", StringComparison.OrdinalIgnoreCase))
                return true;

            // ArchivosController con token variable
            if (path.StartsWith("/api/archivos/validar-credencial/", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static async Task HandleUnauthorizedAsync(HttpContext context, string mensaje)
        {
            if (context.Response.HasStarted) return;

            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new ApiRespuesta<object>
            {
                Exitoso = false,
                Mensaje = mensaje
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
        }
    }
}