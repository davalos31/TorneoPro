using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.Middleware
{
    public class MiddlewareExcepciones
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MiddlewareExcepciones> _logger;

        public MiddlewareExcepciones(RequestDelegate next, ILogger<MiddlewareExcepciones> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción no controlada en {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
   
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("La respuesta ya fue iniciada. No se puede manejar la excepción correctamente.");
                return;
            }

            context.Response.ContentType = "application/json";
            ApiRespuesta<object> response;

            switch (exception)
            {
                case UnauthorizedAccessException unauthorizedEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response = ApiRespuesta<object>.Error(unauthorizedEx.Message ?? "No autorizado");
                    break;

                case KeyNotFoundException notFoundEx:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response = ApiRespuesta<object>.Error(notFoundEx.Message ?? "Recurso no encontrado");
                    break;

                case ArgumentException or ArgumentNullException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = ApiRespuesta<object>.Error(exception.Message);
                    break;

              
                case InvalidOperationException:
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response = ApiRespuesta<object>.Error(exception.Message);
                    break;

                case Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response = ApiRespuesta<object>.Error(
                        "Los datos fueron modificados por otro usuario. Por favor, recargue y vuelva a intentar.");
                    break;

                case Microsoft.EntityFrameworkCore.DbUpdateException dbEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    _logger.LogError(dbEx, "Error de base de datos al guardar cambios");
                    response = ApiRespuesta<object>.Error(
                        "Error al guardar los datos. Verifique que no existan duplicados.");
                    break;

                case OperationCanceledException:
                    context.Response.StatusCode = 499; 
                    _logger.LogInformation("La solicitud fue cancelada por el cliente en {Path}",
                        context.Request.Path);
                    response = ApiRespuesta<object>.Error("La solicitud fue cancelada.");
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response = ApiRespuesta<object>.Error("Ocurrió un error interno en el servidor.");
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}