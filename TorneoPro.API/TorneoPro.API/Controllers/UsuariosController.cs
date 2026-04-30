using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorneoPro.API.DTOs.Jugadores.Request;
using TorneoPro.API.DTOs.Jugadores.Response;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.DTOs.Usuarios;
using TorneoPro.API.DTOs.Usuarios.Request;
using TorneoPro.API.DTOs.Usuarios.Response;
using TorneoPro.API.Servicios.Implementaciones;
using TorneoPro.API.Servicios.Implementaciones.Auditoria;
using TorneoPro.API.Servicios.Implementaciones.Notifiacion;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Jugador;
using TorneoPro.API.Servicios.Interfaces.Notificacion;
using TorneoPro.API.Servicios.Interfaces.Usuarios;

namespace TorneoPro.API.Controllers
{
    [Route("api/usuarios")]
    [ApiController]
    [Authorize]

    /// <summary>
    /// Controlador para la gestión administrativa y de perfil de usuarios.
    /// Permite operaciones de consulta, actualización de datos, gestión de roles y auditoría de acciones.
    /// </summary>
    public class UsuariosController : ControllerBase
    {
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

        /// <summary>
        /// Obtiene una lista paginada de todos los usuarios registrados en el sistema.
        /// </summary>
        /// <param name="solicitud">Objeto que contiene los parámetros de paginación (Página y Tamaño).</param>
        /// <returns>Resultado paginado con información básica de los usuarios.</returns>
        /// <response code="200">Retorna la lista de usuarios.</response>
        /// <response code="401">No autorizado.</response>
        /// <response code="403">El usuario no tiene roles administrativos (SUPER_ADMIN, ADMIN, SUB_ADMIN).</response>
        [HttpGet("listar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,SUB_ADMIN")]
        public async Task<IActionResult> Listar([FromQuery] PaginacionRequest solicitud)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Listado de usuarios - SolicitadoPor: {UsuarioId}, Pagina: {Pagina}, Tamano: {Tamano}, IP: {Ip}",
                usuarioActualId, solicitud.Pagina, solicitud.TamanoPagina, ipCliente);

            try
            {
                var resultado = await _usuarioService.ObtenerTodosAsync(solicitud);

                _logger.LogInformation("Listado de usuarios completado - TotalItems: {TotalItems}, SolicitadoPor: {UsuarioId}",
                    resultado.TotalItems, usuarioActualId);

                return Ok(ApiRespuesta<ResultadoPaginado<UsuarioResponse>>.Success(resultado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar usuarios - SolicitadoPor: {UsuarioId}", usuarioActualId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la lista de usuarios"));
            }
        }

        /// <summary>
        /// Registra un nuevo jugador en el sistema y envía una notificación de bienvenida.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite a un administrador dar de alta a un usuario con el rol de jugador.
        /// El proceso incluye:
        /// 1. Registro en la base de datos a través del servicio de jugadores.
        /// 2. Envío de una notificación interna de bienvenida.
        /// 3. Registro de la acción en la bitácora de auditoría.
        /// </remarks>
        /// <param name="solicitud">Objeto que contiene los datos del jugador (Email, Nombres, Apellidos, etc.).</param>
        /// <returns>Retorna un <see cref="ApiRespuesta{T}"/> con la información del jugador creado.</returns>
        /// <response code="200">El jugador fue registrado y notificado exitosamente.</response>
        /// <response code="400">La solicitud es inválida o el email ya se encuentra registrado.</response>
        /// <response code="403">El usuario no tiene permisos suficientes para realizar esta acción.</response>
        /// <response code="500">Error interno al procesar el registro.</response>
        [HttpPost("registrar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> RegistrarJugador([FromBody] RegistrarJugadorRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Registro de jugador - AdministradorId: {AdminId}, Email: {Email}, IP: {Ip}",
                usuarioId, solicitud.Email, ipCliente);

            try
            {
                var resultado = await _jugadorService.RegistrarJugadorAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
                {
                    IdUsuarioDestino = resultado.Id,
                    IdTipoNotificacion = 1,
                    Titulo = "¡Bienvenido a TorneoPro!",
                    Mensaje = $"Hola {resultado.NombreCompleto}, tu cuenta ha sido creada exitosamente. Ya puedes participar en torneos.",
                    Prioridad = "MEDIA"
                });

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "REGISTRAR_JUGADOR",
                    "usuarios",
                    resultado.Id,
                    System.Text.Json.JsonSerializer.Serialize(new { solicitud.Email, solicitud.Nombres, solicitud.Apellidos }));

                _logger.LogInformation("Jugador registrado exitosamente - JugadorId: {JugadorId}, Email: {Email}",
                    resultado.Id, solicitud.Email);

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



        /// <summary>
        /// Recupera la información detallada de un usuario específico por su identificador único.
        /// </summary>
        /// <param name="id">ID del usuario a consultar.</param>
        /// <returns>Detalles completos del perfil del usuario.</returns>
        /// <remarks>Los usuarios regulares solo pueden consultarse a sí mismos. Admins pueden consultar cualquier ID.</remarks>
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
                        null
                    );

                    _logger.LogWarning("Acceso denegado a consulta de usuario - UsuarioIdConsultado: {UsuarioConsultado}, SolicitadoPor: {UsuarioId}",
                        id, usuarioActualId);
                    return Forbid();
                }

                var usuario = await _usuarioService.ObtenerPorIdAsync(id);
                if (usuario == null)
                {
                    _logger.LogWarning("Usuario no encontrado - UsuarioIdConsultado: {UsuarioConsultado}", id);
                    return NotFound(ApiRespuesta<object>.Error("Usuario no encontrado"));
                }

                _logger.LogInformation("Usuario consultado exitosamente - UsuarioIdConsultado: {UsuarioConsultado}", id);

                return Ok(ApiRespuesta<UsuarioDetalleResponse>.Success(usuario));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario - UsuarioIdConsultado: {UsuarioConsultado}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener el usuario"));
            }
        }

        /// <summary>
        /// Actualiza la información personal y de perfil de un usuario existente.
        /// </summary>
        /// <param name="id">ID del usuario a actualizar.</param>
        /// <param name="solicitud">Datos actualizados del perfil (Nombre, Biografía, Medidas, etc.).</param>
        /// <returns>Respuesta de éxito con los datos actualizados.</returns>
        [HttpPut("actualizar/{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarUsuarioRequest solicitud)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var rolesActuales = ObtenerRolesActuales();
            var ipCliente = ObtenerIpCliente();

         
            var usuarioAnterior = await _usuarioService.ObtenerPorIdAsync(id);

            _logger.LogInformation("Actualización de usuario - UsuarioIdActualizar: {UsuarioActualizar}, SolicitadoPor: {UsuarioId}, IP: {Ip}",
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
                        null
                    );
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
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "ACTUALIZAR_USUARIO",
                    ex.Message,
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "ACTUALIZAR_USUARIO",
                    ex.Message,
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "ACTUALIZAR_USUARIO",
                    "Error interno del servidor",
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                _logger.LogError(ex, "Error al actualizar usuario - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar el usuario"));
            }
        }

        /// <summary>
        /// Actualiza o establece la imagen de perfil del usuario mediante la carga de un archivo.
        /// </summary>
        /// <param name="id">ID del usuario al que pertenece la foto.</param>
        /// <param name="foto">Archivo de imagen (Multipart/form-data).</param>
        /// <returns>La URL de la nueva foto cargada.</returns>
        [HttpPut("actualizar-foto/{id}")]
        public async Task<IActionResult> ActualizarFoto(int id, [FromForm] IFormFile foto)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var rolesActuales = ObtenerRolesActuales();
            var ipCliente = ObtenerIpCliente();
            var nombreArchivo = foto?.FileName ?? "ninguno";

            _logger.LogInformation("Actualización de foto de perfil - UsuarioId: {UsuarioId}, SolicitadoPor: {Solicitante}, Archivo: {Archivo}, IP: {Ip}",
                id, usuarioActualId, nombreArchivo, ipCliente);

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
                        null
                    );
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

                _logger.LogInformation("Foto de perfil actualizada exitosamente - UsuarioId: {UsuarioId}, Tamaño: {Tamaño} bytes",
                    id, foto.Length);

                return Ok(ApiRespuesta<object>.Success(new { UrlFoto = fotoUrl }, "Foto actualizada exitosamente"));
            }
            catch (ArgumentException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "ACTUALIZAR_FOTO",
                    ex.Message,
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                _logger.LogWarning("Error de formato en foto - UsuarioId: {UsuarioId}, Motivo: {Motivo}", id, ex.Message);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "ACTUALIZAR_FOTO",
                    ex.Message,
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "ACTUALIZAR_FOTO",
                    "Error interno del servidor",
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                _logger.LogError(ex, "Error al actualizar foto - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar la foto"));
            }
        }

        /// <summary>
        /// Permite a un usuario autenticado modificar su contraseña de acceso actual.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <param name="solicitud">Contiene la contraseña anterior y la nueva para validación.</param>
        /// <returns>Resultado de la operación.</returns>
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
                        null
                    );
                    _logger.LogWarning("Acceso denegado a cambio de contraseña - UsuarioId: {UsuarioId}, SolicitadoPor: {Solicitante}",
                        id, usuarioActualId);
                    return Forbid();
                }

                await _usuarioService.CambiarContrasenaAsync(id, solicitud);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioActualId,
                    "CAMBIAR_CONTRASENA",
                    "usuarios",
                    id);

                _logger.LogInformation("Contraseña cambiada exitosamente - UsuarioId: {UsuarioId}", id);

                return Ok(ApiRespuesta<object>.Success(null, "Contraseña actualizada exitosamente"));
            }
            catch (UnauthorizedAccessException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "CAMBIAR_CONTRASENA",
                    ex.Message,
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                _logger.LogWarning("Contraseña actual incorrecta - UsuarioId: {UsuarioId}", id);
                return Unauthorized(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (ArgumentException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "CAMBIAR_CONTRASENA",
                    ex.Message,
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                _logger.LogWarning("Error de validación en cambio de contraseña - UsuarioId: {UsuarioId}, Motivo: {Motivo}", id, ex.Message);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "CAMBIAR_CONTRASENA",
                    ex.Message,
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "CAMBIAR_CONTRASENA",
                    "Error interno del servidor",
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                _logger.LogError(ex, "Error al cambiar contraseña - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al cambiar la contraseña"));
            }
        }

        /// <summary>
        /// Realiza una desactivación lógica de un usuario en el sistema. Solo para Administradores.
        /// </summary>
        /// <param name="id">ID del usuario a desactivar.</param>
        /// <returns>Confirmación de la desactivación.</returns>
        [HttpDelete("desactivar/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> Desactivar(int id)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Desactivación de usuario - UsuarioIdDesactivar: {UsuarioDesactivar}, SolicitadoPor: {Solicitante}, IP: {Ip}",
                id, usuarioActualId, ipCliente);

            try
            {
                await _usuarioService.DesactivarAsync(id);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioActualId,
                    "DESACTIVAR_USUARIO",
                    "usuarios",
                    id);

                _logger.LogInformation("Usuario desactivado exitosamente - UsuarioId: {UsuarioId}, DesactivadoPor: {AdminId}",
                    id, usuarioActualId);

                return Ok(ApiRespuesta<object>.Success(null, "Usuario desactivado exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "DESACTIVAR_USUARIO",
                    ex.Message,
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                _logger.LogWarning("Usuario no encontrado para desactivar - UsuarioId: {UsuarioId}", id);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "DESACTIVAR_USUARIO",
                    ex.Message,
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                _logger.LogWarning("No se puede desactivar usuario - UsuarioId: {UsuarioId}, Motivo: {Motivo}", id, ex.Message);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "DESACTIVAR_USUARIO",
                    "Error interno del servidor",
                    "usuarios",
                    id,
                    null,
                    ipCliente,
                    null
                );
                _logger.LogError(ex, "Error al desactivar usuario - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al desactivar el usuario"));
            }
        }

        /// <summary>
        /// Lista todos los roles asociados a un usuario en particular.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <returns>Lista de objetos de tipo RolResponse.</returns>
        [HttpGet("roles/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ObtenerRoles(int id)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de roles - UsuarioIdConsultado: {UsuarioConsultado}, SolicitadoPor: {Solicitante}, IP: {Ip}",
                id, usuarioActualId, ipCliente);

            try
            {
                var roles = await _usuarioService.ObtenerRolesAsync(id);

                _logger.LogInformation("Roles consultados exitosamente - UsuarioId: {UsuarioId}, CantidadRoles: {Cantidad}",
                    id, roles.Count);

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
        /// Asigna un nuevo rol a un usuario, permitiendo especificar opcionalmente el contexto del Torneo o Equipo.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <param name="solicitud">Detalles del rol a asignar y su vigencia.</param>
        /// <returns>Información del rol asignado.</returns>
        [HttpPost("asignar-rol/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> AsignarRol(int id, [FromBody] AsignarRolRequest solicitud)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Asignación de rol - UsuarioId: {UsuarioId}, RolId: {RolId}, SolicitadoPor: {Solicitante}, IP: {Ip}",
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

                _logger.LogInformation("Rol asignado exitosamente - UsuarioId: {UsuarioId}, RolId: {RolId}, AsignadoPor: {AdminId}",
                    id, solicitud.IdRol, usuarioActualId);

                return Ok(ApiRespuesta<RolResponse>.Success(resultado, "Rol asignado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "ASIGNAR_ROL",
                    ex.Message,
                    "usuarios_roles",
                    null,
                    null,
                    null,
                    null
                );
                _logger.LogWarning("Error al asignar rol - UsuarioId: {UsuarioId}, Motivo: {Motivo}", id, ex.Message);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "ASIGNAR_ROL",
                    ex.Message,
                    "usuarios_roles",
                    null,
                    null,
                    null,
                    null
                );
                _logger.LogWarning("Recurso no encontrado para asignar rol - UsuarioId: {UsuarioId}, Motivo: {Motivo}", id, ex.Message);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "ASIGNAR_ROL",
                    "Error interno del servidor",
                    "usuarios_roles",
                    null,
                    null,
                    null,
                    null
                );
                _logger.LogError(ex, "Error al asignar rol - UsuarioId: {UsuarioId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al asignar el rol"));
            }
        }

        /// <summary>
        /// Remueve la asociación de un rol específico de un usuario.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <param name="rolId">ID del rol a revocar.</param>
        /// <returns>Confirmación de la revocación exitosa.</returns>
        [HttpDelete("revocar-rol/{id}/{rolId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> RevocarRol(int id, int rolId)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Revocación de rol - UsuarioId: {UsuarioId}, RolId: {RolId}, SolicitadoPor: {Solicitante}, IP: {Ip}",
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

                _logger.LogInformation("Rol revocado exitosamente - UsuarioId: {UsuarioId}, RolId: {RolId}, RevocadoPor: {AdminId}",
                    id, rolId, usuarioActualId);

                return Ok(ApiRespuesta<object>.Success(null, "Rol revocado exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "REVOCAR_ROL",
                    ex.Message,
                    "usuarios_roles",
                    null,
                    null,
                    null,
                    null
                );
                _logger.LogWarning("Rol no encontrado para revocar - UsuarioId: {UsuarioId}, RolId: {RolId}", id, rolId);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "REVOCAR_ROL",
                    ex.Message,
                    "usuarios_roles",
                    null,
                    null,
                    null,
                    null
                );
                _logger.LogWarning("Error al revocar rol - UsuarioId: {UsuarioId}, Motivo: {Motivo}", id, ex.Message);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "REVOCAR_ROL",
                    "Error interno del servidor",
                    "usuarios_roles",
                    null,
                    null,
                    null,
                    null
                );
                _logger.LogError(ex, "Error al revocar rol - UsuarioId: {UsuarioId}, RolId: {RolId}", id, rolId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al revocar el rol"));
            }
        }

        /// <summary>
        /// Extrae el identificador del usuario (ID) desde los Claims del token JWT actual.
        /// </summary>
        /// <returns>ID numérico del usuario.</returns>
        /// <exception cref="UnauthorizedAccessException">Se lanza si el Claim no está presente o no es válido.</exception>
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
        /// Recupera la lista de nombres de roles contenidos en el token JWT del usuario actual.
        /// </summary>
        /// <returns>Lista de strings con los nombres de los roles.</returns>
        private List<string> ObtenerRolesActuales()
        {
            return User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }
        /// <summary>
        /// Obtiene la dirección IP del cliente desde los encabezados de la solicitud o la conexión remota.
        /// </summary>
        /// <returns>String con la dirección IP o "IP desconocida".</returns>
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