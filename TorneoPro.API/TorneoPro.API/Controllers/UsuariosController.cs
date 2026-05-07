using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorneoPro.API.DTOs.Jugadores.Request;
using TorneoPro.API.DTOs.Jugadores.Response;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.DTOs.Usuarios.Request;
using TorneoPro.API.DTOs.Usuarios.Response;
using TorneoPro.API.Servicios.Implementaciones.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Jugador;
using TorneoPro.API.Servicios.Interfaces.Notificacion;
using TorneoPro.API.Servicios.Interfaces.Usuarios;

namespace TorneoPro.API.Controllers
{
    [Route("api/usuarios")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        #region ========== CAMPOS Y CONSTRUCTOR ==========

        private readonly IUsuarioService _usuarioService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<UsuariosController> _logger;
        private readonly IJugadorService _jugadorService;
        private readonly INotificacionService _notificacionService;

        public UsuariosController(
            IJugadorService jugadorService,
            IUsuarioService usuarioService,
            IAuditoriaService auditoriaService,
            ILogger<UsuariosController> logger,
            INotificacionService notificacionService)
        {
            _usuarioService = usuarioService;
            _auditoriaService = auditoriaService;
            _logger = logger;
            _jugadorService = jugadorService;
            _notificacionService = notificacionService;
        }

        #endregion

        #region ========== CRUD PRINCIPAL DE USUARIOS ==========

        /// <summary>
        /// Obtiene una lista paginada de todos los usuarios registrados en el sistema.
        /// </summary>
        /// <param name="solicitud">Parámetros de paginación (Página y Tamaño).</param>
        /// <returns>Resultado paginado con información básica de los usuarios.</returns>
        /// <response code="200">Retorna la lista de usuarios correctamente.</response>
        /// <response code="401">No autorizado - Token no válido o expirado.</response>
        /// <response code="403">Acceso denegado - Se requieren roles administrativos.</response>
        /// <response code="500">Error interno al procesar la solicitud.</response>
        [HttpGet("listar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,SUB_ADMIN")]
        public async Task<IActionResult> Listar([FromQuery] PaginacionRequest solicitud)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Listado de usuarios - SolicitadoPor: {UsuarioId}, Pagina: {Pagina}, IP: {Ip}",
                usuarioActualId, solicitud.Pagina, ipCliente);

            try
            {
                var resultado = await _usuarioService.ObtenerTodosAsync(solicitud);
                return Ok(ApiRespuesta<ResultadoPaginado<UsuarioResponse>>.Success(resultado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar usuarios - SolicitadoPor: {UsuarioId}", usuarioActualId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la lista de usuarios"));
            }
        }

        /// <summary>
        /// Recupera la información detallada de un usuario específico por su identificador.
        /// </summary>
        /// <param name="id">ID del usuario a consultar.</param>
        /// <returns>Detalles completos del perfil del usuario.</returns>
        /// <remarks>Los usuarios regulares solo pueden consultarse a sí mismos. Administradores pueden consultar cualquier ID.</remarks>
        /// <response code="200">Usuario encontrado correctamente.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado - No tiene permisos para ver este usuario.</response>
        /// <response code="404">Usuario no encontrado.</response>
        [HttpGet("obtener/{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var rolesActuales = ObtenerRolesActuales();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de usuario - UsuarioIdConsultado: {UsuarioConsultado}, SolicitadoPor: {UsuarioId}, IP: {Ip}",
                id, usuarioActualId, ipCliente);

            try
            {
                if (!rolesActuales.Contains("SUPER_ADMIN") &&
                    !rolesActuales.Contains("ADMIN") &&
                    usuarioActualId != id)
                {
                    await _auditoriaService.RegistrarErrorAsync(
                        usuarioActualId,
                        "CONSULTAR_USUARIO",
                        "Acceso denegado - No tiene permisos para ver este usuario",
                        "usuarios",
                        id,
                        null,
                        ipCliente,
                        null);

                    _logger.LogWarning("Acceso denegado a consulta de usuario - UsuarioConsultado: {UsuarioConsultado}, Solicitante: {Solicitante}",
                        id, usuarioActualId);
                    return Forbid();
                }

                var usuario = await _usuarioService.ObtenerPorIdAsync(id);
                if (usuario == null)
                {
                    _logger.LogWarning("Usuario no encontrado - UsuarioIdConsultado: {UsuarioConsultado}", id);
                    return NotFound(ApiRespuesta<object>.Error("Usuario no encontrado"));
                }

                return Ok(ApiRespuesta<UsuarioDetalleResponse>.Success(usuario));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario - UsuarioIdConsultado: {UsuarioConsultado}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener el usuario"));
            }
        }

        /// <summary>
        /// Obtiene el perfil del usuario actualmente autenticado.
        /// </summary>
        /// <returns>Detalles completos del perfil del usuario autenticado.</returns>
        /// <response code="200">Perfil obtenido correctamente.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="404">Usuario no encontrado.</response>
        [HttpGet("me")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            var usuarioId = ObtenerUsuarioActualId();

            _logger.LogInformation("Consulta de perfil propio - UsuarioId: {UsuarioId}", usuarioId);

            try
            {
                var usuario = await _usuarioService.ObtenerPorIdAsync(usuarioId);
                if (usuario == null)
                    return NotFound(ApiRespuesta<object>.Error("Usuario no encontrado"));

                return Ok(ApiRespuesta<UsuarioDetalleResponse>.Success(usuario));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perfil propio - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener el perfil"));
            }
        }

        /// <summary>
        /// Actualiza la información personal y de perfil de un usuario existente.
        /// </summary>
        /// <param name="id">ID del usuario a actualizar.</param>
        /// <param name="solicitud">Datos actualizados del perfil (Nombre, Biografía, Medidas, etc.).</param>
        /// <returns>Respuesta de éxito con los datos actualizados.</returns>
        /// <remarks>Los usuarios pueden actualizar su propio perfil. Administradores pueden actualizar cualquier perfil.</remarks>
        /// <response code="200">Usuario actualizado correctamente.</response>
        /// <response code="400">Datos de entrada inválidos.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado.</response>
        /// <response code="404">Usuario no encontrado.</response>
        [HttpPut("actualizar/{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarUsuarioRequest solicitud)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var rolesActuales = ObtenerRolesActuales();
            var ipCliente = ObtenerIpCliente();
            var usuarioAnterior = await _usuarioService.ObtenerPorIdAsync(id);

            _logger.LogInformation("Actualización de usuario - UsuarioActualizar: {UsuarioActualizar}, Solicitante: {Solicitante}, IP: {Ip}",
                id, usuarioActualId, ipCliente);

            try
            {
                if (!rolesActuales.Contains("SUPER_ADMIN") &&
                    !rolesActuales.Contains("ADMIN") &&
                    usuarioActualId != id)
                {
                    await _auditoriaService.RegistrarErrorAsync(
                        usuarioActualId,
                        "ACTUALIZAR_USUARIO",
                        "Acceso denegado - No tiene permisos para actualizar este usuario",
                        "usuarios",
                        id,
                        null,
                        ipCliente,
                        null);
                    return Forbid();
                }

                var resultado = await _usuarioService.ActualizarAsync(id, solicitud);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioActualId,
                    "ACTUALIZAR_USUARIO",
                    "usuarios",
                    id,
                    AuditoriaService.SerializarDatos(new
                    {
                        Anterior = usuarioAnterior != null ? new
                        {
                            usuarioAnterior.Nombres,
                            usuarioAnterior.Apellidos,
                            usuarioAnterior.Telefono,
                            usuarioAnterior.PesoKg,
                            usuarioAnterior.AlturaCm,
                            usuarioAnterior.Biografia,
                            usuarioAnterior.Ciudad,
                            usuarioAnterior.Pais,
                            usuarioAnterior.Genero
                        } : null,
                        Nuevo = new
                        {
                            solicitud.Nombres,
                            solicitud.Apellidos,
                            solicitud.Telefono,
                            solicitud.PesoKg,
                            solicitud.AlturaCm,
                            solicitud.Biografia,
                            solicitud.Ciudad,
                            solicitud.Pais,
                            solicitud.Genero
                        }
                    }));

                _logger.LogInformation("Usuario actualizado exitosamente - UsuarioIdActualizado: {UsuarioId}", id);
                return Ok(ApiRespuesta<UsuarioResponse>.Success(resultado, "Usuario actualizado exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(usuarioActualId, "ACTUALIZAR_USUARIO", ex.Message, "usuarios", id, null, ipCliente, null);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(usuarioActualId, "ACTUALIZAR_USUARIO", ex.Message, "usuarios", id, null, ipCliente, null);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarErrorAsync(usuarioActualId, "ACTUALIZAR_USUARIO", "Error interno", "usuarios", id, null, ipCliente, null);
                _logger.LogError(ex, "Error al actualizar usuario - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar el usuario"));
            }
        }

        /// <summary>
        /// Desactiva lógicamente un usuario (no se elimina físicamente de la base de datos).
        /// </summary>
        /// <param name="id">ID del usuario a desactivar.</param>
        /// <returns>Confirmación de la desactivación.</returns>
        /// <remarks>Solo usuarios con roles SUPER_ADMIN o ADMIN pueden realizar esta acción.</remarks>
        /// <response code="200">Usuario desactivado correctamente.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado.</response>
        /// <response code="404">Usuario no encontrado.</response>
        [HttpDelete("desactivar/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> Desactivar(int id)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Desactivación de usuario - UsuarioDesactivar: {UsuarioDesactivar}, Solicitante: {Solicitante}, IP: {Ip}",
                id, usuarioActualId, ipCliente);

            try
            {
                await _usuarioService.DesactivarAsync(id);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioActualId,
                    "DESACTIVAR_USUARIO",
                    "usuarios",
                    id);

                _logger.LogInformation("Usuario desactivado exitosamente - UsuarioId: {UsuarioId}, DesactivadoPor: {AdminId}", id, usuarioActualId);
                return Ok(ApiRespuesta<object>.Success(null, "Usuario desactivado exitosamente"));
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
                _logger.LogError(ex, "Error al desactivar usuario - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al desactivar el usuario"));
            }
        }

        /// <summary>
        /// Reactiva un usuario que fue previamente desactivado.
        /// </summary>
        /// <param name="id">ID del usuario a reactivar.</param>
        /// <returns>Confirmación de la reactivación.</returns>
        /// <remarks>Solo usuarios con roles SUPER_ADMIN o ADMIN pueden realizar esta acción.</remarks>
        /// <response code="200">Usuario reactivado correctamente.</response>
        /// <response code="400">El usuario ya está activo.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado.</response>
        /// <response code="404">Usuario no encontrado.</response>
        [HttpPost("reactivar/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> Reactivar(int id)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Reactivación de usuario - UsuarioReactivar: {UsuarioReactivar}, Solicitante: {Solicitante}, IP: {Ip}",
                id, usuarioActualId, ipCliente);

            try
            {
                await _usuarioService.ReactivarAsync(id);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioActualId,
                    "REACTIVAR_USUARIO",
                    "usuarios",
                    id);

                _logger.LogInformation("Usuario reactivado - UsuarioId: {UsuarioId}, AdminId: {AdminId}", id, usuarioActualId);
                return Ok(ApiRespuesta<object>.Success(null, "Usuario reactivado exitosamente"));
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
                _logger.LogError(ex, "Error al reactivar usuario - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al reactivar el usuario"));
            }
        }

        #endregion

        #region ========== GESTIÓN DE FOTOS DE PERFIL ==========

        /// <summary>
        /// Actualiza o establece la imagen de perfil del usuario autenticado.
        /// </summary>
        /// <param name="id">ID del usuario al que pertenece la foto.</param>
        /// <param name="foto">Archivo de imagen (multipart/form-data).</param>
        /// <returns>La URL de la nueva foto cargada.</returns>
        /// <remarks>Formatos soportados: .jpg, .jpeg, .png, .gif. Tamaño máximo: 5MB.</remarks>
        /// <response code="200">Foto actualizada correctamente.</response>
        /// <response code="400">El archivo no es válido o está vacío.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado.</response>
        /// <response code="404">Usuario no encontrado.</response>
        [HttpPut("actualizar-foto/{id}")]
        public async Task<IActionResult> ActualizarFoto(int id, [FromForm] IFormFile foto)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var rolesActuales = ObtenerRolesActuales();
            var ipCliente = ObtenerIpCliente();
            var nombreArchivo = foto?.FileName ?? "ninguno";

            _logger.LogInformation("Actualización de foto de perfil - UsuarioId: {UsuarioId}, Archivo: {Archivo}, IP: {Ip}",
                id, nombreArchivo, ipCliente);

            try
            {
                if (!rolesActuales.Contains("SUPER_ADMIN") &&
                    !rolesActuales.Contains("ADMIN") &&
                    usuarioActualId != id)
                {
                    await _auditoriaService.RegistrarErrorAsync(
                        usuarioActualId,
                        "ACTUALIZAR_FOTO",
                        "Acceso denegado - No tiene permisos para actualizar la foto de este usuario",
                        "usuarios",
                        id,
                        null,
                        ipCliente,
                        null);
                    return Forbid();
                }

                if (foto == null || foto.Length == 0)
                {
                    _logger.LogWarning("Intento de actualización de foto sin archivo - UsuarioId: {UsuarioId}", id);
                    return BadRequest(ApiRespuesta<object>.Error("La foto es requerida"));
                }

                var fotoUrl = await _usuarioService.ActualizarFotoAsync(id, foto);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioActualId,
                    "ACTUALIZAR_FOTO",
                    "usuarios",
                    id,
                    AuditoriaService.SerializarDatos(new
                    {
                        NuevoArchivo = nombreArchivo,
                        Tamaño = foto.Length,
                        Tipo = foto.ContentType
                    }));

                _logger.LogInformation("Foto de perfil actualizada exitosamente - UsuarioId: {UsuarioId}, Tamaño: {Tamaño} bytes", id, foto.Length);
                return Ok(ApiRespuesta<object>.Success(new { UrlFoto = fotoUrl }, "Foto actualizada exitosamente"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Error de formato en foto - UsuarioId: {UsuarioId}, Motivo: {Motivo}", id, ex.Message);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar foto - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar la foto"));
            }
        }

        /// <summary>
        /// Permite a un administrador actualizar la foto de perfil de otro usuario.
        /// </summary>
        /// <param name="id">ID del usuario cuya foto se actualizará.</param>
        /// <param name="foto">Archivo de imagen (multipart/form-data).</param>
        /// <returns>La URL de la nueva foto cargada.</returns>
        /// <remarks>Solo usuarios con roles SUPER_ADMIN o ADMIN pueden realizar esta acción.</remarks>
        /// <response code="200">Foto actualizada correctamente.</response>
        /// <response code="400">El archivo no es válido o está vacío.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado.</response>
        /// <response code="404">Usuario no encontrado.</response>
        [HttpPut("admin/actualizar-foto/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> AdminActualizarFoto(int id, [FromForm] IFormFile foto)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Admin actualizando foto - UsuarioId: {UsuarioId}, AdminId: {AdminId}, Archivo: {Archivo}, IP: {Ip}",
                id, usuarioActualId, foto?.FileName, ipCliente);

            if (foto == null || foto.Length == 0)
                return BadRequest(ApiRespuesta<object>.Error("La foto es requerida"));

            try
            {
                var fotoUrl = await _usuarioService.ActualizarFotoAsync(id, foto);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioActualId,
                    "ADMIN_ACTUALIZAR_FOTO_USUARIO",
                    "usuarios",
                    id,
                    $"Archivo: {foto.FileName}, Tamaño: {foto.Length} bytes");

                _logger.LogInformation("Admin actualizó foto - UsuarioId: {UsuarioId}, AdminId: {AdminId}", id, usuarioActualId);
                return Ok(ApiRespuesta<object>.Success(new { UrlFoto = fotoUrl }, "Foto actualizada exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error admin al actualizar foto - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar la foto"));
            }
        }

        #endregion

        #region ========== GESTIÓN DE CONTRASEÑA ==========

        /// <summary>
        /// Permite a un usuario autenticado modificar su contraseña actual.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <param name="solicitud">Contiene la contraseña actual y la nueva para validación.</param>
        /// <returns>Resultado de la operación.</returns>
        /// <remarks>Solo el propio usuario puede cambiar su contraseña.</remarks>
        /// <response code="200">Contraseña actualizada correctamente.</response>
        /// <response code="400">Las nuevas contraseñas no coinciden o no cumplen requisitos.</response>
        /// <response code="401">No autorizado o contraseña actual incorrecta.</response>
        /// <response code="403">Acceso denegado.</response>
        /// <response code="404">Usuario no encontrado.</response>
        [HttpPut("cambiar-contrasena/{id}")]
        public async Task<IActionResult> CambiarContrasena(int id, [FromBody] CambiarPasswordRequest solicitud)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Cambio de contraseña - UsuarioId: {UsuarioId}, IP: {Ip}", id, ipCliente);

            try
            {
                if (usuarioActualId != id)
                {
                    await _auditoriaService.RegistrarErrorAsync(
                        usuarioActualId,
                        "CAMBIAR_CONTRASENA",
                        "Acceso denegado - Solo el propio usuario puede cambiar su contraseña",
                        "usuarios",
                        id,
                        null,
                        ipCliente,
                        null);

                    _logger.LogWarning("Acceso denegado a cambio de contraseña - UsuarioId: {UsuarioId}, Solicitante: {Solicitante}", id, usuarioActualId);
                    return Forbid();
                }

                await _usuarioService.CambiarContrasenaAsync(id, solicitud);

                await _auditoriaService.RegistrarExitoAsync(usuarioActualId, "CAMBIAR_CONTRASENA", "usuarios", id);
                _logger.LogInformation("Contraseña cambiada exitosamente - UsuarioId: {UsuarioId}", id);

                return Ok(ApiRespuesta<object>.Success(null, "Contraseña actualizada exitosamente"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Contraseña actual incorrecta - UsuarioId: {UsuarioId}", id);
                return Unauthorized(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Error de validación en cambio de contraseña - UsuarioId: {UsuarioId}, Motivo: {Motivo}", id, ex.Message);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al cambiar la contraseña"));
            }
        }

        #endregion

        #region ========== REGISTRO DE JUGADORES ==========

        /// <summary>
        /// Registra un nuevo jugador en el sistema y envía una notificación de bienvenida.
        /// </summary>
        /// <param name="solicitud">Datos del jugador (Email, Nombres, Apellidos, etc.).</param>
        /// <returns>Información del jugador creado.</returns>
        /// <remarks>Solo usuarios con roles SUPER_ADMIN o ADMIN pueden realizar esta acción.</remarks>
        /// <response code="200">Jugador registrado exitosamente.</response>
        /// <response code="400">Datos inválidos o email ya registrado.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado.</response>
        /// <response code="500">Error interno al procesar el registro.</response>
        [HttpPost("registrar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> RegistrarJugador([FromBody] RegistrarJugadorRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Registro de jugador - AdminId: {AdminId}, Email: {Email}, IP: {Ip}",
                usuarioId, solicitud.Email, ipCliente);

            try
            {
                var resultado = await _jugadorService.RegistrarJugadorAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
                {
                    IdUsuarioDestino = resultado.Id,
                    IdTipoNotificacion = 1,
                    Titulo = "¡Bienvenido a TorneoPro!",
                    Mensaje = $"Hola {resultado.NombreCompleto}, tu cuenta ha sido creada exitosamente.",
                    Prioridad = "MEDIA"
                });

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "REGISTRAR_JUGADOR",
                    "usuarios",
                    resultado.Id,
                    System.Text.Json.JsonSerializer.Serialize(new { solicitud.Email, solicitud.Nombres, solicitud.Apellidos }));

                _logger.LogInformation("Jugador registrado exitosamente - JugadorId: {JugadorId}, Email: {Email}", resultado.Id, solicitud.Email);
                return Ok(ApiRespuesta<JugadorResponse>.Success(resultado, "Jugador registrado exitosamente"));
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
                _logger.LogError(ex, "Error al registrar jugador - Email: {Email}", solicitud.Email);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al registrar el jugador"));
            }
        }

        #endregion

        #region ========== GESTIÓN DE ROLES ==========

        /// <summary>
        /// Lista todos los roles asociados a un usuario específico.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <returns>Lista de roles asignados al usuario.</returns>
        /// <remarks>Solo usuarios con roles SUPER_ADMIN o ADMIN pueden realizar esta acción.</remarks>
        /// <response code="200">Roles obtenidos correctamente.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado.</response>
        /// <response code="404">Usuario no encontrado.</response>
        [HttpGet("roles/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ObtenerRoles(int id)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de roles - UsuarioConsultado: {UsuarioConsultado}, Solicitante: {Solicitante}, IP: {Ip}",
                id, usuarioActualId, ipCliente);

            try
            {
                var roles = await _usuarioService.ObtenerRolesAsync(id);
                _logger.LogInformation("Roles consultados - UsuarioId: {UsuarioId}, Cantidad: {Cantidad}", id, roles.Count);
                return Ok(ApiRespuesta<List<RolResponse>>.Success(roles));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Usuario no encontrado para consulta de roles - UsuarioId: {UsuarioId}", id);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener los roles"));
            }
        }

        /// <summary>
        /// Asigna un nuevo rol a un usuario, permitiendo especificar contexto de Torneo o Equipo.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <param name="solicitud">Detalles del rol a asignar (rol, torneo, equipo, fechas).</param>
        /// <returns>Información del rol asignado.</returns>
        /// <remarks>Solo usuarios con roles SUPER_ADMIN o ADMIN pueden realizar esta acción.</remarks>
        /// <response code="200">Rol asignado correctamente.</response>
        /// <response code="400">Datos inválidos o el usuario ya tiene este rol.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado.</response>
        /// <response code="404">Usuario, rol, torneo o equipo no encontrado.</response>
        [HttpPost("asignar-rol/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> AsignarRol(int id, [FromBody] AsignarRolRequest solicitud)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Asignación de rol - UsuarioId: {UsuarioId}, RolId: {RolId}, Solicitante: {Solicitante}, IP: {Ip}",
                id, solicitud.IdRol, usuarioActualId, ipCliente);

            try
            {
                var resultado = await _usuarioService.AsignarRolAsync(id, solicitud);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioActualId,
                    "ASIGNAR_ROL",
                    "usuarios_roles",
                    resultado.Id,
                    AuditoriaService.SerializarDatos(new
                    {
                        UsuarioId = id,
                        RolId = solicitud.IdRol,
                        IdTorneo = solicitud.IdTorneo,
                        IdEquipo = solicitud.IdEquipo,
                        FechaInicio = solicitud.FechaInicio,
                        FechaFin = solicitud.FechaFin
                    }));

                _logger.LogInformation("Rol asignado exitosamente - UsuarioId: {UsuarioId}, RolId: {RolId}, AdminId: {AdminId}",
                    id, solicitud.IdRol, usuarioActualId);

                return Ok(ApiRespuesta<RolResponse>.Success(resultado, "Rol asignado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Error al asignar rol - UsuarioId: {UsuarioId}, Motivo: {Motivo}", id, ex.Message);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Recurso no encontrado para asignar rol - UsuarioId: {UsuarioId}, Motivo: {Motivo}", id, ex.Message);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar rol - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al asignar el rol"));
            }
        }

        /// <summary>
        /// Revoca (elimina) la asociación de un rol específico de un usuario.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <param name="rolId">ID del rol a revocar.</param>
        /// <returns>Confirmación de la revocación.</returns>
        /// <remarks>Solo usuarios con roles SUPER_ADMIN o ADMIN pueden realizar esta acción.</remarks>
        /// <response code="200">Rol revocado correctamente.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado.</response>
        /// <response code="404">Rol no encontrado o ya revocado.</response>
        [HttpDelete("revocar-rol/{id}/{rolId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> RevocarRol(int id, int rolId)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Revocación de rol - UsuarioId: {UsuarioId}, RolId: {RolId}, Solicitante: {Solicitante}, IP: {Ip}",
                id, rolId, usuarioActualId, ipCliente);

            try
            {
                await _usuarioService.RevocarRolAsync(id, rolId);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioActualId,
                    "REVOCAR_ROL",
                    "usuarios_roles",
                    null,
                    AuditoriaService.SerializarDatos(new { UsuarioId = id, RolId = rolId }));

                _logger.LogInformation("Rol revocado - UsuarioId: {UsuarioId}, RolId: {RolId}, AdminId: {AdminId}", id, rolId, usuarioActualId);
                return Ok(ApiRespuesta<object>.Success(null, "Rol revocado exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Rol no encontrado para revocar - UsuarioId: {UsuarioId}, RolId: {RolId}", id, rolId);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Error al revocar rol - UsuarioId: {UsuarioId}, Motivo: {Motivo}", id, ex.Message);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar rol - UsuarioId: {UsuarioId}, RolId: {RolId}", id, rolId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al revocar el rol"));
            }
        }

        #endregion

        #region ========== ESTADÍSTICAS Y REPORTES ==========

        /// <summary>
        /// Obtiene estadísticas generales de usuarios del sistema.
        /// </summary>
        /// <returns>Estadísticas como total de usuarios, activos, inactivos, distribución por tipo/rol, registros por mes.</returns>
        /// <remarks>Solo usuarios con roles SUPER_ADMIN o ADMIN pueden realizar esta acción.</remarks>
        /// <response code="200">Estadísticas obtenidas correctamente.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">Acceso denegado.</response>
        [HttpGet("estadisticas")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ObtenerEstadisticasUsuarios()
        {
            _logger.LogInformation("Consulta de estadísticas de usuarios - Solicitante: {Solicitante}", ObtenerUsuarioActualId());

            try
            {
                var estadisticas = await _usuarioService.ObtenerEstadisticasAsync();
                return Ok(ApiRespuesta<object>.Success(estadisticas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de usuarios");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener estadísticas"));
            }
        }

        #endregion

        #region ========== MÉTODOS PRIVADOS ==========

        /// <summary>
        /// Obtiene el ID del usuario autenticado desde los claims del token JWT.
        /// </summary>
        /// <returns>ID del usuario autenticado.</returns>
        /// <exception cref="UnauthorizedAccessException">Si el claim no está presente o no es válido.</exception>
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
        /// Obtiene la lista de roles del usuario autenticado desde los claims del token JWT.
        /// </summary>
        /// <returns>Lista de nombres de roles.</returns>
        private List<string> ObtenerRolesActuales()
        {
            return User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }

        /// <summary>
        /// Obtiene la dirección IP del cliente, considerando proxies y balanceadores de carga.
        /// </summary>
        /// <returns>Dirección IP del cliente o "IP desconocida".</returns>
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