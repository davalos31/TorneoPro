using Microsoft.EntityFrameworkCore;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.DTOs.Usuarios.Request;
using TorneoPro.API.DTOs.Usuarios.Response;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces.Usuarios;

namespace TorneoPro.API.Servicios.Implementaciones.Usuarios
{
    public class UsuarioService : IUsuarioService
    {
        private readonly TorneoProContext _contexto;
        private readonly ArchivosHelper _archivosHelper;
        private readonly ILogger<UsuarioService> _logger;
        private readonly IConfiguration _configuracion;

        public UsuarioService(
            TorneoProContext contexto,
            ArchivosHelper archivosHelper,
            ILogger<UsuarioService> logger,
            IConfiguration configuracion)
        {
            _contexto = contexto;
            _archivosHelper = archivosHelper;
            _logger = logger;
            _configuracion = configuracion;
        }

        /// <summary>
        /// Reactivar usuario desactivado
        /// </summary>
        public async Task ReactivarAsync(int id)
        {
            var usuario = await _contexto.usuarios.FindAsync(id);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            if (usuario.activo == true)
            {
                throw new InvalidOperationException("El usuario ya está activo");
            }

            usuario.activo = true;
            usuario.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();
            _logger.LogInformation("Usuario reactivado - UsuarioId: {UsuarioId}", id);
        }

        /// <summary>
        /// Obtener estadísticas de usuarios
        /// </summary>
        public async Task<UsuarioEstadisticasResponse> ObtenerEstadisticasAsync()
        {
            var usuarios = await _contexto.usuarios.ToListAsync();
            var usuariosRoles = await _contexto.usuarios_roles
                .Where(ur => ur.estado == "ACTIVO" && ur.activo == true)
                .ToListAsync();
            var tiposUsuarios = await _contexto.tipos_usuarios.ToListAsync();
            var roles = await _contexto.tipos_rols.ToListAsync();

            var fechaActual = DateTime.UtcNow;
            var fechaInicioMes = new DateTime(fechaActual.Year, fechaActual.Month, 1);
            var fechaInicioMesAnterior = fechaInicioMes.AddMonths(-1);
            var fechaInicioHace6Meses = fechaInicioMes.AddMonths(-6);

            // Usuarios por tipo
            var usuariosPorTipo = tiposUsuarios
                .Select(t => new { t.nombre, Cantidad = usuarios.Count(u => u.id_tipo_usuario == t.id) })
                .Where(x => x.Cantidad > 0)
                .ToDictionary(x => x.nombre ?? "Desconocido", x => x.Cantidad);

            // Usuarios por rol
            var usuariosPorRol = roles
                .Select(r => new { r.nombre, Cantidad = usuariosRoles.Count(ur => ur.id_rol == r.id) })
                .Where(x => x.Cantidad > 0)
                .ToDictionary(x => x.nombre ?? "Desconocido", x => x.Cantidad);

            // Usuarios por mes (últimos 6 meses)
            var usuariosPorMes = new Dictionary<string, int>();
            for (int i = 5; i >= 0; i--)
            {
                var mesInicio = fechaActual.AddMonths(-i);
                var mesFin = mesInicio.AddMonths(1);
                var mesNombre = mesInicio.ToString("MMM yyyy");

                var cantidad = usuarios.Count(u =>
                    u.fecha_registro.HasValue &&
                    u.fecha_registro.Value >= mesInicio &&
                    u.fecha_registro.Value < mesFin);

                usuariosPorMes[mesNombre] = cantidad;
            }

            return new UsuarioEstadisticasResponse
            {
                TotalUsuarios = usuarios.Count,
                UsuariosActivos = usuarios.Count(u => u.activo == true),
                UsuariosInactivos = usuarios.Count(u => u.activo == false),
                UsuariosVerificados = usuarios.Count(u => u.email_verificado == true),
                UsuariosPorTipo = usuariosPorTipo,
                UsuariosPorRol = usuariosPorRol,
                UsuariosPorMes = usuariosPorMes
            };
        }

        /// <summary>
        /// Listar usuarios con filtros avanzados
        /// </summary>
        public async Task<ResultadoPaginado<UsuarioResponse>> ObtenerTodosAvanzadoAsync(FiltrarUsuarioRequest solicitud)
        {
            var query = _contexto.usuarios
                .Include(u => u.id_tipo_usuarioNavigation)
                .AsQueryable();

            // Filtro de búsqueda general
            if (!string.IsNullOrWhiteSpace(solicitud.Buscar))
            {
                query = query.Where(u =>
                    (u.nombres != null && u.nombres.Contains(solicitud.Buscar)) ||
                    (u.apellidos != null && u.apellidos.Contains(solicitud.Buscar)) ||
                    (u.email != null && u.email.Contains(solicitud.Buscar)) ||
                    (u.codigo != null && u.codigo.Contains(solicitud.Buscar)) ||
                    (u.telefono != null && u.telefono.Contains(solicitud.Buscar)));
            }

            // Filtro por tipo de usuario
            if (solicitud.IdTipoUsuario.HasValue)
            {
                query = query.Where(u => u.id_tipo_usuario == solicitud.IdTipoUsuario.Value);
            }

            // Filtro por rol
            if (solicitud.IdRol.HasValue)
            {
                query = query.Where(u => _contexto.usuarios_roles
                    .Any(ur => ur.id_usuario == u.id && ur.id_rol == solicitud.IdRol.Value && ur.estado == "ACTIVO"));
            }

            // Filtro por ciudad
            if (!string.IsNullOrWhiteSpace(solicitud.Ciudad))
            {
                query = query.Where(u => u.ciudad != null && u.ciudad.Contains(solicitud.Ciudad));
            }

            // Filtro por país
            if (!string.IsNullOrWhiteSpace(solicitud.Pais))
            {
                query = query.Where(u => u.pais != null && u.pais.Contains(solicitud.Pais));
            }

         
            // Filtro por activo
            if (solicitud.SoloActivos == true)
            {
                query = query.Where(u => u.activo == true);
            }
            else if (solicitud.SoloActivos == false)
            {
                query = query.Where(u => u.activo == false);
            }

            // Filtro por email verificado
            if (solicitud.EmailVerificado == true)
            {
                query = query.Where(u => u.email_verificado == true);
            }
            else if (solicitud.EmailVerificado == false)
            {
                query = query.Where(u => u.email_verificado == false);
            }

            // Filtro por fecha de registro
            if (solicitud.FechaRegistroDesde.HasValue)
            {
                query = query.Where(u => u.fecha_registro >= solicitud.FechaRegistroDesde.Value);
            }
            if (solicitud.FechaRegistroHasta.HasValue)
            {
                var fechaHasta = solicitud.FechaRegistroHasta.Value.Date.AddDays(1);
                query = query.Where(u => u.fecha_registro < fechaHasta);
            }

            var totalItems = await query.CountAsync();

            // Ordenamiento
            if (!string.IsNullOrWhiteSpace(solicitud.OrdenarPor))
            {
                query = solicitud.OrdenDescendente
                    ? query.OrderByDescending(u => EF.Property<object>(u, solicitud.OrdenarPor))
                    : query.OrderBy(u => EF.Property<object>(u, solicitud.OrdenarPor));
            }
            else
            {
                query = query.OrderByDescending(u => u.fecha_registro);
            }

            var usuarios = await query
                .Skip((solicitud.Pagina - 1) * solicitud.TamanoPagina)
                .Take(solicitud.TamanoPagina)
                .ToListAsync();

            var items = new List<UsuarioResponse>();
            foreach (var usuario in usuarios)
            {
                var roles = await ObtenerRolesUsuario(usuario.id);
                items.Add(MapearUsuarioResponse(usuario, roles));
            }

            return ResultadoPaginado<UsuarioResponse>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
        }

        public async Task<ResultadoPaginado<UsuarioResponse>> ObtenerTodosAsync(PaginacionRequest solicitud)
        {
            var query = _contexto.usuarios
                .Include(u => u.id_tipo_usuarioNavigation)
                .Where(u => u.activo == true)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(solicitud.Buscar))
            {
                query = query.Where(u =>
                    (u.nombres != null && u.nombres.Contains(solicitud.Buscar)) ||
                    (u.apellidos != null && u.apellidos.Contains(solicitud.Buscar)) ||
                    (u.email != null && u.email.Contains(solicitud.Buscar)) ||
                    (u.codigo != null && u.codigo.Contains(solicitud.Buscar)));
            }

            var totalItems = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(solicitud.OrdenarPor))
            {
                query = solicitud.OrdenDescendente
                    ? query.OrderByDescending(u => EF.Property<object>(u, solicitud.OrdenarPor))
                    : query.OrderBy(u => EF.Property<object>(u, solicitud.OrdenarPor));
            }
            else
            {
                query = query.OrderByDescending(u => u.fecha_registro);
            }

            var usuarios = await query
                .Skip((solicitud.Pagina - 1) * solicitud.TamanoPagina)
                .Take(solicitud.TamanoPagina)
                .ToListAsync();

            var items = new List<UsuarioResponse>();
            foreach (var usuario in usuarios)
            {
                var roles = await ObtenerRolesUsuario(usuario.id);
                items.Add(MapearUsuarioResponse(usuario, roles));
            }

            return ResultadoPaginado<UsuarioResponse>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
        }

        public async Task<UsuarioDetalleResponse?> ObtenerPorIdAsync(int id)
        {
            var usuario = await _contexto.usuarios
                .Include(u => u.id_tipo_usuarioNavigation)
                .FirstOrDefaultAsync(u => u.id == id && u.activo == true);

            if (usuario == null)
            {
                return null;
            }

            var roles = await ObtenerRolesUsuario(usuario.id);
            return MapearUsuarioDetalleResponse(usuario, roles);
        }

        public async Task<UsuarioResponse> ActualizarAsync(int id, ActualizarUsuarioRequest solicitud)
        {
            var usuario = await _contexto.usuarios.FindAsync(id);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            if (!string.IsNullOrWhiteSpace(solicitud.Nombres))
                usuario.nombres = solicitud.Nombres;

            if (!string.IsNullOrWhiteSpace(solicitud.Apellidos))
                usuario.apellidos = solicitud.Apellidos;

            if (!string.IsNullOrWhiteSpace(solicitud.Telefono))
                usuario.telefono = solicitud.Telefono;

            if (solicitud.PesoKg.HasValue)
                usuario.peso_kg = solicitud.PesoKg.Value;

            if (solicitud.AlturaCm.HasValue)
                usuario.altura_cm = solicitud.AlturaCm.Value;

            if (!string.IsNullOrWhiteSpace(solicitud.Biografia))
                usuario.biografia = solicitud.Biografia;

            if (!string.IsNullOrWhiteSpace(solicitud.Ciudad))
                usuario.ciudad = solicitud.Ciudad;

            if (!string.IsNullOrWhiteSpace(solicitud.Pais))
                usuario.pais = solicitud.Pais;

            if (!string.IsNullOrWhiteSpace(solicitud.Genero))
                usuario.genero = solicitud.Genero;

            if (solicitud.FechaNacimiento.HasValue)
            {
                usuario.fecha_nacimiento = DateOnly.FromDateTime(solicitud.FechaNacimiento.Value);
            }

            if (!string.IsNullOrWhiteSpace(solicitud.TelefonoEmergencia))
                usuario.telefono_emergencia = solicitud.TelefonoEmergencia;

            usuario.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            var roles = await ObtenerRolesUsuario(usuario.id);
            return MapearUsuarioResponse(usuario, roles);
        }

        public async Task<string> ActualizarFotoAsync(int id, IFormFile foto)
        {
            var usuario = await _contexto.usuarios.FindAsync(id);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            var rutaRelativa = await _archivosHelper.GuardarImagenAsync(foto, "perfiles", 5);

            if (!string.IsNullOrEmpty(usuario.foto_perfil_url))
            {
                var rutaAnterior = usuario.foto_perfil_url.Replace("/uploads/", "");
                _archivosHelper.EliminarArchivo(rutaAnterior);
            }

            usuario.foto_perfil_url = rutaRelativa;
            usuario.fecha_modificacion = DateTime.UtcNow;
            await _contexto.SaveChangesAsync();

            var baseUrl = _configuracion["AppUrl"] ?? "https://localhost:7247";
            return _archivosHelper.ObtenerUrlArchivo(rutaRelativa, baseUrl);
        }

        public async Task CambiarContrasenaAsync(int id, CambiarPasswordRequest solicitud)
        {
            var usuario = await _contexto.usuarios.FindAsync(id);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            if (!HashHelper.VerifyPassword(solicitud.CurrentPassword, usuario.password_hash))
            {
                throw new UnauthorizedAccessException("Contraseña actual incorrecta");
            }

            if (solicitud.NewPassword != solicitud.ConfirmPassword)
            {
                throw new ArgumentException("Las contraseñas nuevas no coinciden");
            }

            var salt = HashHelper.GenerateSalt();
            usuario.password_hash = HashHelper.HashPassword(solicitud.NewPassword, salt);
            usuario.salt = salt;
            usuario.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();
        }

        public async Task DesactivarAsync(int id)
        {
            var usuario = await _contexto.usuarios.FindAsync(id);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            if (usuario.id == 1)
            {
                throw new InvalidOperationException("No se puede desactivar el usuario super administrador principal");
            }

            usuario.activo = false;
            usuario.fecha_modificacion = DateTime.UtcNow;
            await _contexto.SaveChangesAsync();
        }

        public async Task<List<RolResponse>> ObtenerRolesAsync(int usuarioId)
        {
            var usuario = await _contexto.usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            var roles = await _contexto.usuarios_roles
                .Where(ur => ur.id_usuario == usuarioId && ur.estado == "ACTIVO" && ur.activo == true)
                .Join(_contexto.tipos_rols,
                    ur => ur.id_rol,
                    r => r.id,
                    (ur, r) => new { ur, r })
                .Select(x => new RolResponse
                {
                    Id = x.r.id,
                    Codigo = x.r.codigo,
                    Nombre = x.r.nombre,
                    NivelJerarquia = x.r.nivel_jerarquia,
                    IdTorneo = x.ur.id_torneo,
                    IdEquipo = x.ur.id_equipo,
                    FechaInicio = x.ur.fecha_inicio.ToDateTime(TimeOnly.MinValue),
                    FechaFin = x.ur.fecha_fin.HasValue ? x.ur.fecha_fin.Value.ToDateTime(TimeOnly.MinValue) : null,
                    Estado = x.ur.estado
                })
                .ToListAsync();

            return roles;
        }

        public async Task<RolResponse> AsignarRolAsync(int usuarioId, AsignarRolRequest solicitud)
        {
            var usuario = await _contexto.usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            var rol = await _contexto.tipos_rols.FindAsync(solicitud.IdRol);
            if (rol == null)
            {
                throw new KeyNotFoundException("Rol no encontrado");
            }

            if (solicitud.IdTorneo.HasValue)
            {
                var torneo = await _contexto.torneos.FindAsync(solicitud.IdTorneo.Value);
                if (torneo == null)
                {
                    throw new KeyNotFoundException("Torneo no encontrado");
                }
            }

            if (solicitud.IdEquipo.HasValue)
            {
                var equipo = await _contexto.equipos.FindAsync(solicitud.IdEquipo.Value);
                if (equipo == null)
                {
                    throw new KeyNotFoundException("Equipo no encontrado");
                }
            }

            var rolExistente = await _contexto.usuarios_roles
                .FirstOrDefaultAsync(ur => ur.id_usuario == usuarioId &&
                                           ur.id_rol == solicitud.IdRol &&
                                           ur.id_torneo == solicitud.IdTorneo &&
                                           ur.id_equipo == solicitud.IdEquipo &&
                                           ur.estado == "ACTIVO");

            if (rolExistente != null)
            {
                throw new InvalidOperationException("El usuario ya tiene este rol asignado");
            }

            var nuevoRol = new usuarios_role
            {
                codigo = CodigoHelper.GenerarCodigo("UR", 8),
                id_usuario = usuarioId,
                id_rol = solicitud.IdRol,
                id_torneo = solicitud.IdTorneo,
                id_equipo = solicitud.IdEquipo,
                fecha_inicio = DateOnly.FromDateTime(solicitud.FechaInicio),
                fecha_fin = solicitud.FechaFin.HasValue ? DateOnly.FromDateTime(solicitud.FechaFin.Value): null,
                estado = "ACTIVO",
                origen_asignacion = "MANUAL",
                fecha_asignacion = DateTime.UtcNow,
                activo = true
            };

            _contexto.usuarios_roles.Add(nuevoRol);
            await _contexto.SaveChangesAsync();

            return new RolResponse
            {
                Id = rol.id,
                Codigo = rol.codigo,
                Nombre = rol.nombre,
                NivelJerarquia = rol.nivel_jerarquia,
                IdTorneo = nuevoRol.id_torneo,
                IdEquipo = nuevoRol.id_equipo,
                FechaInicio = nuevoRol.fecha_inicio.ToDateTime(TimeOnly.MinValue),
                FechaFin = nuevoRol.fecha_fin.HasValue ? nuevoRol.fecha_fin.Value.ToDateTime(TimeOnly.MinValue): null,
                Estado = nuevoRol.estado
            };
        }

        public async Task RevocarRolAsync(int usuarioId, int rolId)
        {
            var rol = await _contexto.usuarios_roles
                .FirstOrDefaultAsync(ur => ur.id_usuario == usuarioId &&
                                           ur.id_rol == rolId &&
                                           ur.estado == "ACTIVO");

            if (rol == null)
            {
                throw new KeyNotFoundException("Rol no encontrado o ya está revocado");
            }

            rol.estado = "REVOCADO";
            rol.fecha_fin = DateOnly.FromDateTime(DateTime.UtcNow);
            rol.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();
        }

        private async Task<List<string>> ObtenerRolesUsuario(int usuarioId)
        {
            return await _contexto.usuarios_roles
                .Where(ur => ur.id_usuario == usuarioId && ur.estado == "ACTIVO" && ur.activo == true)
                .Join(_contexto.tipos_rols,
                    ur => ur.id_rol,
                    r => r.id,
                    (ur, r) => r.codigo)
                .ToListAsync();
        }

        private UsuarioResponse MapearUsuarioResponse(usuario usuario, List<string> roles)
        {
            return new UsuarioResponse
            {
                Id = usuario.id,
                Codigo = usuario.codigo,
                NombreCompleto = $"{usuario.nombres} {usuario.apellidos}",
                Email = usuario.email,
                FotoPerfil = usuario.foto_perfil_url,
                TipoUsuario = usuario.id_tipo_usuarioNavigation?.nombre ?? "",
                Roles = roles,
                Activo = usuario.activo ?? false,
                FechaRegistro = usuario.fecha_registro ?? DateTime.UtcNow
            };
        }

        private UsuarioDetalleResponse MapearUsuarioDetalleResponse(usuario usuario, List<string> roles)
        {
            return new UsuarioDetalleResponse
            {
                Id = usuario.id,
                Codigo = usuario.codigo,
                Nombres = usuario.nombres,
                Apellidos = usuario.apellidos,
                NombreCompleto = $"{usuario.nombres} {usuario.apellidos}",
                Email = usuario.email,
                Telefono = usuario.telefono,
                TelefonoEmergencia = usuario.telefono_emergencia,
                FotoPerfil = usuario.foto_perfil_url,
                PesoKg = usuario.peso_kg,
                AlturaCm = usuario.altura_cm,
                Biografia = usuario.biografia,
                Ciudad = usuario.ciudad,
                Pais = usuario.pais,
                Direccion = usuario.direccion,
                Genero = usuario.genero,
                FechaNacimiento = usuario.fecha_nacimiento.HasValue
                    ? usuario.fecha_nacimiento.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,
                TipoUsuario = usuario.id_tipo_usuarioNavigation?.nombre ?? "",
                Roles = roles,
                EmailVerificado = usuario.email_verificado ?? false,
                TelefonoVerificado = usuario.telefono_verificado ?? false,
                Activo = usuario.activo ?? false,
                FechaRegistro = usuario.fecha_registro ?? DateTime.UtcNow,
                FechaUltimaConexion = usuario.fecha_ultima_conexion
            };
        }
    }
}