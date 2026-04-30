using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorneoPro.API.DTOs.AccesoTemporal.Request;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.AccesoTemporal;
using TorneoPro.API.Servicios.Interfaces.Auditoria;

namespace TorneoPro.API.Controllers
{
    [Route("api/acceso-temporal")]
    [ApiController]
    /// <summary>
    /// Controlador encargado de gestionar el ciclo de vida de los accesos temporales y enlaces compartidos.
    /// Permite la creación de invitaciones, accesos para árbitros, capitanes y jugadores, 
    /// así como la gestión de Deep Links y auditoría de uso.
    /// </summary
    public class AccesoTemporalController : ControllerBase
    {
        private readonly IAccesoTemporalService _accesoTemporalService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<AccesoTemporalController> _logger;

        public AccesoTemporalController(
            IAccesoTemporalService accesoTemporalService,
            IAuditoriaService auditoriaService,
            ILogger<AccesoTemporalController> logger)
        {
            _accesoTemporalService = accesoTemporalService;
            _auditoriaService = auditoriaService;
            _logger = logger;
        }

        /// <summary>
        /// Genera un enlace de acceso temporal para que un árbitro gestione un partido específico.
        /// </summary>
        /// <param name="idPartido">ID del partido al que se otorgará acceso.</param>
        /// <param name="idUsuarioDestino">Opcional. ID del usuario que recibirá el acceso.</param>
        /// <returns>Información del enlace generado incluyendo el Token y la fecha de expiración.</returns>
        /// <response code="200">Enlace creado correctamente.</response>
        /// <response code="404">El partido no existe.</response>
        [HttpPost("arbitro-partido/{idPartido}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> CrearEnlaceArbitroPartido(int idPartido, [FromQuery] int? idUsuarioDestino = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Creando enlace para árbitro de partido - PartidoId: {PartidoId}, UsuarioId: {UsuarioId}",
                idPartido, usuarioId);

            try
            {
                var resultado = await _accesoTemporalService.CrearEnlaceArbitroPartidoAsync(usuarioId, idPartido, idUsuarioDestino, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "CREAR_ENLACE_ARBITRO_PARTIDO",
                    "partidos",
                    idPartido,
                    System.Text.Json.JsonSerializer.Serialize(new { resultado.Token, resultado.FechaExpiracion }));

                _logger.LogInformation("Enlace para árbitro creado - PartidoId: {PartidoId}, Token: {Token}", idPartido, resultado.Token);

                return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(resultado, "Enlace para árbitro creado exitosamente"));
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
                _logger.LogError(ex, "Error al crear enlace para árbitro - PartidoId: {PartidoId}", idPartido);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al crear enlace"));
            }
        }

        /// <summary>
        /// Crea un acceso temporal para el capitán de un equipo, permitiéndole gestionar la alineación de un partido.
        /// </summary>
        /// <param name="idPartido">ID del partido correspondiente.</param>
        /// <param name="idEquipo">ID del equipo que gestionará su alineación.</param>
        /// <param name="idUsuarioDestino">Opcional. Usuario específico que recibirá la invitación.</param>
        /// <returns>Respuesta con el token de acceso generado.</returns>
        [HttpPost("capitan-alineacion/{idPartido}/{idEquipo}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> CrearEnlaceCapitanAlineacion(int idPartido, int idEquipo, [FromQuery] int? idUsuarioDestino = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Creando enlace para capitán de alineación - PartidoId: {PartidoId}, EquipoId: {EquipoId}",
                idPartido, idEquipo);

            try
            {
                var resultado = await _accesoTemporalService.CrearEnlaceCapitanAlineacionAsync(usuarioId, idPartido, idEquipo, idUsuarioDestino, ipCliente);

                return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(resultado, "Enlace para capitán creado exitosamente"));
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
                _logger.LogError(ex, "Error al crear enlace para capitán");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al crear enlace"));
            }
        }

        /// <summary>
        /// Genera un enlace para que un jugador individual pueda confirmar su asistencia a un partido.
        /// </summary>
        /// <param name="idPartido">ID del partido.</param>
        /// <param name="idJugador">ID del jugador que debe confirmar.</param>
        /// <returns>Datos del enlace temporal para su envío.</returns>
        [HttpPost("jugador-asistencia/{idPartido}/{idJugador}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> CrearEnlaceJugadorAsistencia(int idPartido, int idJugador)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Creando enlace para jugador asistencia - PartidoId: {PartidoId}, JugadorId: {JugadorId}",
                idPartido, idJugador);

            try
            {
                var resultado = await _accesoTemporalService.CrearEnlaceJugadorAsistenciaAsync(usuarioId, idPartido, idJugador, ipCliente);

                return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(resultado, "Enlace para jugador creado exitosamente"));
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
                _logger.LogError(ex, "Error al crear enlace para jugador");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al crear enlace"));
            }
        }

        /// <summary>
        /// Permite crear un enlace temporal genérico o personalizado basado en un tipo de entidad y destino.
        /// </summary>
        /// <param name="solicitud">Configuración del enlace (TipoEntidad, IdEntidad, Expiración, etc.).</param>
        /// <returns>El enlace temporal procesado.</returns>
        [HttpPost("crear")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> CrearEnlaceTemporal([FromBody] EnlaceTemporalRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Creando enlace temporal personalizado - TipoEntidad: {TipoEntidad}, IdEntidad: {IdEntidad}",
                solicitud.TipoEntidad, solicitud.IdEntidad);

            try
            {
                var resultado = await _accesoTemporalService.CrearEnlaceTemporalAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "CREAR_ENLACE_TEMPORAL",
                    "enlaces_compartidos",
                    resultado.Id,
                    System.Text.Json.JsonSerializer.Serialize(solicitud));

                return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(resultado, "Enlace temporal creado exitosamente"));
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
                _logger.LogError(ex, "Error al crear enlace temporal");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al crear enlace"));
            }
        }

        /// <summary>
        /// Procesa el uso de un token de acceso temporal, validando su vigencia y otorgando los permisos correspondientes.
        /// </summary>
        /// <remarks>
        /// Este endpoint registra el intento de acceso (éxito o error) en el sistema de auditoría.
        /// Soporta acceso anónimo para flujos de invitaciones externas.
        /// </remarks>
        /// <param name="token">Token único del enlace compartido.</param>
        /// <returns>Resultado del uso, indicando el destino o permisos concedidos.</returns>
        [HttpPost("usar/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> UsarEnlaceTemporal(string token)
        {
            var usuarioId = ObtenerUsuarioActualIdAutenticado();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Uso de enlace temporal - Token: {Token}, UsuarioId: {UsuarioId}, IP: {Ip}",
                token, usuarioId, ipCliente);

            try
            {
                var resultado = await _accesoTemporalService.UsarEnlaceTemporalAsync(token, usuarioId, ipCliente, userAgent);

                if (resultado.Exitoso)
                {
                    await _auditoriaService.RegistrarExitoAsync(
                        usuarioId,
                        "USAR_ENLACE_TEMPORAL",
                        null,
                        null,
                        System.Text.Json.JsonSerializer.Serialize(new { token, resultado.TipoEntidad, resultado.IdEntidad }));
                }
                else
                {
                    await _auditoriaService.RegistrarErrorAsync(
                        usuarioId,
                        "USAR_ENLACE_TEMPORAL",
                        resultado.Mensaje ?? "Error desconocido",
                        null,
                        null,
                        null,
                        ipCliente,
                        userAgent);
                }

                if (!resultado.Exitoso)
                    return BadRequest(ApiRespuesta<object>.Error(resultado.Mensaje ?? "Error al usar enlace"));

                return Ok(ApiRespuesta<UsarEnlaceTemporalResponse>.Success(resultado, "Acceso concedido"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al usar enlace temporal - Token: {Token}", token);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al usar enlace"));
            }
        }

        /// <summary>
        /// Recupera información descriptiva de un enlace temporal sin marcarlo como "utilizado".
        /// </summary>
        /// <param name="token">Token del enlace.</param>
        /// <returns>Metadatos del enlace (Tipo, Entidad asociada, Estado).</returns>
        [HttpGet("info/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerInfoEnlace(string token)
        {
            try
            {
                var info = await _accesoTemporalService.ObtenerInfoEnlaceAsync(token);

                if (info == null)
                    return NotFound(ApiRespuesta<object>.Error("Enlace no encontrado"));

                return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(info));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener info de enlace - Token: {Token}", token);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener información"));
            }
        }

        /// <summary>
        /// Obtiene una lista paginada y filtrable de los enlaces creados por el administrador autenticado.
        /// </summary>
        /// <param name="solicitud">Parámetros de filtrado y paginación.</param>
        /// <returns>Lista de enlaces temporales activos o históricos.</returns>
        [HttpGet("mis-enlaces")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ObtenerMisEnlaces([FromQuery] FiltrarEnlaceTemporalRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();

            try
            {
                var resultado = await _accesoTemporalService.ObtenerMisEnlacesAsync(usuarioId, solicitud);
                return Ok(ApiRespuesta<ResultadoPaginado<EnlaceTemporalResponse>>.Success(resultado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener mis enlaces");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener enlaces"));
            }
        }

        /// <summary>
        /// Invalida manualmente un enlace temporal antes de su fecha de expiración.
        /// </summary>
        /// <param name="id">ID del registro del enlace.</param>
        /// <param name="motivo">Opcional. Razón por la cual se revoca el acceso.</param>
        /// <returns>Confirmación de la desactivación.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> DesactivarEnlace(int id, [FromQuery] string? motivo = null)
        {
            var usuarioId = ObtenerUsuarioActualId();

            try
            {
                await _accesoTemporalService.DesactivarEnlaceAsync(id, usuarioId, motivo);
                return Ok(ApiRespuesta<object>.Success(null, "Enlace desactivado exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar enlace - Id: {Id}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al desactivar enlace"));
            }
        }

        /// <summary>
        /// Extiende la validez de un enlace temporal sumando horas adicionales a su fecha de expiración.
        /// </summary>
        /// <param name="id">ID del enlace.</param>
        /// <param name="horasExtra">Cantidad de horas a añadir.</param>
        /// <returns>El enlace actualizado con la nueva fecha de expiración.</returns>
        [HttpPost("{id}/renovar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> RenovarEnlace(int id, [FromQuery] int horasExtra)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            try
            {
                var resultado = await _accesoTemporalService.RenovarEnlaceAsync(id, usuarioId, horasExtra, ipCliente);
                return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(resultado, "Enlace renovado exitosamente"));
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
                _logger.LogError(ex, "Error al renovar enlace - Id: {Id}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al renovar enlace"));
            }
        }

        /// <summary>
        /// Consulta el historial detallado de cuándo, quién y desde dónde se ha utilizado un enlace específico.
        /// </summary>
        /// <param name="id">ID del enlace.</param>
        /// <returns>Lista de registros de uso (IP, UserAgent, Fecha, Usuario).</returns>
        [HttpGet("{id}/historial")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ObtenerHistorialUsos(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();

            try
            {
                var historial = await _accesoTemporalService.ObtenerHistorialUsosAsync(id, usuarioId);
                return Ok(ApiRespuesta<List<UsoEnlaceTemporalResponse>>.Success(historial));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de enlace - Id: {Id}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener historial"));
            }
        }

        /// <summary>
        /// Trigger manual (o para Jobs programados) para generar y enviar automáticamente enlaces a partidos programados para el día siguiente.
        /// </summary>
        /// <remarks>Restringido exclusivamente a SUPER_ADMIN para procesos de mantenimiento.</remarks>
        /// <returns>Resultado de la ejecución del proceso masivo.</returns>
        [HttpPost("enviar-automaticos")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> EnviarEnlacesAutomaticos()
        {
            var usuarioId = ObtenerUsuarioActualId();

            try
            {
                await _accesoTemporalService.EnviarEnlacesAutomaticosAsync();

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "ENVIAR_ENLACES_AUTOMATICOS",
                    null,
                    null);

                return Ok(ApiRespuesta<object>.Success(null, "Enlaces automáticos enviados exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar enlaces automáticos");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al enviar enlaces automáticos"));
            }
        }


        /// <summary>
        /// Proporciona metadatos sobre un Deep Link antes de que la aplicación móvil procese la redirección.
        /// </summary>
        /// <remarks>
        /// Este endpoint es utilizado por la aplicación móvil para validar el token y obtener información 
        /// contextual (tipo de invitación, nombre del equipo, etc.) antes de decidir a qué pantalla navegar.
        /// Al ser <see cref="AllowAnonymous"/>, permite que la app verifique el enlace incluso antes de que el usuario inicie sesión.
        /// </remarks>
        /// <param name="token">Token único asociado al acceso temporal o invitación.</param>
        /// <returns>Objeto con la información de destino y validez del enlace.</returns>
        /// <response code="200">Retorna los datos del enlace encontrados.</response>
        /// <response code="404">El token no existe, ya fue utilizado o ha expirado.</response>
        [HttpGet("deep-link/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerInfoDeepLink(string token)
        {
            try
            {
                var info = await _accesoTemporalService.ObtenerInfoDeepLinkAsync(token);
                if (info == null)
                    return NotFound(ApiRespuesta<object>.Error("Enlace no válido o expirado"));

                return Ok(ApiRespuesta<object>.Success(info));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener info deep link");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al procesar el enlace"));
            }
        }

        /// <summary>
        /// Gestiona la redirección web cuando un usuario abre un enlace y no tiene la aplicación instalada.
        /// </summary>
        /// <remarks>
        /// Actúa como un "Traffic Manager" que redirige al usuario a la vista web correspondiente 
        /// según el tipo de contenido vinculado al token (Invitación a Equipo, Acceso a Partido, etc.).
        /// Si el token es inválido, redirige a una página de error amigable.
        /// </remarks>
        /// <param name="token">Token de acceso temporal proporcionado en el query string.</param>
        /// <returns>Una redirección (302 Found) a la URL específica del recurso o error.</returns>
        /// <response code="302">Redirige al controlador de destino según el tipo de invitación.</response>
        [HttpGet("web-fallback")]
        [AllowAnonymous]
        public async Task<IActionResult> WebFallback([FromQuery] string token)
        {
            try
            {
                var info = await _accesoTemporalService.ObtenerInfoDeepLinkAsync(token);
                if (info == null || !info.EsValido)
                    return Redirect($"/invite/error?message={Uri.EscapeDataString("Enlace no válido")}");

                return info.Tipo switch
                {
                    "EQUIPO" => Redirect($"/api/jugadores/aceptar-invitacion?token={token}"),
                    "PARTIDO" => Redirect($"/api/partidos/acceder?token={token}"),
                    _ => Redirect($"/api/acceso-temporal/usar/{token}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en web fallback");
                return Redirect($"/invite/error?message={Uri.EscapeDataString("Error al procesar")}");
            }
        }
        /// <summary>
        /// Obtiene el ID del usuario desde el token JWT. Lanza excepción si no está autenticado.
        /// </summary>
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
        /// Intenta obtener el ID del usuario. Si no está autenticado, retorna 0 (Anónimo).
        /// </summary>
        private int ObtenerUsuarioActualIdAutenticado()
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (!string.IsNullOrEmpty(usuarioIdClaim) && int.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return usuarioId;
            }

            return 0; // Usuario anónimo
        }
        /// <summary>
        /// Resuelve la dirección IP del cliente, considerando encabezados de Proxy o Balanceador de carga.
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
    }
}