using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorneoPro.API.DTOs.Jugadores;
using TorneoPro.API.DTOs.Jugadores.Request;
using TorneoPro.API.DTOs.Jugadores.Response;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Jugador;
using TorneoPro.API.Servicios.Interfaces.Notificacion;

namespace TorneoPro.API.Controllers
{
    [Route("api/jugadores")]
    [ApiController]
    [Authorize]
    /// <summary>
    /// Controlador para la gestión integral de jugadores.
    /// Proporciona endpoints para administración (CRUD), invitaciones, suspensiones,
    /// estadísticas y generación de credenciales digitales.
    /// </summary>
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

        /// <summary>
        /// Obtiene una lista paginada de jugadores permitiendo aplicar diversos filtros.
        /// </summary>
        /// <param name="solicitud">Criterios de filtrado y parámetros de paginación.</param>
        /// <returns>Resultado paginado con la lista de jugadores.</returns>
        /// <response code="200">Retorna la lista de jugadores según el filtro.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado (Solo Roles Administrativos).</response>
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
        /// Recupera el perfil detallado de un jugador específico por su ID.
        /// </summary>
        /// <param name="id">ID único del jugador.</param>
        /// <returns>Detalles del perfil del jugador.</returns>
        /// <response code="200">Jugador encontrado correctamente.</response>
        /// <response code="404">El jugador no existe.</response>
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
        /// Actualiza la información de un jugador existente.
        /// </summary>
        /// <remarks>
        /// Si la actualización es realizada por un administrador, se enviará una notificación al jugador.
        /// </remarks>
        /// <param name="id">ID del jugador a modificar.</param>
        /// <param name="solicitud">Nuevos datos para el perfil.</param>
        /// <returns>El perfil actualizado.</returns>
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
                // Solo notificar si fue un admin quien actualizó, no el mismo jugador
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
                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "ACTUALIZAR_JUGADOR",
                    "usuarios",
                    id);

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

        /// <summary>
        /// Envía una invitación por correo electrónico para que un usuario se una como jugador a un equipo.
        /// </summary>
        /// <param name="solicitud">Email del destinatario e ID del equipo.</param>
        /// <returns>Confirmación del envío de la invitación.</returns>
        [HttpPost("invitar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> InvitarJugador([FromBody] InvitarJugadorRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Invitación a jugador - AdministradorId: {AdminId}, Email: {Email}, EquipoId: {EquipoId}, IP: {Ip}",
                usuarioId, solicitud.Email, solicitud.IdEquipo, ipCliente);

            try
            {
                var resultado = await _jugadorService.InvitarJugadorAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "INVITAR_JUGADOR",
                    "equipos",
                    solicitud.IdEquipo,
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
        /// Procesa la aceptación de una invitación a un equipo mediante un token.
        /// </summary>
        /// <remarks>
        /// Este endpoint puede devolver una respuesta JSON para aplicaciones o un HTML de éxito para navegadores web.
        /// </remarks>
        /// <param name="token">Token único de la invitación.</param>
        /// <returns>Perfil del jugador actualizado o vista HTML de confirmación.</returns>
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

                // Si es request de API (tiene Accept: application/json) devolver JSON
                if (Request.Headers["Accept"].ToString().Contains("application/json") &&
                    !Request.Headers["Accept"].ToString().Contains("text/html"))
                {
                    return Ok(ApiRespuesta<JugadorResponse>.Success(resultado, "Te has unido al equipo exitosamente"));
                }

                var equipo = resultado.Equipos?.LastOrDefault();
                var nombreEquipo = equipo?.Equipo ?? "el equipo";

                return Content(GenerarHtmlExito(nombreEquipo, resultado.NombreCompleto), "text/html");
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

        /// <summary>
        /// Obtiene el resumen de rendimiento y estadísticas de un jugador.
        /// </summary>
        /// <param name="id">ID del jugador.</param>
        /// <param name="idTorneo">Opcional. Filtra las estadísticas por un torneo específico.</param>
        /// <returns>Objeto con goles, tarjetas, partidos jugados, etc.</returns>
        [HttpGet("{id}/estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticas(int id, [FromQuery] int? idTorneo = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de estadísticas de jugador - JugadorId: {JugadorId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

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
        /// Lista todos los equipos a los que pertenece o ha pertenecido el jugador.
        /// </summary>
        /// <param name="id">ID del jugador.</param>
        /// <returns>Lista de equipos relacionados.</returns>
        [HttpGet("{id}/equipos")]
        public async Task<IActionResult> ObtenerEquipos(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de equipos de jugador - JugadorId: {JugadorId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

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
        /// Aplica una sanción de suspensión a un jugador, impidiéndole participar en una cantidad determinada de partidos.
        /// </summary>
        /// <param name="id">ID del jugador a suspender.</param>
        /// <param name="request">Detalles de la suspensión (motivo y cantidad de partidos).</param>
        /// <returns>Confirmación de la suspensión aplicada.</returns>
        [HttpPost("{id}/suspender")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> SuspenderJugador(int id, [FromBody] SuspenderJugadorRequest request)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Suspensión de jugador - JugadorId: {JugadorId}, AdministradorId: {AdminId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                await _jugadorService.SuspenderJugadorAsync(id, usuarioId, request.Motivo, request.PartidosSuspension, request.IdTorneo, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "SUSPENDER_JUGADOR",
                    "jugadores_suspensiones",
                    id,
                    System.Text.Json.JsonSerializer.Serialize(new { request.Motivo, request.PartidosSuspension }));

                _logger.LogInformation("Jugador suspendido - JugadorId: {JugadorId}, Partidos: {Partidos}", id, request.PartidosSuspension);

                return Ok(ApiRespuesta<object>.Success(null, $"Jugador suspendido por {request.PartidosSuspension} partidos"));
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
                _logger.LogError(ex, "Error al suspender jugador - JugadorId: {JugadorId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al suspender el jugador"));
            }
        }

        /// <summary>
        /// Levanta manualmente una suspensión activa de un jugador.
        /// </summary>
        /// <param name="id">ID del jugador.</param>
        /// <param name="motivo">Opcional. Justificación de la rehabilitación.</param>
        /// <returns>Confirmación de la rehabilitación.</returns>
        [HttpPost("{id}/rehabilitar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> RehabilitarJugador(int id, [FromQuery] string? motivo = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Rehabilitación de jugador - JugadorId: {JugadorId}, AdministradorId: {AdminId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                await _jugadorService.RehabilitarJugadorAsync(id, usuarioId, motivo, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "REHABILITAR_JUGADOR",
                    "jugadores_suspensiones",
                    id,
                    motivo);

                _logger.LogInformation("Jugador rehabilitado - JugadorId: {JugadorId}", id);

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
        /// Consulta el historial de suspensiones y sanciones de un jugador.
        /// </summary>
        /// <param name="id">ID del jugador.</param>
        /// <returns>Lista de suspensiones (activas e históricas).</returns>
        [HttpGet("{id}/suspensiones")]
        public async Task<IActionResult> ObtenerSuspensiones(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de suspensiones de jugador - JugadorId: {JugadorId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

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

        /// <summary>
        /// Genera un documento PDF con la credencial digital del jugador para un torneo específico.
        /// </summary>
        /// <param name="id">ID del jugador.</param>
        /// <param name="idTorneo">ID del torneo para el cual se emite la credencial.</param>
        /// <returns>Archivo PDF para su descarga o visualización.</returns>
        [HttpGet("{id}/credencial")]
        public async Task<IActionResult> GenerarCredencial(int id, [FromQuery] int idTorneo)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Generación de credencial - JugadorId: {JugadorId}, TorneoId: {TorneoId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, idTorneo, usuarioId, ipCliente);

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



        #region Métodos Privados
        /// <summary>
        /// Extrae el ID del usuario autenticado desde los Claims del Token JWT.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Se lanza si el token no es válido o no contiene el ID.</exception>
        private int ObtenerUsuarioActualId()
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out var usuarioId))
            {
                throw new UnauthorizedAccessException("Usuario no identificado");
            }

            return usuarioId;
        }
        /// <summary>
        /// Identifica la dirección IP del cliente que realiza la petición, considerando entornos con proxies.
        /// </summary>
        private string ObtenerIpCliente()
        {
            var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip))
            {
                ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            return ip ?? "IP desconocida";
        }
        /// <summary>
        /// Genera la estructura HTML para la página de éxito al unirse a un equipo.
        /// </summary>
        /// <param name="nombreEquipo">Nombre del equipo para mostrar en la tarjeta.</param>
        /// <param name="nombreJugador">Nombre del jugador para personalizar el mensaje.</param>
        /// <returns>String con el código HTML embebido.</returns>
        private string GenerarHtmlExito(string nombreEquipo, string nombreJugador)
        {
            return $@"<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>¡Bienvenido! - TorneoPro</title>
    <style>
        * {{ margin:0; padding:0; box-sizing:border-box; }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
            background: linear-gradient(135deg, #1E3A8A 0%, #2563EB 100%);
            min-height: 100vh;
            display: flex; align-items: center; justify-content: center;
            padding: 20px;
        }}
        .card {{
            max-width: 400px; width: 100%;
            background: #fff; border-radius: 28px; overflow: hidden;
            box-shadow: 0 32px 64px -12px rgba(0,0,0,.35);
            animation: rise .5s cubic-bezier(.22,.68,0,1.2) both;
        }}
        @keyframes rise {{
            from {{ opacity:0; transform:translateY(24px) scale(.97); }}
            to   {{ opacity:1; transform:none; }}
        }}
        .header {{
            background: linear-gradient(135deg, #1E3A8A, #2563EB);
            padding: 28px 24px; text-align: center;
        }}
        .header h1 {{ color:#fff; font-size:24px; font-weight:700; }}
        .header p  {{ color:rgba(255,255,255,.7); font-size:13px; margin-top:4px; }}
        .body {{ padding: 32px 28px; text-align: center; }}
        .success-icon {{
            width: 80px; height: 80px; border-radius: 50%;
            background: #DCFCE7; display: flex; align-items: center;
            justify-content: center; margin: 0 auto 20px; font-size: 40px;
        }}
        .title {{ font-size: 20px; font-weight: 700; color: #111827; margin-bottom: 8px; }}
        .subtitle {{ font-size: 14px; color: #6B7280; margin-bottom: 24px; line-height: 1.5; }}
        .info-box {{
            background: #F0FDF4; border: 1px solid #BBF7D0;
            border-radius: 14px; padding: 14px 16px; margin-bottom: 24px;
            text-align: left;
        }}
        .info-box .label {{
            font-size: 11px; color: #16A34A; font-weight: 600;
            text-transform: uppercase; letter-spacing: .5px; margin-bottom: 4px;
        }}
        .info-box .value {{ font-size: 16px; font-weight: 600; color: #111827; }}
        .player-info {{ font-size: 13px; color: #6B7280; margin-top: 4px; }}
        .footer {{
            background: #F9FAFB; border-top: 1px solid #F3F4F6;
            padding: 14px 28px; text-align: center;
            font-size: 12px; color: #9CA3AF;
        }}
    </style>
</head>
<body>
<div class='card'>
    <div class='header'>
        <h1>⚽ TorneoPro</h1>
        <p>Plataforma de gestión deportiva</p>
    </div>
    <div class='body'>
        <div class='success-icon'>✅</div>
        <div class='title'>¡Te has unido exitosamente!</div>
        <div class='subtitle'>
            Ya eres parte del equipo. Puedes iniciar sesión en la app para ver tu perfil y estadísticas.
        </div>
        <div class='info-box'>
            <div class='label'>Equipo</div>
            <div class='value'>🏆 {System.Web.HttpUtility.HtmlEncode(nombreEquipo)}</div>
            <div class='player-info'>👤 {System.Web.HttpUtility.HtmlEncode(nombreJugador)}</div>
        </div>
    </div>
    <div class='footer'>
        © {DateTime.UtcNow.Year} TorneoPro · Todos los derechos reservados
    </div>
</div>
</body>
</html>";
        }

        #endregion
    }
    /// <summary>
    /// Estructura de datos para solicitar la suspensión de un jugador.
    /// </summary>
    public class SuspenderJugadorRequest
    {
        public string Motivo { get; set; } = string.Empty;
        public int PartidosSuspension { get; set; } = 1;
        public int? IdTorneo { get; set; }
    }

    /// <summary>
    /// Respuesta detallada para el procesamiento de Deep Links en la aplicación móvil.
    /// </summary>
    public class DeepLinkInfoResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string NombreEntidad { get; set; } = string.Empty;
        public DateTime FechaExpiracion { get; set; }
        public bool EsValido { get; set; }
        public string? ErrorMensaje { get; set; }
        public string DeepLink { get; set; } = string.Empty;
    }
}