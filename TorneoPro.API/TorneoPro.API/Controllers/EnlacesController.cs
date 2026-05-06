using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorneoPro.API.DTOs.Enlaces;
using TorneoPro.API.DTOs.Enlaces.Request;
using TorneoPro.API.DTOs.Enlaces.Response;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Enlaces;

namespace TorneoPro.API.Controllers
{
    /// <summary>
    /// Controlador encargado de gestionar la generación, validación y uso de enlaces compartidos.
    /// Centraliza la lógica de invitaciones para roles como Capitanes, Árbitros, Sub-administradores y Jugadores.
    /// </summary>
    [Route("api/enlaces")]
    [ApiController]
    public class EnlacesController : ControllerBase
    {
        private readonly IEnlaceService _enlaceService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<EnlacesController> _logger;

        public EnlacesController(
            IEnlaceService enlaceService,
            IAuditoriaService auditoriaService,
            ILogger<EnlacesController> logger)
        {
            _enlaceService = enlaceService;
            _auditoriaService = auditoriaService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el catálogo de tipos de enlace soportados por el sistema.
        /// </summary>
        /// <remarks>
        /// Útil para llenar selectores en el frontend. Este endpoint es de acceso público.
        /// </remarks>
        /// <returns>Una lista de objetos con el ID y descripción de los tipos de enlace.</returns>
        /// <response code="200">Retorna la lista de tipos exitosamente.</response>
        /// <response code="500">Error interno del servidor al procesar la solicitud.</response>
        [HttpGet("tipos")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerTiposEnlace()
        {
            try
            {
                var tipos = await _enlaceService.ObtenerTiposEnlaceAsync();
                return Ok(ApiRespuesta<List<TipoEnlaceResponse>>.Success(tipos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de enlace");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener tipos de enlace"));
            }
        }

        /// <summary>
        /// Obtiene estadísticas globales de los enlaces creados por el usuario actual.
        /// </summary>
        /// <remarks>
        /// Incluye contadores de enlaces activos, expirados y total de usos registrados.
        /// </remarks>
        /// <returns>Objeto con el resumen estadístico del usuario.</returns>
        /// <response code="200">Retorna las estadísticas del usuario.</response>
        /// <response code="401">Usuario no autenticado.</response>
        [HttpGet("estadisticas")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,CAPITAN")]
        public async Task<IActionResult> ObtenerEstadisticas()
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de estadísticas de enlaces - UsuarioId: {UsuarioId}, IP: {Ip}",
                usuarioId, ipCliente);

            try
            {
                var estadisticas = await _enlaceService.ObtenerEstadisticasAsync(usuarioId);
                return Ok(ApiRespuesta<EnlaceEstadisticasResponse>.Success(estadisticas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de enlaces - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener estadísticas"));
            }
        }

        /// <summary>
        /// Invalida el código de un enlace existente y genera uno nuevo.
        /// </summary>
        /// <param name="id">Identificador único del enlace a regenerar.</param>
        /// <remarks>
        /// El código anterior dejará de funcionar inmediatamente. Solo el creador o un admin pueden ejecutar esta acción.
        /// </remarks>
        /// <returns>Datos del enlace con el nuevo código alfanumérico.</returns>
        /// <response code="200">Código regenerado con éxito.</response>
        /// <response code="403">El usuario no tiene permisos para modificar este enlace.</response>
        /// <response code="404">No se encontró el enlace con el ID proporcionado.</response>
        [HttpPost("{id}/regenerar-codigo")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,CAPITAN")]
        public async Task<IActionResult> RegenerarCodigoEnlace(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Regeneración de código de enlace - EnlaceId: {EnlaceId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var resultado = await _enlaceService.RegenerarCodigoEnlaceAsync(id, usuarioId, ipCliente);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "REGENERAR_CODIGO_ENLACE",
                    "enlaces_compartidos",
                    id);

                _logger.LogInformation("Código de enlace regenerado - EnlaceId: {EnlaceId}, NuevoCodigo: {Codigo}",
                    id, resultado.CodigoEnlace);

                return Ok(ApiRespuesta<EnlaceResponse>.Success(resultado, "Código de enlace regenerado exitosamente"));
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
                _logger.LogError(ex, "Error al regenerar código de enlace - EnlaceId: {EnlaceId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al regenerar el código"));
            }
        }

        /// <summary>
        /// Lista de forma paginada los enlaces creados por el usuario que realiza la petición.
        /// </summary>
        /// <param name="solicitud">Filtros de búsqueda (Estado, Tipo) y parámetros de paginación.</param>
        /// <returns>Resultado paginado con la lista de enlaces.</returns>
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,SUB_ADMIN,CAPITAN")]
        public async Task<IActionResult> ObtenerMisEnlaces([FromQuery] FiltrarEnlaceRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de enlaces - UsuarioId: {UsuarioId}, Pagina: {Pagina}, IP: {Ip}",
                usuarioId, solicitud.Pagina, ipCliente);

            try
            {
                var resultado = await _enlaceService.ObtenerMisEnlacesAsync(usuarioId, solicitud);

                _logger.LogInformation("Enlaces consultados - UsuarioId: {UsuarioId}, Total: {Total}",
                    usuarioId, resultado.TotalItems);

                return Ok(ApiRespuesta<ResultadoPaginado<EnlaceResponse>>.Success(resultado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener enlaces - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener enlaces"));
            }
        }

        /// <summary>
        /// Obtiene el detalle completo de un enlace específico.
        /// </summary>
        /// <param name="id">ID del enlace.</param>
        /// <returns>Información detallada del enlace incluyendo configuración y metadatos.</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,SUB_ADMIN,CAPITAN")]
        public async Task<IActionResult> ObtenerEnlace(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de enlace - EnlaceId: {EnlaceId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var enlace = await _enlaceService.ObtenerEnlacePorIdAsync(id, usuarioId);

                if (enlace == null)
                {
                    _logger.LogWarning("Enlace no encontrado - EnlaceId: {EnlaceId}, UsuarioId: {UsuarioId}", id, usuarioId);
                    return NotFound(ApiRespuesta<object>.Error("Enlace no encontrado"));
                }

                return Ok(ApiRespuesta<EnlaceResponse>.Success(enlace));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener enlace - EnlaceId: {EnlaceId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener enlace"));
            }
        }

        /// <summary>
        /// Crea un enlace de invitación para el rol de Capitán.
        /// </summary>
        /// <param name="solicitud">Configuración del enlace (Torneo, fecha de expiración, etc.).</param>
        /// <returns>Objeto con el enlace generado.</returns>
        [HttpPost("capitan")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,CAPITAN")]
        public async Task<IActionResult> GenerarEnlaceCapitan([FromBody] CrearEnlaceRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Generación de enlace de capitán - UsuarioId: {UsuarioId}, IP: {Ip}",
                usuarioId, ipCliente);

            try
            {
                var resultado = await _enlaceService.CrearEnlaceCapitanAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "GENERAR_ENLACE_CAPITAN",
                    "enlaces_compartidos",
                    resultado.Id,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        solicitud.IdRolAsignado,
                        solicitud.IdTorneo,
                        solicitud.IdEquipo,
                        solicitud.FechaExpiracion,
                        solicitud.MaxUsos
                    }));

                _logger.LogInformation("Enlace de capitán generado - EnlaceId: {EnlaceId}, Codigo: {Codigo}",
                    resultado.Id, resultado.CodigoEnlace);

                return Ok(ApiRespuesta<EnlaceResponse>.Success(resultado, "Enlace de capitán generado exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(usuarioId, "GENERAR_ENLACE_CAPITAN", ex.Message, "enlaces_compartidos", null, null, ipCliente, userAgent);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(usuarioId, "GENERAR_ENLACE_CAPITAN", ex.Message, "enlaces_compartidos", null, null, ipCliente, userAgent);
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(usuarioId, "GENERAR_ENLACE_CAPITAN", ex.Message, "enlaces_compartidos", null, null, ipCliente, userAgent);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar enlace de capitán - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al generar enlace"));
            }
        }

        /// <summary>
        /// Crea un enlace de invitación específico para Árbitros.
        /// </summary>
        [HttpPost("arbitro")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> GenerarEnlaceArbitro([FromBody] CrearEnlaceRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Generación de enlace de árbitro - UsuarioId: {UsuarioId}, IP: {Ip}",
                usuarioId, ipCliente);

            try
            {
                var resultado = await _enlaceService.CrearEnlaceArbitroAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "GENERAR_ENLACE_ARBITRO",
                    "enlaces_compartidos",
                    resultado.Id,
                    System.Text.Json.JsonSerializer.Serialize(new { solicitud.IdRolAsignado, solicitud.IdTorneo }));

                _logger.LogInformation("Enlace de árbitro generado - EnlaceId: {EnlaceId}, Codigo: {Codigo}",
                    resultado.Id, resultado.CodigoEnlace);

                return Ok(ApiRespuesta<EnlaceResponse>.Success(resultado, "Enlace de árbitro generado exitosamente"));
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
                _logger.LogError(ex, "Error al generar enlace de árbitro - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al generar enlace"));
            }
        }

        /// <summary>
        /// Genera un enlace para asignar roles de Sub-administrador a usuarios.
        /// </summary>
        /// <param name="solicitud">Parámetros de creación del enlace.</param>
        /// <returns>Detalles del enlace de sub-admin creado.</returns>
        [HttpPost("subadmin")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> GenerarEnlaceSubAdmin([FromBody] CrearEnlaceRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Generación de enlace de subadministrador - UsuarioId: {UsuarioId}, IP: {Ip}",
                usuarioId, ipCliente);

            try
            {
                var resultado = await _enlaceService.CrearEnlaceSubAdminAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "GENERAR_ENLACE_SUBADMIN",
                    "enlaces_compartidos",
                    resultado.Id,
                    System.Text.Json.JsonSerializer.Serialize(new { solicitud.IdRolAsignado, solicitud.IdTorneo }));

                _logger.LogInformation("Enlace de subadministrador generado - EnlaceId: {EnlaceId}, Codigo: {Codigo}",
                    resultado.Id, resultado.CodigoEnlace);

                return Ok(ApiRespuesta<EnlaceResponse>.Success(resultado, "Enlace de subadministrador generado exitosamente"));
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
                _logger.LogError(ex, "Error al generar enlace de subadministrador - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al generar enlace"));
            }
        }

        /// <summary>
        /// Crea un enlace de invitación para Jugadores dentro de un equipo o torneo.
        /// </summary>
        [HttpPost("jugador")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,CAPITAN")]
        public async Task<IActionResult> GenerarEnlaceJugador([FromBody] CrearEnlaceRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Generación de enlace de jugador - UsuarioId: {UsuarioId}, IP: {Ip}",
                usuarioId, ipCliente);

            try
            {
                var resultado = await _enlaceService.CrearEnlaceJugadorAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "GENERAR_ENLACE_JUGADOR",
                    "enlaces_compartidos",
                    resultado.Id,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        solicitud.IdRolAsignado,
                        solicitud.IdTorneo,
                        solicitud.IdEquipo,
                        solicitud.FechaExpiracion,
                        solicitud.MaxUsos
                    }));

                _logger.LogInformation("Enlace de jugador generado - EnlaceId: {EnlaceId}, Codigo: {Codigo}",
                    resultado.Id, resultado.CodigoEnlace);

                return Ok(ApiRespuesta<EnlaceResponse>.Success(resultado, "Enlace de jugador generado exitosamente"));
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
                _logger.LogError(ex, "Error al generar enlace de jugador - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al generar enlace"));
            }
        }

        /// <summary>
        /// Recupera información pública y básica de un enlace mediante su código.
        /// </summary>
        /// <param name="codigoEnlace">El código único del enlace.</param>
        /// <remarks>
        /// Endpoint usado por invitados antes de unirse para ver a qué torneo/equipo han sido invitados.
        /// </remarks>
        [HttpGet("info/{codigoEnlace}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerInfoEnlace(string codigoEnlace)
        {
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de información de enlace público - Codigo: {Codigo}, IP: {Ip}",
                codigoEnlace, ipCliente);

            try
            {
                var info = await _enlaceService.ObtenerInfoEnlaceAsync(codigoEnlace);
                return Ok(ApiRespuesta<EnlaceInfoResponse>.Success(info));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener información de enlace - Codigo: {Codigo}", codigoEnlace);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener información del enlace"));
            }
        }

        /// <summary>
        /// Ejecuta la acción de "unirse" o "usar" un enlace para el usuario autenticado.
        /// </summary>
        /// <param name="codigoEnlace">Código único del enlace.</param>
        /// <remarks>
        /// Valida disponibilidad, fechas y límites de uso antes de asignar el rol correspondiente al usuario.
        /// </remarks>
        /// <response code="200">Rol asignado correctamente.</response>
        /// <response code="400">El enlace ha expirado, está agotado o es inválido.</response>
        [HttpPost("usar/{codigoEnlace}")]
        [Authorize]
        public async Task<IActionResult> UsarEnlace(string codigoEnlace)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Uso de enlace - UsuarioId: {UsuarioId}, Codigo: {Codigo}, IP: {Ip}",
                usuarioId, codigoEnlace, ipCliente);

            try
            {
                var resultado = await _enlaceService.UsarEnlaceAsync(codigoEnlace, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "USAR_ENLACE",
                    "usuarios_roles",
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        CodigoEnlace = codigoEnlace,
                        RolAsignado = resultado.Rol,
                        IdTorneo = resultado.IdTorneo,
                        IdEquipo = resultado.IdEquipo
                    }));

                _logger.LogInformation("Enlace usado exitosamente - UsuarioId: {UsuarioId}, Codigo: {Codigo}, Rol: {Rol}",
                    usuarioId, codigoEnlace, resultado.Rol);

                return Ok(ApiRespuesta<RolAsignadoResponse>.Success(resultado, "Rol asignado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(usuarioId, "USAR_ENLACE", ex.Message, "enlaces_compartidos", null, null, ipCliente, userAgent);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al usar enlace - UsuarioId: {UsuarioId}, Codigo: {Codigo}", usuarioId, codigoEnlace);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al usar el enlace"));
            }
        }

        /// <summary>
        /// Desactiva manualmente un enlace para que no pueda ser usado más.
        /// </summary>
        /// <param name="id">ID del enlace.</param>
        /// <param name="motivo">Opcional. Razón de la desactivación.</param>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,SUB_ADMIN,CAPITAN")]
        public async Task<IActionResult> DesactivarEnlace(int id, [FromQuery] string? motivo = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Desactivación de enlace - EnlaceId: {EnlaceId}, UsuarioId: {UsuarioId}, Motivo: {Motivo}, IP: {Ip}",
                id, usuarioId, motivo ?? "Sin motivo", ipCliente);

            try
            {
                await _enlaceService.DesactivarEnlaceAsync(id, usuarioId, motivo);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "DESACTIVAR_ENLACE",
                    "enlaces_compartidos",
                    id,
                    motivo);

                _logger.LogInformation("Enlace desactivado - EnlaceId: {EnlaceId}, UsuarioId: {UsuarioId}", id, usuarioId);

                return Ok(ApiRespuesta<object>.Success(null, "Enlace desactivado exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar enlace - EnlaceId: {EnlaceId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al desactivar el enlace"));
            }
        }

        /// <summary>
        /// Obtiene el registro histórico de qué usuarios han utilizado un enlace específico.
        /// </summary>
        /// <param name="id">ID del enlace.</param>
        /// <returns>Lista de usos registrados con fechas y usuarios.</returns>
        [HttpGet("{id}/historial")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ObtenerHistorialUsos(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de historial de enlace - EnlaceId: {EnlaceId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var historial = await _enlaceService.ObtenerHistorialUsosAsync(id, usuarioId);

                _logger.LogInformation("Historial consultado - EnlaceId: {EnlaceId}, Usos: {Usos}", id, historial.Count);

                return Ok(ApiRespuesta<List<UsoEnlaceResponse>>.Success(historial));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de enlace - EnlaceId: {EnlaceId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener historial"));
            }
        }

        /// <summary>
        /// Renueva un enlace que ha expirado o se ha quedado sin usos disponibles.
        /// </summary>
        /// <param name="id">ID del enlace.</param>
        /// <param name="nuevaFechaExpiracion">Nueva fecha límite (opcional).</param>
        /// <param name="nuevoMaxUsos">Nuevo límite de usos (opcional).</param>
        [HttpPost("{id}/renovar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,CAPITAN")]
        public async Task<IActionResult> RenovarEnlace(int id, [FromQuery] DateTime? nuevaFechaExpiracion, [FromQuery] int? nuevoMaxUsos)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Renovación de enlace - EnlaceId: {EnlaceId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var resultado = await _enlaceService.RenovarEnlaceAsync(id, usuarioId, nuevaFechaExpiracion, nuevoMaxUsos);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "RENOVAR_ENLACE",
                    "enlaces_compartidos",
                    resultado.Id,
                    System.Text.Json.JsonSerializer.Serialize(new { nuevaFechaExpiracion, nuevoMaxUsos }));

                _logger.LogInformation("Enlace renovado - EnlaceId: {EnlaceId}, NuevaExpiracion: {Expiracion}, NuevoMaxUsos: {MaxUsos}",
                    id, nuevaFechaExpiracion, nuevoMaxUsos);

                return Ok(ApiRespuesta<EnlaceResponse>.Success(resultado, "Enlace renovado exitosamente"));
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
                _logger.LogError(ex, "Error al renovar enlace - EnlaceId: {EnlaceId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al renovar el enlace"));
            }
        }

        #region Métodos Privados

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

        private string ObtenerIpCliente()
        {
            var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip))
            {
                ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            return ip ?? "IP desconocida";
        }

        private string ObtenerUserAgent()
        {
            return Request.Headers["User-Agent"].FirstOrDefault() ?? "User-Agent desconocido";
        }

        #endregion
    }
}