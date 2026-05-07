using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using TorneoPro.API.DTOs.Equipo.Request;
using TorneoPro.API.DTOs.Equipo.Response;
using TorneoPro.API.DTOs.Equipos;
using TorneoPro.API.DTOs.Equipos.Request;
using TorneoPro.API.DTOs.Equipos.Response;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Equipo;

namespace TorneoPro.API.Controllers
{
    [Route("api/equipos")]
    [ApiController]
    [Authorize]
    public class EquiposController : ControllerBase
    {
        #region ========== CAMPOS Y CONSTRUCTOR ==========

        private readonly IEquipoService _equipoService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<EquiposController> _logger;

        public EquiposController(
            IEquipoService equipoService,
            IAuditoriaService auditoriaService,
            ILogger<EquiposController> logger)
        {
            _equipoService = equipoService;
            _auditoriaService = auditoriaService;
            _logger = logger;
        }

        #endregion

        #region ========== CRUD PRINCIPAL DE EQUIPOS ==========

        /// <summary>
        /// Obtiene una lista paginada de equipos con filtros avanzados.
        /// </summary>
        /// <param name="solicitud">Filtros: Búsqueda, ciudad, estado, verificado, torneo, jugador.</param>
        /// <returns>Lista paginada de equipos.</returns>
        /// <response code="200">Lista obtenida correctamente.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="500">Error interno.</response>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] EquipoFilterRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Listado de equipos - UsuarioId: {UsuarioId}, Pagina: {Pagina}, IP: {Ip}",
                usuarioId, solicitud.Pagina, ipCliente);

            try
            {
                var resultado = await _equipoService.ObtenerTodosAsync(solicitud);
                return Ok(ApiRespuesta<ResultadoPaginado<EquipoResponse>>.Success(resultado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar equipos");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la lista de equipos"));
            }
        }

        /// <summary>
        /// Obtiene los detalles completos de un equipo específico por su ID.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <returns>Detalles del equipo incluyendo jugadores y torneos.</returns>
        /// <response code="200">Equipo encontrado.</response>
        /// <response code="404">Equipo no encontrado.</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de equipo - EquipoId: {EquipoId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var equipo = await _equipoService.ObtenerPorIdAsync(id);
                if (equipo == null)
                    return NotFound(ApiRespuesta<object>.Error("Equipo no encontrado"));

                return Ok(ApiRespuesta<EquipoDetalleResponse>.Success(equipo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener equipo - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener el equipo"));
            }
        }

        /// <summary>
        /// Crea un nuevo equipo. El creador se convierte automáticamente en capitán.
        /// </summary>
        /// <param name="solicitud">Datos del equipo (nombre, ciudad, colores, etc.).</param>
        /// <returns>Equipo creado con su información.</returns>
        /// <response code="200">Equipo creado exitosamente.</response>
        /// <response code="400">Nombre duplicado o datos inválidos.</response>
        /// <response code="401">No autorizado.</response>
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearEquipoRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Creación de equipo - UsuarioId: {UsuarioId}, Nombre: {Nombre}, IP: {Ip}",
                usuarioId, solicitud.Nombre, ipCliente);

            try
            {
                var resultado = await _equipoService.CrearAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "CREAR_EQUIPO",
                    "equipos",
                    resultado.Id,
                    JsonSerializer.Serialize(new { solicitud.Nombre, solicitud.Ciudad }));

                return Ok(ApiRespuesta<EquipoResponse>.Success(resultado, "Equipo creado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear equipo");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al crear el equipo"));
            }
        }

        /// <summary>
        /// Actualiza la información de un equipo existente.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="solicitud">Datos a actualizar.</param>
        /// <returns>Equipo actualizado.</returns>
        /// <response code="200">Equipo actualizado.</response>
        /// <response code="403">Sin permisos (no eres capitán ni admin).</response>
        /// <response code="404">Equipo no encontrado.</response>
        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarEquipoRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Actualización de equipo - EquipoId: {EquipoId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var resultado = await _equipoService.ActualizarAsync(id, usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "ACTUALIZAR_EQUIPO", "equipos", id);

                return Ok(ApiRespuesta<EquipoResponse>.Success(resultado, "Equipo actualizado exitosamente"));
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
                _logger.LogError(ex, "Error al actualizar equipo - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar el equipo"));
            }
        }

        #endregion

        #region ========== GESTIÓN DE ESCUDOS ==========

        /// <summary>
        /// Sube el escudo del equipo por primera vez.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="archivo">Archivo de imagen (jpg, png, gif).</param>
        /// <returns>URL del escudo subido.</returns>
        [HttpPost("{id}/escudo")]
        public async Task<IActionResult> SubirEscudo(int id, IFormFile archivo)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Subida de escudo - EquipoId: {EquipoId}, UsuarioId: {UsuarioId}, Archivo: {Archivo}",
                id, usuarioId, archivo?.FileName);

            try
            {
                if (archivo == null || archivo.Length == 0)
                    return BadRequest(ApiRespuesta<object>.Error("El archivo es requerido"));

                var url = await _equipoService.SubirEscudoAsync(id, usuarioId, archivo, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "SUBIR_ESCUDO_EQUIPO", "equipos", id,
                    JsonSerializer.Serialize(new { Archivo = archivo.FileName, Tamaño = archivo.Length }));

                return Ok(ApiRespuesta<object>.Success(new { Url = url }, "Escudo subido exitosamente"));
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
                _logger.LogError(ex, "Error al subir escudo - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al subir el escudo"));
            }
        }

        /// <summary>
        /// Reemplaza el escudo actual del equipo por uno nuevo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="archivo">Nuevo archivo de imagen.</param>
        /// <returns>URL del nuevo escudo.</returns>
        [HttpPut("{id}/escudo")]
        public async Task<IActionResult> ActualizarEscudo(int id, IFormFile archivo)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Actualización de escudo - EquipoId: {EquipoId}, UsuarioId: {UsuarioId}, Archivo: {Archivo}",
                id, usuarioId, archivo?.FileName);

            try
            {
                if (archivo == null || archivo.Length == 0)
                    return BadRequest(ApiRespuesta<object>.Error("El archivo es requerido"));

                var url = await _equipoService.ActualizarEscudoAsync(id, usuarioId, archivo, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "ACTUALIZAR_ESCUDO_EQUIPO", "equipos", id,
                    JsonSerializer.Serialize(new { Archivo = archivo.FileName, Tamaño = archivo.Length }));

                return Ok(ApiRespuesta<object>.Success(new { Url = url }, "Escudo actualizado exitosamente"));
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
                _logger.LogError(ex, "Error al actualizar escudo - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar el escudo"));
            }
        }

        /// <summary>
        /// Elimina el escudo del equipo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <returns>Confirmación de eliminación.</returns>
        [HttpDelete("{id}/escudo")]
        public async Task<IActionResult> EliminarEscudo(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Eliminación de escudo - EquipoId: {EquipoId}, UsuarioId: {UsuarioId}",
                id, usuarioId);

            try
            {
                await _equipoService.EliminarEscudoAsync(id, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "ELIMINAR_ESCUDO_EQUIPO", "equipos", id);

                return Ok(ApiRespuesta<object>.Success(null, "Escudo eliminado exitosamente"));
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
                _logger.LogError(ex, "Error al eliminar escudo - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al eliminar el escudo"));
            }
        }

        #endregion

        #region ========== TORNEOS E INSCRIPCIONES ==========

        /// <summary>
        /// Inscribe un equipo en un torneo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="torneoId">ID del torneo.</param>
        /// <returns>Detalle de la inscripción.</returns>
        [HttpPost("{id}/inscribir/{torneoId}")]
        public async Task<IActionResult> InscribirEnTorneo(int id, int torneoId)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Inscripción en torneo - EquipoId: {EquipoId}, TorneoId: {TorneoId}",
                id, torneoId);

            try
            {
                var resultado = await _equipoService.InscribirEnTorneoAsync(id, torneoId, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "INSCRIBIR_EQUIPO_TORNEO", "equipos_torneos", resultado.Id);

                return Ok(ApiRespuesta<EquipoTorneoResponse>.Success(resultado, "Equipo inscrito exitosamente"));
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
                _logger.LogError(ex, "Error al inscribir equipo en torneo");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al inscribir el equipo"));
            }
        }

        /// <summary>
        /// Aprobar inscripción de equipo en torneo (solo administradores).
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="torneoId">ID del torneo.</param>
        /// <returns>Confirmación de aprobación.</returns>
        [HttpPut("{id}/aprobar/{torneoId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> AprobarInscripcion(int id, int torneoId)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Aprobación de inscripción - EquipoId: {EquipoId}, TorneoId: {TorneoId}",
                id, torneoId);

            try
            {
                await _equipoService.AprobarInscripcionAsync(id, torneoId, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "APROBAR_INSCRIPCION_EQUIPO", "equipos_torneos", null);

                return Ok(ApiRespuesta<object>.Success(null, "Inscripción aprobada exitosamente"));
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
                _logger.LogError(ex, "Error al aprobar inscripción");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al aprobar la inscripción"));
            }
        }

        /// <summary>
        /// Obtiene los equipos participantes en un torneo específico.
        /// </summary>
        /// <param name="torneoId">ID del torneo.</param>
        /// <returns>Lista de equipos.</returns>
        [HttpGet("torneo/{torneoId}")]
        public async Task<IActionResult> ObtenerEquiposPorTorneo(int torneoId)
        {
            try
            {
                var equipos = await _equipoService.ObtenerEquiposPorTorneoAsync(torneoId);
                return Ok(ApiRespuesta<List<EquipoResponse>>.Success(equipos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener equipos por torneo - TorneoId: {TorneoId}", torneoId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener equipos"));
            }
        }

        #endregion

        #region ========== GESTIÓN DE JUGADORES ==========

        /// <summary>
        /// Obtiene la lista de jugadores de un equipo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <returns>Lista de jugadores con sus datos.</returns>
        [HttpGet("{id}/jugadores")]
        public async Task<IActionResult> ObtenerJugadores(int id)
        {
            try
            {
                var jugadores = await _equipoService.ObtenerJugadoresAsync(id);
                return Ok(ApiRespuesta<List<JugadorEquipoResponse>>.Success(jugadores));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener jugadores - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener los jugadores"));
            }
        }

        /// <summary>
        /// Agrega un jugador existente al equipo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="solicitud">Datos del jugador (ID, número, posición, capitán).</param>
        /// <returns>Jugador agregado.</returns>
        [HttpPost("{id}/jugadores")]
        public async Task<IActionResult> AgregarJugador(int id, [FromBody] AgregarJugadorRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Agregar jugador - EquipoId: {EquipoId}, JugadorId: {JugadorId}",
                id, solicitud.IdJugador);

            try
            {
                var resultado = await _equipoService.AgregarJugadorAsync(id, usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "AGREGAR_JUGADOR_EQUIPO", "jugadores_equipos", resultado.Id);

                return Ok(ApiRespuesta<JugadorEquipoResponse>.Success(resultado, "Jugador agregado exitosamente"));
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
                _logger.LogError(ex, "Error al agregar jugador");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al agregar el jugador"));
            }
        }

        /// <summary>
        /// Actualiza los datos de un jugador dentro del equipo (número, posición, capitán).
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="jugadorId">ID del jugador.</param>
        /// <param name="solicitud">Datos a actualizar.</param>
        /// <returns>Jugador actualizado.</returns>
        [HttpPut("{id}/jugadores/{jugadorId}")]
        public async Task<IActionResult> ActualizarJugador(int id, int jugadorId, [FromBody] ActualizarJugadorRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Actualizar jugador - EquipoId: {EquipoId}, JugadorId: {JugadorId}",
                id, jugadorId);

            try
            {
                var resultado = await _equipoService.ActualizarJugadorAsync(id, jugadorId, usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "ACTUALIZAR_JUGADOR_EQUIPO", "jugadores_equipos", resultado.Id);

                return Ok(ApiRespuesta<JugadorEquipoResponse>.Success(resultado, "Jugador actualizado exitosamente"));
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
                _logger.LogError(ex, "Error al actualizar jugador");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar el jugador"));
            }
        }

        /// <summary>
        /// Remueve un jugador del equipo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="jugadorId">ID del jugador a remover.</param>
        /// <returns>Confirmación de remoción.</returns>
        [HttpDelete("{id}/jugadores/{jugadorId}")]
        public async Task<IActionResult> RemoverJugador(int id, int jugadorId)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Remover jugador - EquipoId: {EquipoId}, JugadorId: {JugadorId}",
                id, jugadorId);

            try
            {
                await _equipoService.RemoverJugadorAsync(id, jugadorId, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "REMOVER_JUGADOR_EQUIPO", "jugadores_equipos", null);

                return Ok(ApiRespuesta<object>.Success(null, "Jugador removido exitosamente"));
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
                _logger.LogError(ex, "Error al remover jugador");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al remover el jugador"));
            }
        }

        /// <summary>
        /// Cambia el capitán del equipo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="nuevoCapitanId">ID del nuevo capitán.</param>
        /// <returns>Confirmación del cambio.</returns>
        [HttpPut("{id}/capitan/{nuevoCapitanId}")]
        public async Task<IActionResult> CambiarCapitan(int id, int nuevoCapitanId)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Cambiar capitán - EquipoId: {EquipoId}, NuevoCapitanId: {NuevoCapitanId}",
                id, nuevoCapitanId);

            try
            {
                await _equipoService.CambiarCapitanAsync(id, nuevoCapitanId, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "CAMBIAR_CAPITAN_EQUIPO", "equipos", id);

                return Ok(ApiRespuesta<object>.Success(null, "Capitán cambiado exitosamente"));
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
                _logger.LogError(ex, "Error al cambiar capitán");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al cambiar el capitán"));
            }
        }

        /// <summary>
        /// Autoriza a un jugador en el equipo (validación de documentación).
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="jugadorId">ID del jugador a autorizar.</param>
        /// <returns>Confirmación de autorización.</returns>
        [HttpPost("{id}/autorizar/{jugadorId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> AutorizarJugador(int id, int jugadorId)
        {
            var usuarioId = ObtenerUsuarioActualId();

            try
            {
                await _equipoService.AutorizarJugadorAsync(id, jugadorId, usuarioId);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "AUTORIZAR_JUGADOR_EQUIPO", "jugadores_equipos", jugadorId);

                return Ok(ApiRespuesta<object>.Success(null, "Jugador autorizado exitosamente"));
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
                _logger.LogError(ex, "Error al autorizar jugador");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al autorizar el jugador"));
            }
        }

        #endregion

        #region ========== SUSPENSIONES ==========

        /// <summary>
        /// Suspende a un jugador del equipo por N partidos.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="jugadorId">ID del jugador a suspender.</param>
        /// <param name="solicitud">Detalles de la suspensión (motivo, partidos, torneo).</param>
        /// <returns>Detalle de la suspensión.</returns>
        [HttpPost("{id}/suspender/{jugadorId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> SuspenderJugador(int id, int jugadorId, [FromBody] SuspenderJugadorRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Suspender jugador - EquipoId: {EquipoId}, JugadorId: {JugadorId}",
                id, jugadorId);

            try
            {
                var resultado = await _equipoService.SuspenderJugadorAsync(id, jugadorId, usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "SUSPENDER_JUGADOR", "jugadores_suspensiones", resultado.Id);

                return Ok(ApiRespuesta<SuspensionResponse>.Success(resultado, "Jugador suspendido exitosamente"));
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
                _logger.LogError(ex, "Error al suspender jugador");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al suspender el jugador"));
            }
        }

        #endregion

        #region ========== HISTORIAL Y ESTADÍSTICAS ==========

        /// <summary>
        /// Obtiene el historial de torneos en los que participó el equipo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <returns>Lista de torneos con resultados.</returns>
        [HttpGet("{id}/historial")]
        public async Task<IActionResult> ObtenerHistorial(int id)
        {
            try
            {
                var historial = await _equipoService.ObtenerHistorialAsync(id);
                return Ok(ApiRespuesta<List<HistorialTorneoResponse>>.Success(historial));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener el historial"));
            }
        }

        /// <summary>
        /// Obtiene estadísticas del equipo (partidos, goles, tarjetas, etc.).
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="idTorneo">Filtrar por torneo específico.</param>
        /// <returns>Estadísticas del equipo.</returns>
        [HttpGet("{id}/estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticasEquipo(int id, [FromQuery] int? idTorneo = null)
        {
            try
            {
                var estadisticas = await _equipoService.ObtenerEstadisticasAsync(id, idTorneo);
                return Ok(ApiRespuesta<EquipoEstadisticasResponse>.Success(estadisticas));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener estadísticas"));
            }
        }

        /// <summary>
        /// Obtiene el calendario de próximos partidos del equipo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="idTorneo">Filtrar por torneo.</param>
        /// <param name="limite">Cantidad de partidos a mostrar.</param>
        /// <returns>Lista de próximos partidos.</returns>
        [HttpGet("{id}/calendario")]
        public async Task<IActionResult> ObtenerCalendarioEquipo(int id, [FromQuery] int? idTorneo = null, [FromQuery] int limite = 10)
        {
            try
            {
                var calendario = await _equipoService.ObtenerCalendarioAsync(id, idTorneo, limite);
                return Ok(ApiRespuesta<List<PartidoEquipoResponse>>.Success(calendario));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener calendario - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener calendario"));
            }
        }

        /// <summary>
        /// Obtiene las estadísticas de los jugadores del equipo en un torneo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="idTorneo">ID del torneo.</param>
        /// <returns>Estadísticas de jugadores.</returns>
        [HttpGet("{id}/jugadores/estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticasJugadores(int id, [FromQuery] int idTorneo)
        {
            try
            {
                var estadisticas = await _equipoService.ObtenerEstadisticasJugadoresAsync(id, idTorneo);
                return Ok(ApiRespuesta<List<JugadorEstadisticaEquipoResponse>>.Success(estadisticas));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de jugadores - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener estadísticas"));
            }
        }

        /// <summary>
        /// Valida si un número de camiseta está disponible en el equipo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="numero">Número de camiseta a validar.</param>
        /// <param name="jugadorExcluirId">ID del jugador a excluir (para ediciones).</param>
        /// <returns>Disponibilidad del número.</returns>
        [HttpGet("{id}/validar-camiseta/{numero}")]
        public async Task<IActionResult> ValidarNumeroCamiseta(int id, int numero, [FromQuery] int? jugadorExcluirId = null)
        {
            try
            {
                var disponible = await _equipoService.ValidarNumeroCamisetaAsync(id, numero, jugadorExcluirId);
                return Ok(ApiRespuesta<object>.Success(new { Disponible = disponible }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar número de camiseta - EquipoId: {EquipoId}, Numero: {Numero}", id, numero);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al validar número de camiseta"));
            }
        }

        #endregion

        #region ========== SOLICITUDES DE UNIÓN ==========

        /// <summary>
        /// Solicita unirse a un equipo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="solicitud">Mensaje opcional.</param>
        /// <returns>Detalle de la solicitud.</returns>
        [HttpPost("{id}/solicitar-union")]
        public async Task<IActionResult> SolicitarUnion(int id, [FromBody] SolicitarUnionRequest? solicitud = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                var resultado = await _equipoService.SolicitarUnionAsync(id, usuarioId, solicitud?.Mensaje, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "SOLICITAR_UNION_EQUIPO", "solicitudes_equipos", resultado.Id);

                return Ok(ApiRespuesta<SolicitudUnionResponse>.Success(resultado, "Solicitud enviada exitosamente"));
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
                _logger.LogError(ex, "Error al solicitar unión - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al solicitar unión"));
            }
        }

        /// <summary>
        /// Procesa una solicitud de unión (aprobar/rechazar). Solo capitán o admin.
        /// </summary>
        /// <param name="solicitudId">ID de la solicitud.</param>
        /// <param name="request">Indica si se aprueba y comentario opcional.</param>
        /// <returns>Confirmación del procesamiento.</returns>
        [HttpPut("solicitudes/{solicitudId}/procesar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,CAPITAN")]
        public async Task<IActionResult> ProcesarSolicitudUnion(int solicitudId, [FromBody] ProcesarSolicitudUnionRequest request)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                await _equipoService.ProcesarSolicitudUnionAsync(solicitudId, usuarioId, request.Aprobar, request.Comentario, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    request.Aprobar ? "APROBAR_SOLICITUD_EQUIPO" : "RECHAZAR_SOLICITUD_EQUIPO",
                    "solicitudes_equipos",
                    solicitudId,
                    request.Comentario);

                var mensaje = request.Aprobar ? "Solicitud aprobada exitosamente" : "Solicitud rechazada";
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar solicitud de unión - SolicitudId: {SolicitudId}", solicitudId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al procesar solicitud"));
            }
        }

        /// <summary>
        /// Obtiene las invitaciones pendientes del equipo.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <returns>Lista de invitaciones.</returns>
        [HttpGet("{id}/invitaciones")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,CAPITAN")]
        public async Task<IActionResult> ObtenerInvitacionesPendientes(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();

            try
            {
                var invitaciones = await _equipoService.ObtenerInvitacionesPendientesAsync(id, usuarioId);
                return Ok(ApiRespuesta<List<InvitacionEquipoResponse>>.Success(invitaciones));
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
                _logger.LogError(ex, "Error al obtener invitaciones pendientes - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener invitaciones"));
            }
        }

        #endregion

        #region ========== ACTIVAR/DESACTIVAR EQUIPO ==========

        /// <summary>
        /// Desactiva un equipo (solo administradores).
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <param name="motivo">Motivo de desactivación.</param>
        /// <returns>Confirmación de desactivación.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> DesactivarEquipo(int id, [FromQuery] string? motivo = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                await _equipoService.DesactivarEquipoAsync(id, usuarioId, motivo, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "DESACTIVAR_EQUIPO", "equipos", id, motivo);

                return Ok(ApiRespuesta<object>.Success(null, "Equipo desactivado exitosamente"));
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
                _logger.LogError(ex, "Error al desactivar equipo - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al desactivar el equipo"));
            }
        }

        /// <summary>
        /// Reactiva un equipo previamente desactivado.
        /// </summary>
        /// <param name="id">ID del equipo.</param>
        /// <returns>Confirmación de activación.</returns>
        [HttpPost("{id}/activar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ActivarEquipo(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                await _equipoService.ActivarEquipoAsync(id, usuarioId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "ACTIVAR_EQUIPO", "equipos", id);

                return Ok(ApiRespuesta<object>.Success(null, "Equipo activado exitosamente"));
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
                _logger.LogError(ex, "Error al activar equipo - EquipoId: {EquipoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al activar el equipo"));
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