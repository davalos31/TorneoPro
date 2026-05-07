using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorneoPro.API.DTOs.Jugadores;
using TorneoPro.API.DTOs.Jugadores.Request;
using TorneoPro.API.DTOs.Jugadores.Response;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Jugador;
using TorneoPro.API.Servicios.Interfaces.Notificacion;

namespace TorneoPro.API.Controllers
{
    /// <summary>
    /// Controlador para la gestión integral de jugadores, incluyendo CRUD, 
    /// estadísticas, invitaciones, suspensiones y transferencias.
    /// </summary>
    [Route("api/jugadores")]
    [ApiController]
    [Authorize]
    public class JugadoresController : ControllerBase
    {
        private readonly IJugadorService _jugadorService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<JugadoresController> _logger;
        private readonly INotificacionService _notificacionService;

        public JugadoresController(
            IJugadorService jugadorService,
            IAuditoriaService auditoriaService,
            ILogger<JugadoresController> logger,
            INotificacionService notificacionService)
        {
            _jugadorService = jugadorService;
            _auditoriaService = auditoriaService;
            _logger = logger;
            _notificacionService = notificacionService;
        }

        #region ========== CRUD PRINCIPAL ==========
        /// <summary>
        /// Obtiene una lista paginada de jugadores según los filtros proporcionados.
        /// </summary>
        /// <param name="solicitud">Filtros de búsqueda y parámetros de paginación.</param>
        /// <returns>Resultado paginado con la información de los jugadores.</returns>
        /// <response code="200">Retorna la lista de jugadores.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Permisos insuficientes (Requiere SUPER_ADMIN, ADMIN o SUB_ADMIN).</response>
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,SUB_ADMIN")]
        public async Task<IActionResult> Listar([FromQuery] FiltrarJugadorRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Listado de jugadores - UsuarioId: {UsuarioId}, Pagina: {Pagina}, IP: {Ip}",
                usuarioId, solicitud.Pagina, ipCliente);

            try
            {
                var resultado = await _jugadorService.ObtenerTodosAsync(solicitud);
                return Ok(ApiRespuesta<ResultadoPaginado<JugadorResponse>>.Success(resultado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar jugadores");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la lista de jugadores"));
            }
        }

        /// <summary>
        /// Obtiene el perfil detallado de un jugador por su identificador único.
        /// </summary>
        /// <param name="id">ID del jugador.</param>
        /// <returns>Detalles del perfil del jugador.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de jugador - JugadorId: {JugadorId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var jugador = await _jugadorService.ObtenerPorIdAsync(id);
                if (jugador == null)
                    return NotFound(ApiRespuesta<object>.Error("Jugador no encontrado"));

                return Ok(ApiRespuesta<JugadorResponse>.Success(jugador));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener jugador - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener el jugador"));
            }
        }

        /// <summary>
        /// Actualiza la información del perfil de un jugador.
        /// </summary>
        /// <param name="id">ID del jugador a actualizar.</param>
        /// <param name="solicitud">Nuevos datos del jugador.</param>
        /// <returns>Información del jugador actualizada.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarJugador(int id, [FromBody] ActualizarJugadorRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Actualización de jugador - JugadorId: {JugadorId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var resultado = await _jugadorService.ActualizarJugadorAsync(id, usuarioId, solicitud, ipCliente, userAgent);

                if (usuarioId != id)
                {
                    await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
                    {
                        IdUsuarioDestino = id,
                        IdTipoNotificacion = 15,
                        Titulo = "Tu perfil fue actualizado",
                        Mensaje = "Un administrador ha actualizado la información de tu perfil.",
                        Prioridad = "BAJA"
                    });
                }

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "ACTUALIZAR_JUGADOR", "usuarios", id);
                _logger.LogInformation("Jugador actualizado exitosamente - JugadorId: {JugadorId}", id);

                return Ok(ApiRespuesta<JugadorResponse>.Success(resultado, "Jugador actualizado exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar jugador - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar el jugador"));
            }
        }

        #endregion

        #region ========== INVITACIONES ==========
        /// <summary>
        /// Envía una invitación por correo electrónico para que un jugador se una a un equipo.
        /// </summary>
        /// <param name="solicitud">Datos del destinatario y equipo de destino.</param>
        /// <returns>Resultado del envío de la invitación.</returns>
        [HttpPost("invitar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> InvitarJugador([FromBody] InvitarJugadorRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Invitación a jugador - AdminId: {AdminId}, Email: {Email}, EquipoId: {EquipoId}",
                usuarioId, solicitud.Email, solicitud.IdEquipo);

            try
            {
                var resultado = await _jugadorService.InvitarJugadorAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId, "INVITAR_JUGADOR", "equipos", solicitud.IdEquipo,
                    System.Text.Json.JsonSerializer.Serialize(new { solicitud.Email, solicitud.IdEquipo }));

                _logger.LogInformation("Invitación enviada - Email: {Email}, EquipoId: {EquipoId}", solicitud.Email, solicitud.IdEquipo);

                return Ok(ApiRespuesta<InvitarJugadorResponse>.Success(resultado, resultado.Mensaje));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al invitar jugador - Email: {Email}", solicitud.Email);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al invitar al jugador"));
            }
        }
        /// <summary>
        /// Procesa la aceptación de una invitación mediante un token de seguridad.
        /// </summary>
        /// <param name="token">Token único de la invitación.</param>
        /// <returns>Retorna el perfil del jugador o una página HTML de éxito.</returns>
        [HttpGet("aceptar-invitacion")]
        public async Task<IActionResult> AceptarInvitacion([FromQuery] string token)
        {
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Aceptación de invitación - Token: {Token}, IP: {Ip}", token, ipCliente);

            try
            {
                var usuarioId = ObtenerUsuarioActualId();
                var resultado = await _jugadorService.AceptarInvitacionAsync(token, usuarioId, ipCliente, userAgent);

                await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
                {
                    IdUsuarioDestino = usuarioId,
                    IdTipoNotificacion = 6,
                    Titulo = "¡Te has unido al equipo!",
                    Mensaje = $"Ahora eres parte de {resultado.Equipos?.LastOrDefault()?.Equipo ?? "tu nuevo equipo"}. ¡Bienvenido!",
                    Prioridad = "MEDIA"
                });

                if (Request.Headers["Accept"].ToString().Contains("application/json") &&
                    !Request.Headers["Accept"].ToString().Contains("text/html"))
                {
                    return Ok(ApiRespuesta<JugadorResponse>.Success(resultado, "Te has unido al equipo exitosamente"));
                }

                var equipo = resultado.Equipos?.LastOrDefault();
                var nombreEquipo = equipo?.Equipo ?? "el equipo";
                var html = ViewHelper.GenerarPaginaExitoEquipo(nombreEquipo, resultado.NombreCompleto);

                return Content(html, "text/html");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aceptar invitación - Token: {Token}", token);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al procesar la invitación"));
            }
        }

        #endregion

        #region ========== ESTADÍSTICAS ==========
        /// <summary>
        /// Obtiene el resumen de estadísticas generales de un jugador, opcionalmente filtrado por torneo.
        /// </summary>
        /// <param name="id">ID del jugador.</param>
        /// <param name="idTorneo">ID opcional del torneo.</param>
        [HttpGet("{id}/estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticas(int id, [FromQuery] int? idTorneo = null)
        {
            var usuarioId = ObtenerUsuarioActualId();

            try
            {
                var estadisticas = await _jugadorService.ObtenerEstadisticasAsync(id, idTorneo);
                return Ok(ApiRespuesta<EstadisticasJugadorResumen>.Success(estadisticas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener estadísticas"));
            }
        }

        /// <summary>
        /// Obtiene un desglose de estadísticas del jugador por cada torneo participado.
        /// </summary>
        [HttpGet("{id}/estadisticas-por-torneo")]
        public async Task<IActionResult> ObtenerEstadisticasPorTorneo(int id)
        {
            try
            {
                var estadisticas = await _jugadorService.ObtenerEstadisticasPorTorneoAsync(id);
                return Ok(ApiRespuesta<List<EstadisticasPorTorneoResponse>>.Success(estadisticas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas por torneo - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener estadísticas"));
            }
        }
        /// <summary>
        /// Obtiene métricas de rendimiento avanzadas del jugador.
        /// </summary>
        [HttpGet("{id}/estadisticas-avanzadas")]
        public async Task<IActionResult> ObtenerEstadisticasAvanzadas(int id, [FromQuery] int? idTorneo = null)
        {
            try
            {
                var estadisticas = await _jugadorService.ObtenerEstadisticasAvanzadasAsync(id, idTorneo);
                return Ok(ApiRespuesta<EstadisticasAvanzadasResponse>.Success(estadisticas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas avanzadas - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener estadísticas avanzadas"));
            }
        }

        /// <summary>
        /// Genera un resumen del desempeño del jugador durante un año específico.
        /// </summary>
        [HttpGet("{id}/resumen-temporada")]
        public async Task<IActionResult> ObtenerResumenTemporada(int id, [FromQuery] int anio)
        {
            try
            {
                var resumen = await _jugadorService.ObtenerResumenTemporadaAsync(id, anio);
                return Ok(ApiRespuesta<ResumenTemporadaResponse>.Success(resumen));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de temporada - JugadorId: {JugadorId}, Anio: {Anio}", id, anio);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener resumen de temporada"));
            }
        }

        #endregion

        #region ========== EQUIPOS Y COMPAÑEROS ==========
        /// <summary>
        /// Lista todos los equipos a los que pertenece o ha pertenecido el jugador.
        /// </summary>
        [HttpGet("{id}/equipos")]
        public async Task<IActionResult> ObtenerEquipos(int id)
        {
            try
            {
                var equipos = await _jugadorService.ObtenerEquiposAsync(id);
                return Ok(ApiRespuesta<List<EquipoJugadorResponse>>.Success(equipos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener equipos - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener equipos"));
            }
        }

        /// <summary>
        /// Obtiene la lista de compañeros de equipo para un torneo específico.
        /// </summary>
        [HttpGet("{id}/companeros/{idTorneo}")]
        public async Task<IActionResult> ObtenerCompanerosEquipo(int id, int idTorneo)
        {
            try
            {
                var companeros = await _jugadorService.ObtenerCompanerosEquipoAsync(id, idTorneo);
                return Ok(ApiRespuesta<List<CompañeroEquipoResponse>>.Success(companeros));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener compañeros - JugadorId: {JugadorId}, TorneoId: {TorneoId}", id, idTorneo);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener compañeros"));
            }
        }

        #endregion

        #region ========== PARTIDOS ==========
        /// <summary>
        /// Obtiene el historial de partidos jugados por el usuario.
        /// </summary>
        [HttpGet("{id}/historial-partidos")]
        public async Task<IActionResult> ObtenerHistorialPartidos(int id, [FromQuery] int? idTorneo = null, [FromQuery] int limite = 10)
        {
            try
            {
                var historial = await _jugadorService.ObtenerHistorialPartidosAsync(id, idTorneo, limite);
                return Ok(ApiRespuesta<List<PartidoJugadorResponse>>.Success(historial));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de partidos - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener historial de partidos"));
            }
        }
        /// <summary>
        /// Obtiene la agenda de los próximos encuentros programados para el jugador.
        /// </summary>
        [HttpGet("{id}/proximos-partidos")]
        public async Task<IActionResult> ObtenerProximosPartidos(int id, [FromQuery] int limite = 5)
        {
            try
            {
                var partidos = await _jugadorService.ObtenerProximosPartidosAsync(id, limite);
                return Ok(ApiRespuesta<List<PartidoJugadorResponse>>.Success(partidos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener próximos partidos - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener próximos partidos"));
            }
        }

        #endregion

        #region ========== SUSPENSIONES ==========
        /// <summary>
        /// Registra una sanción disciplinaria para un jugador.
        /// </summary>
        /// <param name="id">ID del jugador.</param>
        /// <param name="request">Motivo y duración de la suspensión.</param>
        [HttpPost("{id}/suspender")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> SuspenderJugador(int id, [FromBody] SuspenderJugadorRequest request)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Suspensión de jugador - JugadorId: {JugadorId}, AdminId: {AdminId}", id, usuarioId);

            try
            {
                await _jugadorService.SuspenderJugadorAsync(id, usuarioId, request.Motivo, request.PartidosSuspension, request.IdTorneo, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId, "SUSPENDER_JUGADOR", "jugadores_suspensiones", id,
                    System.Text.Json.JsonSerializer.Serialize(new { request.Motivo, request.PartidosSuspension }));

                return Ok(ApiRespuesta<object>.Success(null, $"Jugador suspendido por {request.PartidosSuspension} partidos"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al suspender jugador - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al suspender el jugador"));
            }
        }
        /// <summary>
        /// Levanta una suspensión activa de un jugador antes de su cumplimiento total.
        /// </summary>
        [HttpPost("{id}/rehabilitar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> RehabilitarJugador(int id, [FromQuery] string? motivo = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Rehabilitación de jugador - JugadorId: {JugadorId}, AdminId: {AdminId}", id, usuarioId);

            try
            {
                await _jugadorService.RehabilitarJugadorAsync(id, usuarioId, motivo, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "REHABILITAR_JUGADOR", "jugadores_suspensiones", id, motivo);

                return Ok(ApiRespuesta<object>.Success(null, "Jugador rehabilitado exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rehabilitar jugador - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al rehabilitar el jugador"));
            }
        }

        /// <summary>
        /// Consulta el historial de sanciones y suspensiones de un jugador.
        /// </summary>
        [HttpGet("{id}/suspensiones")]
        public async Task<IActionResult> ObtenerSuspensiones(int id)
        {
            try
            {
                var suspensiones = await _jugadorService.ObtenerSuspensionesAsync(id);
                return Ok(ApiRespuesta<List<SuspensionJugadorResponse>>.Success(suspensiones));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener suspensiones - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener suspensiones"));
            }
        }

        #endregion

        #region ========== TRANSFERENCIAS ==========
        /// <summary>
        /// Inicia un proceso de solicitud para cambiar de equipo.
        /// </summary>
        [HttpPost("{id}/solicitar-transferencia")]
        public async Task<IActionResult> SolicitarTransferencia(int id, [FromBody] SolicitarTransferenciaRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            if (usuarioId != id)
                return StatusCode(403, ApiRespuesta<object>.Error("No puedes solicitar transferencia para otro jugador"));

            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                var resultado = await _jugadorService.SolicitarTransferenciaAsync(id, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId, "SOLICITAR_TRANSFERENCIA", "transferencias", resultado.Id,
                    System.Text.Json.JsonSerializer.Serialize(new { solicitud.IdEquipoDestino, solicitud.Motivo }));

                return Ok(ApiRespuesta<SolicitudTransferenciaResponse>.Success(resultado, "Solicitud de transferencia enviada"));
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
                _logger.LogError(ex, "Error al solicitar transferencia - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al solicitar transferencia"));
            }
        }
        /// <summary>
        /// Aprueba o rechaza una solicitud de transferencia pendiente.
        /// </summary>
        [HttpPost("transferencias/{solicitudId}/procesar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,CAPITAN")]
        public async Task<IActionResult> ProcesarTransferencia(int solicitudId, [FromQuery] bool aprobada, [FromQuery] string? comentario = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                await _jugadorService.ProcesarTransferenciaAsync(solicitudId, usuarioId, aprobada, comentario, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId, aprobada ? "APROBAR_TRANSFERENCIA" : "RECHAZAR_TRANSFERENCIA", "transferencias", solicitudId, comentario);

                var mensaje = aprobada ? "Transferencia aprobada exitosamente" : "Transferencia rechazada";
                return Ok(ApiRespuesta<object>.Success(null, mensaje));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar transferencia - SolicitudId: {SolicitudId}", solicitudId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al procesar transferencia"));
            }
        }

        #endregion

        #region ========== CREDENCIALES ==========
        /// <summary>
        /// Genera un documento PDF con la credencial oficial del jugador para un torneo.
        /// </summary>
        [HttpGet("{id}/credencial")]
        public async Task<IActionResult> GenerarCredencial(int id, [FromQuery] int idTorneo)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Generación de credencial - JugadorId: {JugadorId}, TorneoId: {TorneoId}", id, idTorneo);

            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var pdfBytes = await _jugadorService.GenerarCredencialAsync(id, idTorneo, baseUrl);

                return File(pdfBytes, "application/pdf", $"credencial_jugador_{id}.pdf");
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
                _logger.LogError(ex, "Error al generar credencial - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al generar la credencial"));
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

        #endregion
    }
}