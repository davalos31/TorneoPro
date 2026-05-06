using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorneoPro.API.DTOs.Arbitros;
using TorneoPro.API.DTOs.Arbitros.Request;
using TorneoPro.API.DTOs.Arbitros.Response;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Arbitro;
using TorneoPro.API.Servicios.Interfaces.Auditoria;

namespace TorneoPro.API.Controllers
{
    /// <summary>
    /// Controlador para la gestión integral de árbitros en el sistema.
    /// Permite el registro, asignación a partidos, gestión de disponibilidad y seguimiento de rendimiento.
    /// </summary>
    [Route("api/arbitros")]
    [ApiController]
    [Authorize]
    public class ArbitrosController : ControllerBase
    {
        private readonly IArbitroService _arbitroService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<ArbitrosController> _logger;

        public ArbitrosController(
            IArbitroService arbitroService,
            IAuditoriaService auditoriaService,
            ILogger<ArbitrosController> logger)
        {
            _arbitroService = arbitroService;
            _auditoriaService = auditoriaService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene una lista paginada de árbitros basada en criterios de filtrado.
        /// </summary>
        /// <remarks>Accesible únicamente por roles administrativos.</remarks>
        /// <param name="solicitud">Parámetros de paginación y filtros (nombre, especialidad, etc.).</param>
        /// <returns>Resultado paginado de árbitros.</returns>
        /// <response code="200">Retorna la lista de árbitros según los filtros.</response>
        /// <response code="403">Si el usuario no tiene permisos administrativos.</response>
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,SUB_ADMIN")]
        public async Task<IActionResult> Listar([FromQuery] FiltrarArbitroRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Listado de árbitros - UsuarioId: {UsuarioId}, Pagina: {Pagina}, IP: {Ip}",
                usuarioId, solicitud.Pagina, ipCliente);

            try
            {
                var resultado = await _arbitroService.ObtenerTodosAsync(solicitud);
                return Ok(ApiRespuesta<ResultadoPaginado<ArbitroResponse>>.Success(resultado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar árbitros");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la lista de árbitros"));
            }
        }

        /// <summary>
        /// Recupera el perfil detallado de un árbitro específico.
        /// </summary>
        /// <param name="id">ID único del árbitro.</param>
        /// <returns>Datos del perfil del árbitro.</returns>
        /// <response code="200">Retorna el objeto del árbitro solicitado.</response>
        /// <response code="404">Si el árbitro no existe.</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de árbitro - ArbitroId: {ArbitroId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var arbitro = await _arbitroService.ObtenerPorIdAsync(id);
                if (arbitro == null)
                    return NotFound(ApiRespuesta<object>.Error("Árbitro no encontrado"));

                return Ok(ApiRespuesta<ArbitroResponse>.Success(arbitro));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener árbitro - ArbitroId: {ArbitroId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener el árbitro"));
            }
        }

        /// <summary>
        /// Registra un nuevo árbitro en la plataforma y genera su cuenta de usuario vinculada.
        /// </summary>
        /// <remarks>Requiere rol de SUPER_ADMIN o ADMIN.</remarks>
        /// <param name="solicitud">Datos de registro (credenciales, nombres, etc.).</param>
        /// <returns>Datos del árbitro registrado.</returns>
        /// <response code="200">Registro exitoso.</response>
        /// <response code="400">Si el email ya está en uso o los datos son inválidos.</response>
        [HttpPost("registrar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> RegistrarArbitro([FromBody] RegistrarArbitroRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Registro de árbitro - AdministradorId: {AdminId}, Email: {Email}, IP: {Ip}",
                usuarioId, solicitud.Email, ipCliente);

            try
            {
                var resultado = await _arbitroService.RegistrarArbitroAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "REGISTRAR_ARBITRO",
                    "usuarios",
                    resultado.Id,
                    System.Text.Json.JsonSerializer.Serialize(new { solicitud.Email, solicitud.Nombres, solicitud.Apellidos }));

                _logger.LogInformation("Árbitro registrado exitosamente - ArbitroId: {ArbitroId}, Email: {Email}",
                    resultado.Id, solicitud.Email);

                return Ok(ApiRespuesta<ArbitroResponse>.Success(resultado, "Árbitro registrado exitosamente"));
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
                _logger.LogError(ex, "Error al registrar árbitro - Email: {Email}", solicitud.Email);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al registrar el árbitro"));
            }
        }

        /// <summary>
        /// Actualiza la información personal y profesional de un árbitro.
        /// </summary>
        /// <param name="id">ID del árbitro.</param>
        /// <param name="solicitud">Objeto con los campos a actualizar.</param>
        /// <returns>Datos actualizados del árbitro.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarArbitro(int id, [FromBody] ActualizarArbitroRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Actualización de árbitro - ArbitroId: {ArbitroId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var resultado = await _arbitroService.ActualizarArbitroAsync(id, usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "ACTUALIZAR_ARBITRO",
                    "usuarios",
                    id);

                _logger.LogInformation("Árbitro actualizado exitosamente - ArbitroId: {ArbitroId}", id);

                return Ok(ApiRespuesta<ArbitroResponse>.Success(resultado, "Árbitro actualizado exitosamente"));
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
                _logger.LogError(ex, "Error al actualizar árbitro - ArbitroId: {ArbitroId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar el árbitro"));
            }
        }

        /// <summary>
        /// Asocia a un árbitro con un partido específico en un rol determinado (Principal, Asistente, etc.).
        /// </summary>
        /// <param name="id">ID del árbitro.</param>
        /// <param name="solicitud">Detalles de la asignación (ID Partido y Tipo).</param>
        /// <returns>Confirmación de la asignación.</returns>
        /// <response code="409">Si el árbitro ya tiene otro partido en el mismo horario.</response>
        [HttpPost("{id}/asignar-partido")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> AsignarAPartido(int id, [FromBody] AsignarArbitroRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Asignación de árbitro a partido - ArbitroId: {ArbitroId}, PartidoId: {PartidoId}, Tipo: {Tipo}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, solicitud.IdPartido, solicitud.Tipo, usuarioId, ipCliente);

            try
            {
                await _arbitroService.AsignarAPartidoAsync(id, solicitud, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "ASIGNAR_ARBITRO_PARTIDO",
                    "partidos",
                    solicitud.IdPartido);

                _logger.LogInformation("Árbitro asignado a partido - ArbitroId: {ArbitroId}, PartidoId: {PartidoId}", id, solicitud.IdPartido);

                return Ok(ApiRespuesta<object>.Success(null, "Árbitro asignado exitosamente"));
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
                _logger.LogError(ex, "Error al asignar árbitro a partido");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al asignar el árbitro"));
            }
        }

        /// <summary>
        /// Remueve la asignación de un árbitro de un partido específico.
        /// </summary>
        /// <param name="id">ID del árbitro.</param>
        /// <param name="partidoId">ID del partido.</param>
        /// <returns>Confirmación de la desasignación.</returns>
        [HttpDelete("{id}/desasignar-partido/{partidoId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> DesasignarDePartido(int id, int partidoId)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Desasignación de árbitro de partido - ArbitroId: {ArbitroId}, PartidoId: {PartidoId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, partidoId, usuarioId, ipCliente);

            try
            {
                await _arbitroService.DesasignarDePartidoAsync(id, partidoId, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "DESASIGNAR_ARBITRO_PARTIDO",
                    "partidos",
                    partidoId);

                _logger.LogInformation("Árbitro desasignado de partido - ArbitroId: {ArbitroId}, PartidoId: {PartidoId}", id, partidoId);

                return Ok(ApiRespuesta<object>.Success(null, "Árbitro desasignado exitosamente"));
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
                _logger.LogError(ex, "Error al desasignar árbitro de partido");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al desasignar el árbitro"));
            }
        }

        /// <summary>
        /// Consulta el historial o programación de partidos vinculados a un árbitro.
        /// </summary>
        /// <param name="id">ID del árbitro.</param>
        /// <param name="estado">Opcional. Filtrar por partidos 'Programados', 'Finalizados' o 'Cancelados'.</param>
        /// <returns>Lista de partidos asignados.</returns>
        [HttpGet("{id}/partidos")]
        public async Task<IActionResult> ObtenerPartidosAsignados(int id, [FromQuery] string? estado = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de partidos asignados - ArbitroId: {ArbitroId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var partidos = await _arbitroService.ObtenerPartidosAsignadosAsync(id, estado);
                return Ok(ApiRespuesta<List<PartidoAsignadoResponse>>.Success(partidos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener partidos asignados - ArbitroId: {ArbitroId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener los partidos"));
            }
        }

        /// <summary>
        /// Registra un bloque de tiempo (disponible o no) para el calendario del árbitro.
        /// </summary>
        /// <param name="id">ID del árbitro.</param>
        /// <param name="solicitud">Fecha, hora y estado de disponibilidad.</param>
        /// <returns>Mensaje de éxito.</returns>
        [HttpPost("{id}/disponibilidad")]
        public async Task<IActionResult> RegistrarDisponibilidad(int id, [FromBody] DisponibilidadArbitroRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Registro de disponibilidad - ArbitroId: {ArbitroId}, FechaHora: {FechaHora}, Disponible: {Disponible}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, solicitud.FechaHora, solicitud.Disponible, usuarioId, ipCliente);

            try
            {
                await _arbitroService.RegistrarDisponibilidadAsync(id, solicitud, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "REGISTRAR_DISPONIBILIDAD_ARBITRO",
                    "disponibilidad_arbitros",
                    id);

                return Ok(ApiRespuesta<object>.Success(null, "Disponibilidad registrada exitosamente"));
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
                _logger.LogError(ex, "Error al registrar disponibilidad - ArbitroId: {ArbitroId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al registrar la disponibilidad"));
            }
        }

        /// <summary>
        /// Obtiene la agenda de disponibilidad de un árbitro en un rango de tiempo.
        /// </summary>
        /// <param name="id">ID del árbitro.</param>
        /// <param name="fechaDesde">Fecha inicial del rango.</param>
        /// <param name="fechaHasta">Fecha final del rango.</param>
        /// <returns>Lista de bloques de disponibilidad.</returns>
        [HttpGet("{id}/disponibilidad")]
        public async Task<IActionResult> ObtenerDisponibilidad(int id, [FromQuery] DateTime? fechaDesde = null, [FromQuery] DateTime? fechaHasta = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de disponibilidad - ArbitroId: {ArbitroId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var disponibilidad = await _arbitroService.ObtenerDisponibilidadAsync(id, fechaDesde, fechaHasta);
                return Ok(ApiRespuesta<List<DisponibilidadArbitroResponse>>.Success(disponibilidad));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener disponibilidad - ArbitroId: {ArbitroId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la disponibilidad"));
            }
        }

        /// <summary>
        /// Registra una evaluación cualitativa y cuantitativa sobre el desempeño de un árbitro en un partido.
        /// </summary>
        /// <param name="id">ID del árbitro.</param>
        /// <param name="solicitud">Puntuación y comentarios de la actuación.</param>
        /// <returns>Resumen de la calificación registrada.</returns>
        [HttpPost("{id}/calificar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,SUB_ADMIN")]
        public async Task<IActionResult> CalificarArbitro(int id, [FromBody] CalificacionArbitroRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = ObtenerUserAgent();

            _logger.LogInformation("Calificación de árbitro - ArbitroId: {ArbitroId}, PartidoId: {PartidoId}, Puntuacion: {Puntuacion}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, solicitud.IdPartido, solicitud.Puntuacion, usuarioId, ipCliente);

            try
            {
                var resultado = await _arbitroService.CalificarArbitroAsync(id, solicitud, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "CALIFICAR_ARBITRO",
                    "calificaciones_arbitros",
                    resultado.Id);

                return Ok(ApiRespuesta<CalificacionArbitroResponse>.Success(resultado, "Calificación registrada exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calificar árbitro - ArbitroId: {ArbitroId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al calificar el árbitro"));
            }
        }

        /// <summary>
        /// Obtiene el historial de todas las calificaciones recibidas por el árbitro.
        /// </summary>
        /// <param name="id">ID del árbitro.</param>
        /// <returns>Colección de calificaciones detalladas.</returns>
        [HttpGet("{id}/calificaciones")]
        public async Task<IActionResult> ObtenerCalificaciones(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de calificaciones - ArbitroId: {ArbitroId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var calificaciones = await _arbitroService.ObtenerCalificacionesAsync(id);
                return Ok(ApiRespuesta<List<CalificacionArbitroResponse>>.Success(calificaciones));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener calificaciones - ArbitroId: {ArbitroId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener las calificaciones"));
            }
        }

        /// <summary>
        /// Genera un informe estadístico del árbitro (promedio calificación, partidos dirigidos, tarjetas mostradas, etc.).
        /// </summary>
        /// <param name="id">ID del árbitro.</param>
        /// <returns>Estadísticas acumuladas.</returns>
        [HttpGet("{id}/estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticas(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de estadísticas de árbitro - ArbitroId: {ArbitroId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var estadisticas = await _arbitroService.ObtenerEstadisticasAsync(id);
                return Ok(ApiRespuesta<EstadisticasArbitroResponse>.Success(estadisticas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas - ArbitroId: {ArbitroId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener las estadísticas"));
            }
        }

        /// <summary>
        /// Busca árbitros que no tengan compromisos y estén marcados como disponibles en un horario específico.
        /// </summary>
        /// <param name="fechaHora">Fecha y hora del encuentro.</param>
        /// <param name="especialidad">Opcional. Filtrar por especialidad técnica.</param>
        /// <returns>Lista de árbitros aptos para ser asignados.</returns>
        [HttpGet("disponibles")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,SUB_ADMIN")]
        public async Task<IActionResult> BuscarArbitrosDisponibles([FromQuery] DateTime fechaHora, [FromQuery] string? especialidad = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Búsqueda de árbitros disponibles - FechaHora: {FechaHora}, Especialidad: {Especialidad}, UsuarioId: {UsuarioId}, IP: {Ip}",
                fechaHora, especialidad ?? "Todas", usuarioId, ipCliente);

            try
            {
                var arbitros = await _arbitroService.BuscarArbitrosDisponiblesAsync(fechaHora, especialidad);
                return Ok(ApiRespuesta<List<ArbitroResponse>>.Success(arbitros));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar árbitros disponibles");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al buscar árbitros disponibles"));
            }
        }

        /// <summary>
        /// Elimina un registro de disponibilidad previamente guardado.
        /// </summary>
        /// <param name="id">ID del árbitro.</param>
        /// <param name="fecha">Fecha y hora exacta a remover del calendario.</param>
        /// <returns>Mensaje de éxito.</returns>
        [HttpDelete("{id}/disponibilidad/{fecha}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> EliminarDisponibilidad(int id, DateTime fecha)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Eliminación de disponibilidad - ArbitroId: {ArbitroId}, Fecha: {Fecha}, UsuarioId: {UsuarioId}",
                id, fecha, usuarioId);

            try
            {
                await _arbitroService.EliminarDisponibilidadAsync(id, fecha, usuarioId, ipCliente, ObtenerUserAgent());
                return Ok(ApiRespuesta<object>.Success(null, "Disponibilidad eliminada exitosamente"));
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
                _logger.LogError(ex, "Error al eliminar disponibilidad");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al eliminar la disponibilidad"));
            }
        }



        /// <summary>
        /// Extrae el ID del usuario del Token JWT actual (Claim sub o NameIdentifier).
        /// </summary>
        /// <returns>ID numérico del usuario.</returns>
        /// <exception cref="UnauthorizedAccessException">Si el token es inválido o no contiene el ID.</exception>
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
        /// Obtiene la dirección IP del cliente desde los encabezados de la solicitud.
        /// </summary>
        /// <returns>Dirección IP en formato string.</returns>
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
        /// Obtiene la cadena User-Agent del navegador o cliente que realiza la petición.
        /// </summary>
        /// <returns>Cadena identificadora del cliente.</returns>
        private string ObtenerUserAgent()
        {
            return Request.Headers["User-Agent"].FirstOrDefault() ?? "User-Agent desconocido";
        }
    }
}