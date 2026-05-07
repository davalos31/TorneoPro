using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Notificacion;

namespace TorneoPro.API.Controllers
{
    [Route("api/notificaciones")]
    [ApiController]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        #region ========== CAMPOS Y CONSTRUCTOR ==========

        private readonly INotificacionService _notificacionService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<NotificacionesController> _logger;

        public NotificacionesController(
            INotificacionService notificacionService,
            IAuditoriaService auditoriaService,
            ILogger<NotificacionesController> logger)
        {
            _notificacionService = notificacionService;
            _auditoriaService = auditoriaService;
            _logger = logger;
        }

        #endregion

        #region ========== CONSULTA DE NOTIFICACIONES ==========

        /// <summary>
        /// Obtiene una lista paginada de las notificaciones del usuario autenticado
        /// </summary>
        /// <param name="solicitud">Filtros: leída, archivada, tipo, prioridad, rango de fechas</param>
        /// <returns>Lista paginada de notificaciones</returns>
        [HttpGet("mis-notificaciones")]
        public async Task<IActionResult> ObtenerMisNotificaciones([FromQuery] FiltrarNotificacionRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de notificaciones - UsuarioId: {UsuarioId}, Pagina: {Pagina}, IP: {Ip}",
                usuarioId, solicitud.Pagina, ipCliente);

            try
            {
                var resultado = await _notificacionService.ObtenerMisNotificacionesAsync(usuarioId, solicitud);

                _logger.LogInformation("Notificaciones consultadas - UsuarioId: {UsuarioId}, Total: {Total}",
                    usuarioId, resultado.TotalItems);

                return Ok(ApiRespuesta<ResultadoPaginado<NotificacionResponse>>.Success(resultado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener notificaciones - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener notificaciones"));
            }
        }

        /// <summary>
        /// Obtiene el detalle de una notificación específica
        /// </summary>
        /// <param name="id">ID de la notificación</param>
        /// <returns>Detalle de la notificación</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerNotificacion(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de notificación - NotificacionId: {NotificacionId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var notificacion = await _notificacionService.ObtenerNotificacionPorIdAsync(id, usuarioId);

                if (notificacion == null)
                {
                    _logger.LogWarning("Notificación no encontrada - NotificacionId: {NotificacionId}, UsuarioId: {UsuarioId}",
                        id, usuarioId);
                    return NotFound(ApiRespuesta<object>.Error("Notificación no encontrada"));
                }

                return Ok(ApiRespuesta<NotificacionResponse>.Success(notificacion));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener notificación - NotificacionId: {NotificacionId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener notificación"));
            }
        }

        #endregion

        #region ========== GESTIÓN DE NOTIFICACIONES ==========

        /// <summary>
        /// Marca una notificación como leída
        /// </summary>
        /// <param name="id">ID de la notificación</param>
        /// <returns>Confirmación de la operación</returns>
        [HttpPut("{id}/leer")]
        public async Task<IActionResult> MarcarComoLeida(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Marcar notificación como leída - NotificacionId: {NotificacionId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                await _notificacionService.MarcarComoLeidaAsync(id, usuarioId);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "MARCAR_NOTIFICACION_LEIDA",
                    "notificaciones",
                    id);

                _logger.LogInformation("Notificación marcada como leída - NotificacionId: {NotificacionId}", id);
                return Ok(ApiRespuesta<object>.Success(null, "Notificación marcada como leída"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Notificación no encontrada - NotificacionId: {NotificacionId}", id);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar notificación como leída - NotificacionId: {NotificacionId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al marcar notificación como leída"));
            }
        }

        /// <summary>
        /// Marca todas las notificaciones del usuario como leídas
        /// </summary>
        /// <returns>Confirmación de la operación</returns>
        [HttpPut("leer-todas")]
        public async Task<IActionResult> MarcarTodasComoLeidas()
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Marcar todas las notificaciones como leídas - UsuarioId: {UsuarioId}, IP: {Ip}",
                usuarioId, ipCliente);

            try
            {
                await _notificacionService.MarcarTodasComoLeidasAsync(usuarioId);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "MARCAR_TODAS_NOTIFICACIONES_LEIDAS",
                    "notificaciones",
                    null);

                _logger.LogInformation("Todas las notificaciones marcadas como leídas - UsuarioId: {UsuarioId}", usuarioId);
                return Ok(ApiRespuesta<object>.Success(null, "Todas las notificaciones marcadas como leídas"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar todas las notificaciones como leídas - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al marcar notificaciones"));
            }
        }

        /// <summary>
        /// Archiva una notificación (la oculta de la lista principal)
        /// </summary>
        /// <param name="id">ID de la notificación</param>
        /// <returns>Confirmación de la operación</returns>
        [HttpDelete("{id}/archivar")]
        public async Task<IActionResult> ArchivarNotificacion(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Archivar notificación - NotificacionId: {NotificacionId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                await _notificacionService.ArchivarNotificacionAsync(id, usuarioId);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "ARCHIVAR_NOTIFICACION",
                    "notificaciones",
                    id);

                _logger.LogInformation("Notificación archivada - NotificacionId: {NotificacionId}", id);
                return Ok(ApiRespuesta<object>.Success(null, "Notificación archivada"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Notificación no encontrada - NotificacionId: {NotificacionId}", id);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al archivar notificación - NotificacionId: {NotificacionId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al archivar notificación"));
            }
        }

        #endregion

        #region ========== ENVÍO DE NOTIFICACIONES ==========

        /// <summary>
        /// Envía una notificación individual a un usuario específico
        /// </summary>
        /// <param name="solicitud">Datos de la notificación</param>
        /// <returns>Notificación enviada</returns>
        [HttpPost("enviar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> EnviarNotificacion([FromBody] EnviarNotificacionRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Envío de notificación individual - Origen: {OrigenId}, Destino: {DestinoId}, Titulo: {Titulo}",
                usuarioId, solicitud.IdUsuarioDestino, solicitud.Titulo);

            try
            {
                var resultado = await _notificacionService.EnviarNotificacionAsync(usuarioId, solicitud);

                await _auditoriaService.RegistrarAsync(
                    usuarioId,
                    "ENVIAR_NOTIFICACION",
                    "notificaciones",
                    resultado.Id,
                    null,
                    $"Destino: {solicitud.IdUsuarioDestino}, Título: {solicitud.Titulo}",
                    ipCliente,
                    userAgent,
                    true);

                _logger.LogInformation("Notificación enviada - NotificacionId: {NotificacionId}, Destino: {Destino}",
                    resultado.Id, solicitud.IdUsuarioDestino);

                return Ok(ApiRespuesta<NotificacionResponse>.Success(resultado, "Notificación enviada exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioId,
                    "ENVIAR_NOTIFICACION",
                    ex.Message,
                    "notificaciones",
                    null,
                    $"Destino: {solicitud.IdUsuarioDestino}",
                    ipCliente,
                    userAgent);

                _logger.LogWarning("Error al enviar notificación - Motivo: {Motivo}", ex.Message);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioId,
                    "ENVIAR_NOTIFICACION",
                    ex.Message,
                    "notificaciones",
                    null,
                    $"Destino: {solicitud.IdUsuarioDestino}",
                    ipCliente,
                    userAgent);

                _logger.LogError(ex, "Error al enviar notificación - Destino: {Destino}", solicitud.IdUsuarioDestino);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al enviar notificación"));
            }
        }

        /// <summary>
        /// Envía una notificación masiva a múltiples usuarios
        /// </summary>
        /// <param name="solicitud">Configuración de la notificación masiva</param>
        /// <returns>Resumen del envío</returns>
        [HttpPost("enviar-masiva")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> EnviarNotificacionMasiva([FromBody] NotificacionMasivaRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Envío de notificación masiva - Origen: {OrigenId}, Tipo: {TipoId}, Titulo: {Titulo}",
                usuarioId, solicitud.IdTipoNotificacion, solicitud.Titulo);

            try
            {
                var cantidadEnviadas = await _notificacionService.EnviarNotificacionMasivaAsync(usuarioId, solicitud);

                await _auditoriaService.RegistrarAsync(
                    usuarioId,
                    "ENVIAR_NOTIFICACION_MASIVA",
                    "notificaciones",
                    null,
                    null,
                    $"Cantidad: {cantidadEnviadas}, TipoNotificacion: {solicitud.IdTipoNotificacion}",
                    ipCliente,
                    userAgent,
                    true);

                _logger.LogInformation("Notificación masiva enviada - Cantidad: {Cantidad}, Origen: {OrigenId}",
                    cantidadEnviadas, usuarioId);

                return Ok(ApiRespuesta<object>.Success(new { CantidadEnviadas = cantidadEnviadas },
                    $"Notificación masiva enviada a {cantidadEnviadas} usuarios"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioId,
                    "ENVIAR_NOTIFICACION_MASIVA",
                    ex.Message,
                    "notificaciones",
                    null,
                    $"TipoNotificacion: {solicitud.IdTipoNotificacion}",
                    ipCliente,
                    userAgent);

                _logger.LogError(ex, "Error al enviar notificación masiva");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al enviar notificación masiva"));
            }
        }

        #endregion

        #region ========== PREFERENCIAS DE NOTIFICACIÓN ==========

        /// <summary>
        /// Obtiene las preferencias de notificación del usuario autenticado
        /// </summary>
        /// <returns>Lista de preferencias por tipo de notificación</returns>
        [HttpGet("preferencias")]
        public async Task<IActionResult> ObtenerPreferencias()
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de preferencias - UsuarioId: {UsuarioId}, IP: {Ip}", usuarioId, ipCliente);

            try
            {
                var preferencias = await _notificacionService.ObtenerPreferenciasAsync(usuarioId);
                return Ok(ApiRespuesta<List<PreferenciaNotificacionResponse>>.Success(preferencias));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener preferencias - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener preferencias"));
            }
        }

        /// <summary>
        /// Actualiza las preferencias de notificación para un tipo específico
        /// </summary>
        /// <param name="idTipoNotificacion">ID del tipo de notificación</param>
        /// <param name="solicitud">Nuevas preferencias</param>
        /// <returns>Confirmación de la actualización</returns>
        [HttpPut("preferencias/{idTipoNotificacion}")]
        public async Task<IActionResult> ActualizarPreferencia(int idTipoNotificacion, [FromBody] ActualizarPreferenciaRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Actualización de preferencias - UsuarioId: {UsuarioId}, TipoNotificacionId: {TipoId}",
                usuarioId, idTipoNotificacion);

            try
            {
                var preferenciasAnteriores = await _notificacionService.ObtenerPreferenciasPorTipoAsync(usuarioId, idTipoNotificacion);

                var datosAnteriores = preferenciasAnteriores != null ? new
                {
                    Activado = preferenciasAnteriores.Activado,
                    PushActivado = preferenciasAnteriores.PushActivado,
                    EmailActivado = preferenciasAnteriores.EmailActivado,
                    SmsActivado = preferenciasAnteriores.SmsActivado
                } : null;

                await _notificacionService.ActualizarPreferenciasAsync(usuarioId, idTipoNotificacion, solicitud);

                await _auditoriaService.RegistrarAsync(
                    usuarioId,
                    "ACTUALIZAR_PREFERENCIA_NOTIFICACION",
                    "preferencias_notificacion",
                    idTipoNotificacion,
                    JsonSerializer.Serialize(datosAnteriores),
                    JsonSerializer.Serialize(new
                    {
                        Activado = solicitud.Activado,
                        PushActivado = solicitud.PushActivado,
                        EmailActivado = solicitud.EmailActivado,
                        SmsActivado = solicitud.SmsActivado
                    }),
                    ipCliente,
                    userAgent,
                    true);

                _logger.LogInformation("Preferencias actualizadas - UsuarioId: {UsuarioId}, TipoNotificacion: {TipoId}",
                    usuarioId, idTipoNotificacion);

                return Ok(ApiRespuesta<object>.Success(null, "Preferencias actualizadas exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioId,
                    "ACTUALIZAR_PREFERENCIA_NOTIFICACION",
                    ex.Message,
                    "preferencias_notificacion",
                    idTipoNotificacion,
                    null,
                    ipCliente,
                    userAgent);

                _logger.LogWarning(ex, "Preferencia no encontrada - TipoNotificacion: {TipoId}", idTipoNotificacion);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioId,
                    "ACTUALIZAR_PREFERENCIA_NOTIFICACION",
                    ex.Message,
                    "preferencias_notificacion",
                    idTipoNotificacion,
                    null,
                    ipCliente,
                    userAgent);

                _logger.LogError(ex, "Error al actualizar preferencias - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar preferencias"));
            }
        }

        #endregion

        #region ========== ESTADÍSTICAS ==========

        /// <summary>
        /// Obtiene estadísticas de notificaciones del usuario autenticado
        /// </summary>
        /// <returns>Estadísticas: total, no leídas, archivadas, últimas 24h, por prioridad, por tipo</returns>
        [HttpGet("estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticas()
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de estadísticas - UsuarioId: {UsuarioId}, IP: {Ip}", usuarioId, ipCliente);

            try
            {
                var estadisticas = await _notificacionService.ObtenerEstadisticasAsync(usuarioId);
                return Ok(ApiRespuesta<NotificacionEstadisticasResponse>.Success(estadisticas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener estadísticas"));
            }
        }

        #endregion

        #region ========== PROCESOS AUTOMÁTICOS ==========

        /// <summary>
        /// Procesa manualmente las notificaciones pendientes (job programado)
        /// </summary>
        /// <returns>Resultado del procesamiento</returns>
        [HttpPost("procesar-pendientes")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ProcesarNotificacionesPendientes()
        {
            var usuarioId = ObtenerUsuarioActualId();

            _logger.LogInformation("Procesamiento manual de notificaciones pendientes - UsuarioId: {UsuarioId}", usuarioId);

            try
            {
                await _notificacionService.ProcesarNotificacionesPendientesAsync();

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "PROCESAR_NOTIFICACIONES_PENDIENTES",
                    "notificaciones",
                    null);

                return Ok(ApiRespuesta<object>.Success(null, "Notificaciones pendientes procesadas exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar notificaciones pendientes");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al procesar notificaciones pendientes"));
            }
        }

        #endregion

        #region ========== MÉTODOS PRIVADOS ==========

        private int ObtenerUsuarioActualId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Usuario no identificado");
            return id;
        }

        private string ObtenerIpCliente()
        {
            var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return string.IsNullOrEmpty(ip) ? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "IP desconocida" : ip;
        }

        private string ObtenerUserAgent() => Request.Headers["User-Agent"].FirstOrDefault() ?? "desconocido";

        #endregion
    }
}