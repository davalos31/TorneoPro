using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorneoPro.API.DTOs.AccesoTemporal.Request;
using TorneoPro.API.DTOs.AccesoTemporal.Response;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using TorneoPro.API.Servicios.Interfaces.AccesoTemporal;
using TorneoPro.API.Servicios.Interfaces.Auditoria;

namespace TorneoPro.API.Controllers
{
    [Route("api/acceso-temporal")]
    [ApiController]
    public class AccesoTemporalController : ControllerBase
    {
        private readonly IAccesoTemporalService _accesoTemporalService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccesoTemporalController> _logger;

        public AccesoTemporalController(
            IAccesoTemporalService accesoTemporalService,
            IAuditoriaService auditoriaService,
            IConfiguration configuration,
            ILogger<AccesoTemporalController> logger)
        {
            _accesoTemporalService = accesoTemporalService;
            _auditoriaService = auditoriaService;
            _configuration = configuration;
            _logger = logger;
        }

        private string ObtenerBaseUrl() => _configuration["AppConfig:AppUrl"] ?? $"{Request.Scheme}://{Request.Host}";

        #region ========== 1. CREACIÓN DE ENLACES ==========

        /// <summary>
        /// Enlace para árbitro gestionar partido
        /// </summary>
        [HttpPost("arbitro/{idPartido}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> CrearEnlaceArbitro(int idPartido, [FromQuery] int? idUsuarioDestino = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var resultado = await _accesoTemporalService.CrearEnlaceArbitroPartidoAsync(
                usuarioId, idPartido, idUsuarioDestino, ObtenerIpCliente(), ObtenerUserAgent());

            await _auditoriaService.RegistrarExitoAsync(usuarioId, "CREAR_ENLACE_ARBITRO", "partidos", idPartido);
            return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(resultado, "Enlace para árbitro creado"));
        }

        /// <summary>
        /// Enlace para capitán registrar alineación
        /// </summary>
        [HttpPost("capitan/{idPartido}/{idEquipo}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> CrearEnlaceCapitan(int idPartido, int idEquipo, [FromQuery] int? idUsuarioDestino = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var resultado = await _accesoTemporalService.CrearEnlaceCapitanAlineacionAsync(
                usuarioId, idPartido, idEquipo, idUsuarioDestino, ObtenerIpCliente(), ObtenerUserAgent());

            await _auditoriaService.RegistrarExitoAsync(usuarioId, "CREAR_ENLACE_CAPITAN", "partidos", idPartido);
            return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(resultado, "Enlace para capitán creado"));
        }

        /// <summary>
        /// Enlace para jugador confirmar asistencia
        /// </summary>
        [HttpPost("jugador/{idPartido}/{idJugador}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> CrearEnlaceJugador(int idPartido, int idJugador)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var resultado = await _accesoTemporalService.CrearEnlaceJugadorAsistenciaAsync(
                usuarioId, idPartido, idJugador, ObtenerIpCliente(), ObtenerUserAgent());

            await _auditoriaService.RegistrarExitoAsync(usuarioId, "CREAR_ENLACE_JUGADOR", "partidos", idPartido);
            return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(resultado, "Enlace para jugador creado"));
        }

        /// <summary>
        /// Invitación a equipo
        /// </summary>
        [HttpPost("invitar-equipo/{idEquipo}/{idJugador}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,CAPITAN")]
        public async Task<IActionResult> InvitarJugadorAEquipo(int idEquipo, int idJugador)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var resultado = await _accesoTemporalService.CrearEnlaceInvitacionEquipoAsync(
                usuarioId, idEquipo, idJugador, ObtenerIpCliente(), ObtenerUserAgent());

            await _auditoriaService.RegistrarExitoAsync(usuarioId, "INVITAR_JUGADOR_EQUIPO", "equipos", idEquipo);
            return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(resultado, "Invitación enviada"));
        }

        /// <summary>
        /// Enlace temporal genérico
        /// </summary>
        [HttpPost("crear")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> CrearEnlaceTemporal([FromBody] EnlaceTemporalRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var resultado = await _accesoTemporalService.CrearEnlaceTemporalAsync(
                usuarioId, solicitud, ObtenerIpCliente(), ObtenerUserAgent());

            await _auditoriaService.RegistrarExitoAsync(usuarioId, "CREAR_ENLACE_TEMPORAL", "enlaces_compartidos", resultado.Id);
            return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(resultado, "Enlace temporal creado"));
        }

        #endregion

        #region ========== 2. USO Y CONSULTA ==========

        /// <summary>
        /// Usar enlace (asignar rol o acceder a recurso)
        /// </summary>
        [HttpPost("usar/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> UsarEnlace(string token)
        {
            var usuarioId = ObtenerUsuarioActualIdAutenticado();
            var resultado = await _accesoTemporalService.UsarEnlaceTemporalAsync(
                token, usuarioId, ObtenerIpCliente(), ObtenerUserAgent());

            if (!resultado.Exitoso)
                return BadRequest(ApiRespuesta<object>.Error(resultado.Mensaje ?? "Error al usar enlace"));

            await _auditoriaService.RegistrarExitoAsync(usuarioId, "USAR_ENLACE_TEMPORAL", null, null);
            return Ok(ApiRespuesta<UsarEnlaceTemporalResponse>.Success(resultado, "Acceso concedido"));
        }

        /// <summary>
        /// Obtener información del enlace (JSON)
        /// </summary>
        [HttpGet("info/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerInfoEnlace(string token)
        {
            var info = await _accesoTemporalService.ObtenerInfoEnlaceAsync(token);
            if (info == null)
                return NotFound(ApiRespuesta<object>.Error("Enlace no encontrado"));

            return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(info));
        }

        /// <summary>
        /// Obtener información para deep link (app móvil)
        /// </summary>
        [HttpGet("deep-link/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerInfoDeepLink(string token)
        {
            var info = await _accesoTemporalService.ObtenerInfoDeepLinkAsync(token);
            if (info == null)
                return NotFound(ApiRespuesta<object>.Error("Enlace no válido o expirado"));

            return Ok(ApiRespuesta<object>.Success(info));
        }

        #endregion

        #region ========== 3. PÁGINAS WEB (HTML) ==========

        /// <summary>
        /// Página principal de invitación
        /// </summary>
        [HttpGet("/invite/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> InvitePage(string token)
        {
            var info = await _accesoTemporalService.ObtenerInfoInvitacionAsync(token);

            if (info == null || !info.EsValido)
            {
                var errorMsg = info?.ErrorMensaje ?? "Enlace no válido o expirado";
                return Redirect($"/invite/error?message={Uri.EscapeDataString(errorMsg)}");
            }

            // Si es petición API, devolver JSON
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
                return Ok(ApiRespuesta<InviteInfoResponse>.Success(info));

            // Si es móvil, redirigir directamente al deep link
            if (Request.Headers["User-Agent"].ToString().Contains("Mobile"))
                return Redirect(info.DeepLink);

            var html = ViewHelper.GenerarPaginaInvitacion(info, ObtenerBaseUrl());
            return Content(html, "text/html");
        }

        /// <summary>
        /// Página de error
        /// </summary>
        [HttpGet("/invite/error")]
        [AllowAnonymous]
        public IActionResult InviteErrorPage([FromQuery] string message = "Enlace no válido")
        {
            var html = ViewHelper.GenerarPaginaError(message, ObtenerBaseUrl());
            return Content(html, "text/html");
        }

        /// <summary>
        /// Fallback web - redirige a login
        /// </summary>
        [HttpGet("/invite/fallback/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> InviteFallback(string token)
        {
            var info = await _accesoTemporalService.ObtenerInfoInvitacionAsync(token);

            if (info == null || !info.EsValido)
                return Redirect($"/invite/error?message={Uri.EscapeDataString("Enlace no válido")}");

            var returnUrl = info.Tipo switch
            {
                "EQUIPO" => $"/api/equipos/unirse?token={token}",
                "PARTIDO" => $"/api/partidos/acceder?token={token}",
                "TORNEO" => $"/api/torneos/acceder?token={token}",
                _ => $"/api/acceso-temporal/usar/{token}"
            };

            return Redirect($"/login?token={token}&entidad={Uri.EscapeDataString(info.NombreEntidad)}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        /// <summary>
        /// Página de login (para web)
        /// </summary>
        [HttpGet("/login")]
        [AllowAnonymous]
        public IActionResult LoginPage([FromQuery] string? token = null, [FromQuery] string? entidad = null, [FromQuery] string? returnUrl = null)
        {
            var html = ViewHelper.GenerarPaginaLogin(token ?? "", entidad ?? "", returnUrl ?? "/", ObtenerBaseUrl());
            return Content(html, "text/html");
        }

        /// <summary>
        /// Página de registro (para web)
        /// </summary>
        [HttpGet("/registro")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistroPage([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
                return Redirect("/invite/error?message=Token no proporcionado");

            var info = await _accesoTemporalService.ObtenerInfoInvitacionAsync(token);
            if (info == null || !info.EsValido)
                return Redirect($"/invite/error?message={Uri.EscapeDataString(info?.ErrorMensaje ?? "Enlace no válido")}");

            var html = ViewHelper.GenerarPaginaRegistro(token, info.NombreEntidad, ObtenerBaseUrl());
            return Content(html, "text/html");
        }

        #endregion

        #region ========== 4. REGISTRO PÚBLICO (API) ==========

        /// <summary>
        /// Registrar nuevo jugador desde invitación
        /// </summary>
        [HttpPost("registro/crear")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrarDesdeInvitacion([FromBody] RegistroPublicoRequest request)
        {
            _logger.LogInformation("Registro público - Email: {Email}, TokenEquipo: {TokenEquipo}", request.Email, request.TokenEquipo);

            try
            {
                var resultado = await _accesoTemporalService.RegistrarJugadorDesdeInvitacionAsync(request, ObtenerIpCliente(), ObtenerUserAgent());
                return Ok(ApiRespuesta<RegistroPublicoResponse>.Success(resultado, "Registro exitoso. Te has unido al equipo."));
            }
            catch (InvalidOperationException ex) { return BadRequest(ApiRespuesta<object>.Error(ex.Message)); }
            catch (KeyNotFoundException ex) { return NotFound(ApiRespuesta<object>.Error(ex.Message)); }
            catch (Exception ex) { return StatusCode(500, ApiRespuesta<object>.Error("Error al registrar")); }
        }

        /// <summary>
        /// Usuario existente se une a equipo
        /// </summary>
        [HttpPost("registro/unirse")]
        [Authorize]
        public async Task<IActionResult> UnirseAEquipo([FromBody] UnirseEquipoRequest request)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var resultado = await _accesoTemporalService.UnirseAEquipoAsync(request.TokenEquipo, usuarioId, ObtenerIpCliente(), ObtenerUserAgent());
            return Ok(ApiRespuesta<UnirseEquipoResponse>.Success(resultado, "Te has unido al equipo exitosamente"));
        }

        #endregion

        #region ========== 5. GESTIÓN DE ENLACES ==========

        [HttpGet("mis-enlaces")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ObtenerMisEnlaces([FromQuery] FiltrarEnlaceTemporalRequest solicitud)
        {
            var resultado = await _accesoTemporalService.ObtenerMisEnlacesAsync(ObtenerUsuarioActualId(), solicitud);
            return Ok(ApiRespuesta<ResultadoPaginado<EnlaceTemporalResponse>>.Success(resultado));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> DesactivarEnlace(int id, [FromQuery] string? motivo = null)
        {
            await _accesoTemporalService.DesactivarEnlaceAsync(id, ObtenerUsuarioActualId(), motivo);
            return Ok(ApiRespuesta<object>.Success(null, "Enlace desactivado"));
        }

        [HttpPost("{id}/renovar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> RenovarEnlace(int id, [FromQuery] int horasExtra)
        {
            var resultado = await _accesoTemporalService.RenovarEnlaceAsync(id, ObtenerUsuarioActualId(), horasExtra, ObtenerIpCliente());
            return Ok(ApiRespuesta<EnlaceTemporalResponse>.Success(resultado, "Enlace renovado"));
        }

        [HttpGet("{id}/historial")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ObtenerHistorialUsos(int id)
        {
            var historial = await _accesoTemporalService.ObtenerHistorialUsosAsync(id, ObtenerUsuarioActualId());
            return Ok(ApiRespuesta<List<UsoEnlaceTemporalResponse>>.Success(historial));
        }

        [HttpPost("enviar-automaticos")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<IActionResult> EnviarEnlacesAutomaticos()
        {
            await _accesoTemporalService.EnviarEnlacesAutomaticosAsync();
            return Ok(ApiRespuesta<object>.Success(null, "Enlaces automáticos enviados"));
        }

        #endregion

        #region ========== 6. MÉTODOS PRIVADOS ==========

        private int ObtenerUsuarioActualId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Usuario no identificado");
            return id;
        }

        private int ObtenerUsuarioActualIdAutenticado()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out var id) ? id : 0;
        }

        private string ObtenerIpCliente()
        {
            var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return string.IsNullOrEmpty(ip) ? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "IP desconocida" : ip;
        }

        private string ObtenerUserAgent() => Request.Headers["User-Agent"].FirstOrDefault() ?? "desconocido";

        #endregion
    }

    #region DTOs

    public class RegistroPublicoRequest
    {
        public string TokenEquipo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public int? IdTipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public decimal? PesoKg { get; set; }
        public decimal? AlturaCm { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? Genero { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public string? PosicionPreferida { get; set; }
        public int? NumeroCamisetaPreferido { get; set; }
    }

    public class RegistroPublicoResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }

    public class UnirseEquipoRequest
    {
        public string TokenEquipo { get; set; } = string.Empty;
    }

    public class UnirseEquipoResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; } = string.Empty;
    }

    #endregion
}