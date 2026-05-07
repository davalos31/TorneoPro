using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using TorneoPro.API.DTOs.Canchas;
using TorneoPro.API.DTOs.Canchas.Response;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Cancha;

namespace TorneoPro.API.Controllers
{
    [Route("api/canchas")]
    [ApiController]
    [Authorize]
    public class CanchasController : ControllerBase
    {
        private readonly ICanchaService _canchaService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<CanchasController> _logger;

        public CanchasController(
            ICanchaService canchaService,
            IAuditoriaService auditoriaService,
            ILogger<CanchasController> logger)
        {
            _canchaService = canchaService;
            _auditoriaService = auditoriaService;
            _logger = logger;
        }

        #region CRUD Principal

        /// <summary>
        /// Obtiene una lista paginada de canchas aplicando filtros de búsqueda
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] FiltrarCanchaRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Listado de canchas - UsuarioId: {UsuarioId}, Pagina: {Pagina}, IP: {Ip}",
                usuarioId, solicitud.Pagina, ipCliente);

            try
            {
                var resultado = await _canchaService.ObtenerTodosAsync(solicitud);
                return Ok(ApiRespuesta<ResultadoPaginado<CanchaResponse>>.Success(resultado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar canchas");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la lista de canchas"));
            }
        }

        /// <summary>
        /// Obtiene la información detallada de una cancha específica
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de cancha - CanchaId: {CanchaId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var cancha = await _canchaService.ObtenerPorIdAsync(id);
                if (cancha == null)
                    return NotFound(ApiRespuesta<object>.Error("Cancha no encontrada"));

                return Ok(ApiRespuesta<CanchaDetalleResponse>.Success(cancha));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cancha - CanchaId: {CanchaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la cancha"));
            }
        }

        /// <summary>
        /// Crea un nuevo registro de cancha
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> Crear([FromBody] CrearCanchaRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Creación de cancha - UsuarioId: {UsuarioId}, Nombre: {Nombre}, IP: {Ip}",
                usuarioId, solicitud.Nombre, ipCliente);

            try
            {
                var resultado = await _canchaService.CrearAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "CREAR_CANCHA",
                    "canchas",
                    resultado.Id,
                    JsonSerializer.Serialize(new { solicitud.Nombre, solicitud.Ciudad, solicitud.IdTipoSuperficie }));

                return Ok(ApiRespuesta<CanchaResponse>.Success(resultado, "Cancha creada exitosamente"));
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
                _logger.LogError(ex, "Error al crear cancha");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al crear la cancha"));
            }
        }

        /// <summary>
        /// Actualiza la información de una cancha existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarCanchaRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Actualización de cancha - CanchaId: {CanchaId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var resultado = await _canchaService.ActualizarAsync(id, usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "ACTUALIZAR_CANCHA",
                    "canchas",
                    id);

                return Ok(ApiRespuesta<CanchaResponse>.Success(resultado, "Cancha actualizada exitosamente"));
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
                _logger.LogError(ex, "Error al actualizar cancha - CanchaId: {CanchaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar la cancha"));
            }
        }

        /// <summary>
        /// Desactiva una cancha (borrado lógico)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> Desactivar(int id, [FromQuery] string? motivo = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Desactivación de cancha - CanchaId: {CanchaId}, UsuarioId: {UsuarioId}, Motivo: {Motivo}, IP: {Ip}",
                id, usuarioId, motivo ?? "Sin motivo", ipCliente);

            try
            {
                await _canchaService.DesactivarAsync(id, usuarioId, motivo, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "DESACTIVAR_CANCHA",
                    "canchas",
                    id);

                return Ok(ApiRespuesta<object>.Success(null, "Cancha desactivada exitosamente"));
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
                _logger.LogError(ex, "Error al desactivar cancha - CanchaId: {CanchaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al desactivar la cancha"));
            }
        }

        /// <summary>
        /// Reactiva una cancha desactivada
        /// </summary>
        [HttpPost("{id}/activar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> Activar(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Activación de cancha - CanchaId: {CanchaId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                await _canchaService.ActivarAsync(id, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "ACTIVAR_CANCHA",
                    "canchas",
                    id);

                return Ok(ApiRespuesta<object>.Success(null, "Cancha activada exitosamente"));
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
                _logger.LogError(ex, "Error al activar cancha - CanchaId: {CanchaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al activar la cancha"));
            }
        }

        #endregion

        #region Gestión de Fotos

        /// <summary>
        /// Sube una fotografía principal a la cancha
        /// </summary>
        [HttpPost("{id}/foto")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> SubirFoto(int id, [FromForm] IFormFile foto)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Subida de foto de cancha - CanchaId: {CanchaId}, UsuarioId: {UsuarioId}, Archivo: {Archivo}, IP: {Ip}",
                id, usuarioId, foto?.FileName, ipCliente);

            try
            {
                if (foto == null || foto.Length == 0)
                    return BadRequest(ApiRespuesta<object>.Error("La foto es requerida"));

                var url = await _canchaService.SubirFotoAsync(id, usuarioId, foto, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "SUBIR_FOTO_CANCHA",
                    "canchas",
                    id,
                    JsonSerializer.Serialize(new { Archivo = foto.FileName, Tamaño = foto.Length }));

                return Ok(ApiRespuesta<object>.Success(new { Url = url }, "Foto subida exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir foto - CanchaId: {CanchaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al subir la foto"));
            }
        }

        /// <summary>
        /// Reemplaza la fotografía actual de una cancha
        /// </summary>
        [HttpPut("{id}/foto")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ActualizarFoto(int id, [FromForm] IFormFile foto)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Actualización de foto de cancha - CanchaId: {CanchaId}, UsuarioId: {UsuarioId}, Archivo: {Archivo}, IP: {Ip}",
                id, usuarioId, foto?.FileName, ipCliente);

            try
            {
                if (foto == null || foto.Length == 0)
                    return BadRequest(ApiRespuesta<object>.Error("La foto es requerida"));

                var url = await _canchaService.ActualizarFotoAsync(id, usuarioId, foto, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "ACTUALIZAR_FOTO_CANCHA",
                    "canchas",
                    id,
                    JsonSerializer.Serialize(new { Archivo = foto.FileName, Tamaño = foto.Length }));

                return Ok(ApiRespuesta<object>.Success(new { Url = url }, "Foto actualizada exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar foto - CanchaId: {CanchaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar la foto"));
            }
        }

        /// <summary>
        /// Elimina la fotografía asociada a una cancha
        /// </summary>
        [HttpDelete("{id}/foto")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> EliminarFoto(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Eliminación de foto de cancha - CanchaId: {CanchaId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                await _canchaService.EliminarFotoAsync(id, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "ELIMINAR_FOTO_CANCHA",
                    "canchas",
                    id);

                return Ok(ApiRespuesta<object>.Success(null, "Foto eliminada exitosamente"));
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
                _logger.LogError(ex, "Error al eliminar foto - CanchaId: {CanchaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al eliminar la foto"));
            }
        }

        #endregion

        #region Disponibilidad y Bloqueos

        /// <summary>
        /// Consulta los intervalos de tiempo disponibles de una cancha
        /// </summary>
        [HttpGet("{id}/disponibilidad")]
        public async Task<IActionResult> ObtenerDisponibilidad(int id, [FromQuery] DateTime fechaInicio, [FromQuery] DateTime fechaFin)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de disponibilidad - CanchaId: {CanchaId}, FechaInicio: {FechaInicio}, FechaFin: {FechaFin}, IP: {Ip}",
                id, fechaInicio, fechaFin, ipCliente);

            try
            {
                var disponibilidad = await _canchaService.ObtenerDisponibilidadAsync(id, fechaInicio, fechaFin);
                return Ok(ApiRespuesta<List<DisponibilidadResponse>>.Success(disponibilidad));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener disponibilidad - CanchaId: {CanchaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la disponibilidad"));
            }
        }

        /// <summary>
        /// Obtiene disponibilidad horaria de una cancha para un día específico
        /// </summary>
        [HttpGet("{id}/disponibilidad/horaria")]
        public async Task<IActionResult> ObtenerDisponibilidadHoraria(int id, [FromQuery] DateTime fecha, [FromQuery] int duracionMinutos = 60)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de disponibilidad horaria - CanchaId: {CanchaId}, Fecha: {Fecha}, Duracion: {Duracion}min, IP: {Ip}",
                id, fecha, duracionMinutos, ipCliente);

            try
            {
                var disponibilidad = await _canchaService.ObtenerDisponibilidadHorariaAsync(id, fecha, duracionMinutos);
                return Ok(ApiRespuesta<List<DisponibilidadHorariaResponse>>.Success(disponibilidad));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener disponibilidad horaria - CanchaId: {CanchaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la disponibilidad horaria"));
            }
        }

        /// <summary>
        /// Bloquea un rango horario de una cancha
        /// </summary>
        [HttpPost("{id}/bloquear")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> BloquearFecha(int id, [FromBody] BloquearFechaRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Bloqueo de fecha - CanchaId: {CanchaId}, UsuarioId: {UsuarioId}, FechaInicio: {FechaInicio}, FechaFin: {FechaFin}, IP: {Ip}",
                id, usuarioId, solicitud.FechaInicio, solicitud.FechaFin, ipCliente);

            try
            {
                var resultado = await _canchaService.BloquearFechaAsync(id, usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "BLOQUEAR_FECHA_CANCHA",
                    "fechas_bloqueadas",
                    resultado.Id);

                return Ok(ApiRespuesta<BloqueoResponse>.Success(resultado, "Fecha bloqueada exitosamente"));
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
                _logger.LogError(ex, "Error al bloquear fecha - CanchaId: {CanchaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al bloquear la fecha"));
            }
        }

        /// <summary>
        /// Elimina un bloqueo de horario
        /// </summary>
        [HttpDelete("{canchaId}/bloquear/{bloqueoId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> EliminarBloqueo(int canchaId, int bloqueoId)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Eliminación de bloqueo - CanchaId: {CanchaId}, BloqueoId: {BloqueoId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                canchaId, bloqueoId, usuarioId, ipCliente);

            try
            {
                await _canchaService.EliminarBloqueoAsync(canchaId, bloqueoId, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "ELIMINAR_BLOQUEO_CANCHA",
                    "fechas_bloqueadas",
                    bloqueoId);

                return Ok(ApiRespuesta<object>.Success(null, "Bloqueo eliminado exitosamente"));
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
                _logger.LogError(ex, "Error al eliminar bloqueo - CanchaId: {CanchaId}", canchaId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al eliminar el bloqueo"));
            }
        }

        #endregion

        #region Búsqueda y Catálogos

        /// <summary>
        /// Busca canchas disponibles en un rango de fecha y hora
        /// </summary>
        [HttpGet("disponibles")]
        public async Task<IActionResult> BuscarCanchasDisponibles([FromQuery] BuscarCanchaRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Búsqueda de canchas disponibles - FechaInicio: {FechaInicio}, FechaFin: {FechaFin}, IP: {Ip}",
                solicitud.FechaHoraInicio, solicitud.FechaHoraFin, ipCliente);

            try
            {
                var canchas = await _canchaService.BuscarCanchasDisponiblesAsync(solicitud);
                return Ok(ApiRespuesta<List<CanchaResponse>>.Success(canchas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar canchas disponibles");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al buscar canchas disponibles"));
            }
        }

        /// <summary>
        /// Obtiene los tipos de superficie disponibles (catálogo)
        /// </summary>
        [HttpGet("tipos-superficie")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerTiposSuperficie()
        {
            try
            {
                var tipos = await _canchaService.ObtenerTiposSuperficieAsync();
                return Ok(ApiRespuesta<object>.Success(tipos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de superficie");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener tipos de superficie"));
            }
        }

        #endregion

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

        #endregion
    }
}