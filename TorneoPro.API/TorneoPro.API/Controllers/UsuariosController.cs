using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.DTOs.Usuarios;
using TorneoPro.API.DTOs.Usuarios.Request;
using TorneoPro.API.DTOs.Usuarios.Response;
using TorneoPro.API.Servicios.Implementaciones;
using TorneoPro.API.Servicios.Implementaciones.Auditoria;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Usuarios;

namespace TorneoPro.API.Controllers
{
    [Route("api/usuarios")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(
            IUsuarioService usuarioService,
            IAuditoriaService auditoriaService,
            ILogger<UsuariosController> logger)
        {
            _usuarioService = usuarioService;
            _auditoriaService = auditoriaService;
            _logger = logger;
        }

        /// <summary>
        /// Listar usuarios (paginado) - Solo administradores
        /// </summary>
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
        /// Obtener usuario por ID
        /// </summary>
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
        /// Actualizar perfil de usuario
        /// </summary>
        [HttpPut("actualizar/{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarUsuarioRequest solicitud)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var rolesActuales = ObtenerRolesActuales();
            var ipCliente = ObtenerIpCliente();

            // Obtener datos anteriores para auditoría
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
        /// Actualizar foto de perfil
        /// </summary>
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
        /// Cambiar contraseña
        /// </summary>
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
        /// Desactivar usuario (eliminación lógica) - Solo administradores
        /// </summary>
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
        /// Ver roles del usuario
        /// </summary>
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
        /// Asignar rol a usuario - Solo administradores
        /// </summary>
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
        /// Revocar rol de usuario - Solo administradores
        /// </summary>
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

        private List<string> ObtenerRolesActuales()
        {
            return User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
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
    }
}