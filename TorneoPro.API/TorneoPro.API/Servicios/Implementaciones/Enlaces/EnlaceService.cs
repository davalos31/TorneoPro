using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.Enlaces;
using TorneoPro.API.DTOs.Enlaces.Request;
using TorneoPro.API.DTOs.Enlaces.Response;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Enlaces;

namespace TorneoPro.API.Servicios.Implementaciones.Enlaces
{
    public class EnlaceService : IEnlaceService
    {
        private readonly TorneoProContext _contexto;
        private readonly ILogger<EnlaceService> _logger;
        private readonly IConfiguration _configuracion;

        public EnlaceService(
            TorneoProContext contexto,
            ILogger<EnlaceService> logger,
            IConfiguration configuracion)
        {
            _contexto = contexto;
            _logger = logger;
            _configuracion = configuracion;
        }

        public async Task<ResultadoPaginado<EnlaceResponse>> ObtenerMisEnlacesAsync(int usuarioId, FiltrarEnlaceRequest solicitud)
        {
            var query = _contexto.enlaces_compartidos
                .Include(e => e.id_tipo_enlaceNavigation)
                .Include(e => e.id_rol_asignadoNavigation)
                .Include(e => e.id_torneoNavigation)
                .Include(e => e.id_equipoNavigation)
                .Where(e => e.id_usuario_creador == usuarioId && e.activo == true)
                .AsQueryable();

            if (solicitud.IdTipoEnlace.HasValue)
                query = query.Where(e => e.id_tipo_enlace == solicitud.IdTipoEnlace.Value);

            if (solicitud.IdTorneo.HasValue)
                query = query.Where(e => e.id_torneo == solicitud.IdTorneo.Value);

            if (solicitud.IdEquipo.HasValue)
                query = query.Where(e => e.id_equipo == solicitud.IdEquipo.Value);

            if (!string.IsNullOrWhiteSpace(solicitud.Estado))
                query = query.Where(e => e.estado == solicitud.Estado);

            if (solicitud.SoloActivos == true)
                query = query.Where(e => e.estado == "ACTIVO");

            if (solicitud.FechaDesde.HasValue)
                query = query.Where(e => e.fecha_creacion >= solicitud.FechaDesde.Value);

            if (solicitud.FechaHasta.HasValue)
                query = query.Where(e => e.fecha_creacion <= solicitud.FechaHasta.Value);

            var totalItems = await query.CountAsync();

            query = query.OrderByDescending(e => e.fecha_creacion);

            var enlaces = await query
                .Skip((solicitud.Pagina - 1) * solicitud.TamanoPagina)
                .Take(solicitud.TamanoPagina)
                .ToListAsync();

            var baseUrl = _configuracion["AppUrl"] ?? "https://localhost:7247";
            var items = enlaces.Select(e => MapearEnlaceResponse(e, baseUrl)).ToList();

            return ResultadoPaginado<EnlaceResponse>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
        }

        public async Task<EnlaceResponse?> ObtenerEnlacePorIdAsync(int id, int usuarioId)
        {
            var enlace = await _contexto.enlaces_compartidos
                .Include(e => e.id_tipo_enlaceNavigation)
                .Include(e => e.id_rol_asignadoNavigation)
                .Include(e => e.id_torneoNavigation)
                .Include(e => e.id_equipoNavigation)
                .Include(e => e.id_usuario_creadorNavigation)
                .FirstOrDefaultAsync(e => e.id == id && e.id_usuario_creador == usuarioId);

            if (enlace == null)
                return null;

            var baseUrl = _configuracion["AppUrl"] ?? "https://localhost:7247";
            return MapearEnlaceResponse(enlace, baseUrl);
        }

        public async Task<EnlaceResponse> CrearEnlaceCapitanAsync(int usuarioId, CrearEnlaceRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            return await CrearEnlaceBaseAsync(usuarioId, solicitud, "CAPITAN", ipAddress, userAgent);
        }

        public async Task<EnlaceResponse> CrearEnlaceArbitroAsync(int usuarioId, CrearEnlaceRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            return await CrearEnlaceBaseAsync(usuarioId, solicitud, "ARBITRO", ipAddress, userAgent);
        }

        public async Task<EnlaceResponse> CrearEnlaceSubAdminAsync(int usuarioId, CrearEnlaceRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            return await CrearEnlaceBaseAsync(usuarioId, solicitud, "SUB_ADMIN", ipAddress, userAgent);
        }

        public async Task<EnlaceResponse> CrearEnlaceJugadorAsync(int usuarioId, CrearEnlaceRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            return await CrearEnlaceBaseAsync(usuarioId, solicitud, "JUGADOR", ipAddress, userAgent);
        }

        public async Task<List<TipoEnlaceResponse>> ObtenerTiposEnlaceAsync()
        {
            var tipos = await _contexto.tipos_enlaces
                .Where(t => t.activo == true)
                .Select(t => new TipoEnlaceResponse
                {
                    Id = t.id,
                    Codigo = t.codigo,
                    Nombre = t.nombre,
                    Alcance = t.alcance,
                    PermiteExpiracion = t.permite_expiracion ?? false
                })
                .ToListAsync();

            return tipos;
        }

        public async Task<EnlaceEstadisticasResponse> ObtenerEstadisticasAsync(int usuarioId)
        {
            var enlaces = await _contexto.enlaces_compartidos
                .Where(e => e.id_usuario_creador == usuarioId && e.activo == true)
                .ToListAsync();

            var totalEnlaces = enlaces.Count;
            var enlacesActivos = enlaces.Count(e => e.estado == "ACTIVO");
            var enlacesExpirados = enlaces.Count(e => e.estado == "EXPIRADO");
            var enlacesAgotados = enlaces.Count(e => e.estado == "AGOTADO");
            var totalUsos = enlaces.Sum(e => e.usos_actuales ?? 0);
            var idsEnlaces = enlaces.Select(e => e.id).ToList();
            var totalUsosExitosos = await _contexto.enlaces_historial_usos
                .Where(u => idsEnlaces.Contains(u.id_enlace) && u.uso_exitoso == true)
                .CountAsync();

            return new EnlaceEstadisticasResponse
            {
                TotalEnlaces = totalEnlaces,
                EnlacesActivos = enlacesActivos,
                EnlacesExpirados = enlacesExpirados,
                EnlacesAgotados = enlacesAgotados,
                TotalUsos = totalUsos,
                TotalUsosExitosos = totalUsosExitosos,
                PromedioUsosPorEnlace = totalEnlaces > 0 ? (double)totalUsos / totalEnlaces : 0
            };
        }

        public async Task<EnlaceResponse> RegenerarCodigoEnlaceAsync(int id, int usuarioId, string? ipAddress = null)
        {
            var enlace = await _contexto.enlaces_compartidos
                .Include(e => e.id_tipo_enlaceNavigation)
                .Include(e => e.id_rol_asignadoNavigation)
                .Include(e => e.id_torneoNavigation)
                .Include(e => e.id_equipoNavigation)
                .FirstOrDefaultAsync(e => e.id == id && e.id_usuario_creador == usuarioId);

            if (enlace == null)
                throw new KeyNotFoundException("Enlace no encontrado o no tiene permisos");

            if (enlace.estado != "ACTIVO")
                throw new InvalidOperationException($"No se puede regenerar el código de un enlace {enlace.estado?.ToLowerInvariant()}");

            var nuevoCodigo = CodigoHelper.GenerarCodigoEnlace();
            var codigoAnterior = enlace.codigo_enlace;
            enlace.codigo_enlace = nuevoCodigo;

            var metadata = string.IsNullOrEmpty(enlace.metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(enlace.metadata ?? "{}") ?? new();

            var historialRegeneraciones = metadata.ContainsKey("historial_regeneraciones")
                ? JsonSerializer.Deserialize<List<Dictionary<string, object>>>(metadata["historial_regeneraciones"]?.ToString() ?? "[]")
                : new List<Dictionary<string, object>>();

            historialRegeneraciones.Add(new Dictionary<string, object>
            {
                ["fecha"] = DateTime.UtcNow,
                ["codigo_anterior"] = codigoAnterior,
                ["codigo_nuevo"] = nuevoCodigo,
                ["realizado_por"] = usuarioId,
                ["ip"] = ipAddress ?? "unknown"
            });

            metadata["historial_regeneraciones"] = historialRegeneraciones;
            metadata["ultima_regeneracion"] = DateTime.UtcNow;
            metadata["ultimo_regenerador"] = usuarioId;

            enlace.metadata = JsonSerializer.Serialize(metadata);


            await _contexto.SaveChangesAsync();

            var baseUrl = _configuracion["AppUrl"] ?? "https://localhost:7247";
            return MapearEnlaceResponse(enlace, baseUrl);
        }

        private async Task<EnlaceResponse> CrearEnlaceBaseAsync(int usuarioId, CrearEnlaceRequest solicitud, string tipoEsperado, string? ipAddress = null, string? userAgent = null)
        {
            // Validar tipo de enlace
            var tipoEnlace = await _contexto.tipos_enlaces
                .FirstOrDefaultAsync(t => t.id == solicitud.IdTipoEnlace && t.activo == true);

            if (tipoEnlace == null)
                throw new KeyNotFoundException("Tipo de enlace no encontrado");

            // Validar rol a asignar
            var rol = await _contexto.tipos_rols
                .FirstOrDefaultAsync(r => r.id == solicitud.IdRolAsignado && r.activo == true);

            if (rol == null)
                throw new KeyNotFoundException("Rol no encontrado");

            // Validar permisos del creador
            await ValidarPermisosCreadorAsync(usuarioId, rol, solicitud);

            // Validar torneo si aplica
            if (solicitud.IdTorneo.HasValue)
            {
                var torneo = await _contexto.torneos.FindAsync(solicitud.IdTorneo.Value);
                if (torneo == null)
                    throw new KeyNotFoundException("Torneo no encontrado");
            }

            // Validar equipo si aplica
            if (solicitud.IdEquipo.HasValue)
            {
                var equipo = await _contexto.equipos.FindAsync(solicitud.IdEquipo.Value);
                if (equipo == null)
                    throw new KeyNotFoundException("Equipo no encontrado");

                // Verificar que el creador sea capitán o admin del equipo
                var esAdmin = await EsAdminAsync(usuarioId);
                var esCapitan = equipo.id_capitan == usuarioId;

                if (!esAdmin && !esCapitan)
                    throw new UnauthorizedAccessException("No tiene permisos para crear enlaces para este equipo");
            }

            // Validar fecha de expiración
            if (solicitud.FechaExpiracion.HasValue && solicitud.FechaExpiracion.Value <= DateTime.UtcNow)
                throw new InvalidOperationException("La fecha de expiración debe ser futura");

            var codigoEnlace = CodigoHelper.GenerarCodigoEnlace();

            var enlace = new enlaces_compartido
            {
                codigo = CodigoHelper.GenerarCodigo("ENL", 10),
                codigo_enlace = codigoEnlace,
                id_tipo_enlace = solicitud.IdTipoEnlace,
                id_usuario_creador = usuarioId,
                id_rol_asignado = solicitud.IdRolAsignado,
                id_torneo = solicitud.IdTorneo,
                id_equipo = solicitud.IdEquipo,
                fecha_creacion = DateTime.UtcNow,
                fecha_expiracion = solicitud.FechaExpiracion,
                max_usos = solicitud.MaxUsos,
                usos_actuales = 0,
                estado = "ACTIVO",
                activo = true
            };

            _contexto.enlaces_compartidos.Add(enlace);
            await _contexto.SaveChangesAsync();

            var baseUrl = _configuracion["AppUrl"] ?? "https://localhost:7247";
            return MapearEnlaceResponse(enlace, baseUrl);
        }

        public async Task<EnlaceInfoResponse> ObtenerInfoEnlaceAsync(string codigoEnlace)
        {
            var enlace = await _contexto.enlaces_compartidos
                .Include(e => e.id_tipo_enlaceNavigation)
                .Include(e => e.id_rol_asignadoNavigation)
                .Include(e => e.id_torneoNavigation)
                .Include(e => e.id_equipoNavigation)
                .FirstOrDefaultAsync(e => e.codigo_enlace == codigoEnlace && e.activo == true);

            var respuesta = new EnlaceInfoResponse
            {
                CodigoEnlace = codigoEnlace,
                Valido = false
            };

            if (enlace == null)
            {
                respuesta.Mensaje = "Enlace no encontrado";
                return respuesta;
            }

            respuesta.TipoEnlace = enlace.id_tipo_enlaceNavigation?.nombre ?? "";
            respuesta.RolAsignado = enlace.id_rol_asignadoNavigation?.nombre ?? "";
            respuesta.Torneo = enlace.id_torneoNavigation?.nombre;
            respuesta.Equipo = enlace.id_equipoNavigation?.nombre;
            respuesta.FechaExpiracion = enlace.fecha_expiracion;

            if (enlace.max_usos.HasValue)
                respuesta.UsosRestantes = enlace.max_usos.Value - (enlace.usos_actuales ?? 0);

            // Validar estado del enlace
            if (enlace.estado != "ACTIVO")
            {
                respuesta.Mensaje = $"El enlace está {enlace.estado?.ToLowerInvariant()}";
                return respuesta;
            }

            if (enlace.fecha_expiracion.HasValue && enlace.fecha_expiracion.Value < DateTime.UtcNow)
            {
                respuesta.Mensaje = "El enlace ha expirado";
                return respuesta;
            }

            if (enlace.max_usos.HasValue && (enlace.usos_actuales ?? 0) >= enlace.max_usos.Value)
            {
                respuesta.Mensaje = "El enlace ha alcanzado el número máximo de usos";
                return respuesta;
            }

            respuesta.Valido = true;
            respuesta.Mensaje = "Enlace válido";

            return respuesta;
        }

        public async Task<RolAsignadoResponse> UsarEnlaceAsync(string codigoEnlace, int usuarioId, string? ipAddress, string? userAgent)
        {
            var enlace = await _contexto.enlaces_compartidos
                .Include(e => e.id_tipo_enlaceNavigation)
                .Include(e => e.id_rol_asignadoNavigation)
                .Include(e => e.id_torneoNavigation)
                .Include(e => e.id_equipoNavigation)
                .FirstOrDefaultAsync(e => e.codigo_enlace == codigoEnlace && e.activo == true);

            if (enlace == null)
            {
                await RegistrarUsoEnlace(null, usuarioId, null, null, false, "Enlace no encontrado", ipAddress, userAgent);
                throw new InvalidOperationException("Enlace no encontrado");
            }

            // Validar que el creador no use su propio enlace (excepto admin)
            if (enlace.id_usuario_creador == usuarioId && !await EsAdminAsync(usuarioId))
            {
                await RegistrarUsoEnlace(enlace.id, usuarioId, null, null, false, "El creador no puede usar su propio enlace", ipAddress, userAgent);
                throw new InvalidOperationException("No puedes usar un enlace que tú mismo creaste");
            }

            // Validar estado del enlace
            if (enlace.estado != "ACTIVO")
            {
                await RegistrarUsoEnlace(enlace.id, usuarioId, null, null, false, $"Enlace {enlace.estado}", ipAddress, userAgent);
                throw new InvalidOperationException($"El enlace está {enlace.estado?.ToLowerInvariant()}");
            }

            if (enlace.fecha_expiracion.HasValue && enlace.fecha_expiracion.Value < DateTime.UtcNow)
            {
                await RegistrarUsoEnlace(enlace.id, usuarioId, null, null, false, "Enlace expirado", ipAddress, userAgent);
                throw new InvalidOperationException("El enlace ha expirado");
            }

            if (enlace.max_usos.HasValue && (enlace.usos_actuales ?? 0) >= enlace.max_usos.Value)
            {
                await RegistrarUsoEnlace(enlace.id, usuarioId, null, null, false, "Máximo de usos alcanzado", ipAddress, userAgent);
                throw new InvalidOperationException("El enlace ha alcanzado el número máximo de usos");
            }

            var usuario = await _contexto.usuarios.FindAsync(usuarioId);
            if (usuario == null)
                throw new KeyNotFoundException("Usuario no encontrado");

            // Verificar si el usuario ya tiene el rol
            var rolExistente = await _contexto.usuarios_roles
                .FirstOrDefaultAsync(ur => ur.id_usuario == usuarioId &&
                                           ur.id_rol == enlace.id_rol_asignado &&
                                           ur.id_torneo == enlace.id_torneo &&
                                           ur.id_equipo == enlace.id_equipo &&
                                           ur.estado == "ACTIVO");

            int? rolAnteriorId = null;

            if (rolExistente != null)
            {
                rolAnteriorId = rolExistente.id_rol;
                await RegistrarUsoEnlace(enlace.id, usuarioId, rolAnteriorId, enlace.id_rol_asignado, false, "Usuario ya tiene este rol", ipAddress, userAgent);
                throw new InvalidOperationException("El usuario ya tiene este rol asignado");
            }

            // Asignar el rol
            var nuevoRol = new usuarios_role
            {
                codigo = CodigoHelper.GenerarCodigo("UR", 8),
                id_usuario = usuarioId,
                id_rol = enlace.id_rol_asignado,
                id_torneo = enlace.id_torneo,
                id_equipo = enlace.id_equipo,
                fecha_asignacion = DateTime.UtcNow,
                fecha_inicio = DateOnly.FromDateTime(DateTime.UtcNow),
                estado = "ACTIVO",
                origen_asignacion = "ENLACE",
                id_enlace_origen = enlace.id,
                activo = true
            };

            _contexto.usuarios_roles.Add(nuevoRol);

            // Incrementar uso del enlace
            enlace.usos_actuales = (enlace.usos_actuales ?? 0) + 1;

            // Si alcanzó el máximo, cambiar estado
            if (enlace.max_usos.HasValue && enlace.usos_actuales >= enlace.max_usos.Value)
            {
                enlace.estado = "AGOTADO";
            }

            await _contexto.SaveChangesAsync();

            // Registrar uso exitoso
            await RegistrarUsoEnlace(enlace.id, usuarioId, rolAnteriorId, enlace.id_rol_asignado, true, null, ipAddress, userAgent);

            return new RolAsignadoResponse
            {
                IdRol = enlace.id_rol_asignado,
                Rol = enlace.id_rol_asignadoNavigation?.nombre ?? "",
                IdTorneo = enlace.id_torneo,
                Torneo = enlace.id_torneoNavigation?.nombre,
                IdEquipo = enlace.id_equipo,
                Equipo = enlace.id_equipoNavigation?.nombre,
                FechaInicio = DateTime.UtcNow,
                FechaFin = null
            };
        }

        public async Task DesactivarEnlaceAsync(int id, int usuarioId, string? motivo = null)
        {
            var enlace = await _contexto.enlaces_compartidos
                .FirstOrDefaultAsync(e => e.id == id && e.id_usuario_creador == usuarioId);

            if (enlace == null)
                throw new KeyNotFoundException("Enlace no encontrado o no tiene permisos");

            enlace.estado = "DESACTIVADO";
            enlace.fecha_desactivacion = DateTime.UtcNow;
            enlace.motivo_desactivacion = motivo;

            await _contexto.SaveChangesAsync();
        }

        public async Task<List<UsoEnlaceResponse>> ObtenerHistorialUsosAsync(int enlaceId, int usuarioId)
        {
            var enlace = await _contexto.enlaces_compartidos
                .FirstOrDefaultAsync(e => e.id == enlaceId && e.id_usuario_creador == usuarioId);

            if (enlace == null)
                throw new KeyNotFoundException("Enlace no encontrado o no tiene permisos");

            var usos = await _contexto.enlaces_historial_usos
                .Include(u => u.id_usuarioNavigation)
                .Include(u => u.id_rol_anteriorNavigation)
                .Include(u => u.id_rol_nuevoNavigation)
                .Where(u => u.id_enlace == enlaceId)
                .OrderByDescending(u => u.fecha_uso)
                .ToListAsync();

            var respuesta = new List<UsoEnlaceResponse>();
            foreach (var uso in usos)
            {
                respuesta.Add(new UsoEnlaceResponse
                {
                    Id = uso.id,
                    IdUsuario = uso.id_usuario,
                    Usuario = $"{uso.id_usuarioNavigation?.nombres} {uso.id_usuarioNavigation?.apellidos}",
                    Email = uso.id_usuarioNavigation?.email,
                    FechaUso = uso.fecha_uso ?? DateTime.UtcNow,
                    IpAddress = uso.ip_address,
                    IdRolAnterior = uso.id_rol_anterior,
                    RolAnterior = uso.id_rol_anteriorNavigation?.nombre,
                    IdRolNuevo = uso.id_rol_nuevo,
                    RolNuevo = uso.id_rol_nuevoNavigation?.nombre,
                    UsoExitoso = uso.uso_exitoso ?? true,
                    MotivoFallo = uso.motivo_fallo
                });
            }

            return respuesta;
        }

        public async Task<EnlaceResponse> RenovarEnlaceAsync(int id, int usuarioId, DateTime? nuevaFechaExpiracion = null, int? nuevoMaxUsos = null)
        {
            var enlace = await _contexto.enlaces_compartidos
                .Include(e => e.id_tipo_enlaceNavigation)
                .Include(e => e.id_rol_asignadoNavigation)
                .Include(e => e.id_torneoNavigation)
                .Include(e => e.id_equipoNavigation)
                .FirstOrDefaultAsync(e => e.id == id && e.id_usuario_creador == usuarioId);

            if (enlace == null)
                throw new KeyNotFoundException("Enlace no encontrado o no tiene permisos");

            if (enlace.estado != "EXPIRADO" && enlace.estado != "AGOTADO" && enlace.estado != "DESACTIVADO")
                throw new InvalidOperationException($"Solo se pueden renovar enlaces expirados, agotados o desactivados. Estado actual: {enlace.estado}");

            // Reactivar enlace
            enlace.estado = "ACTIVO";
            enlace.fecha_desactivacion = null;
            enlace.motivo_desactivacion = null;

            if (nuevaFechaExpiracion.HasValue)
            {
                if (nuevaFechaExpiracion.Value <= DateTime.UtcNow)
                    throw new InvalidOperationException("La fecha de expiración debe ser futura");
                enlace.fecha_expiracion = nuevaFechaExpiracion;
            }

            if (nuevoMaxUsos.HasValue)
            {
                if (nuevoMaxUsos.Value <= (enlace.usos_actuales ?? 0))
                    throw new InvalidOperationException("El nuevo máximo de usos debe ser mayor a los usos actuales");
                enlace.max_usos = nuevoMaxUsos;
            }

            await _contexto.SaveChangesAsync();

            var baseUrl = _configuracion["AppUrl"] ?? "https://localhost:7247";
            return MapearEnlaceResponse(enlace, baseUrl);
        }

        #region Métodos Privados

        private async Task<bool> EsAdminAsync(int usuarioId)
        {
            return await _contexto.usuarios_roles
                .AnyAsync(ur => ur.id_usuario == usuarioId &&
                               (ur.id_rol == 1 || ur.id_rol == 2) &&
                               ur.estado == "ACTIVO");
        }

        private async Task ValidarPermisosCreadorAsync(int usuarioId, tipos_rol rol, CrearEnlaceRequest solicitud)
        {
            var esAdmin = await EsAdminAsync(usuarioId);

            // Los admins pueden crear cualquier tipo de enlace
            if (esAdmin)
                return;

            // Los capitanes solo pueden crear enlaces para su equipo
            if (rol.codigo == "CAPITAN" || rol.codigo == "JUGADOR")
            {
                if (!solicitud.IdEquipo.HasValue)
                    throw new InvalidOperationException("Debe especificar un equipo para este tipo de enlace");

                var equipo = await _contexto.equipos.FindAsync(solicitud.IdEquipo.Value);
                if (equipo == null)
                    throw new KeyNotFoundException("Equipo no encontrado");

                if (equipo.id_capitan != usuarioId)
                    throw new UnauthorizedAccessException("Solo el capitán puede crear enlaces para este equipo");
            }
            else
            {
                throw new UnauthorizedAccessException("No tiene permisos para crear este tipo de enlace");
            }
        }

        private async Task RegistrarUsoEnlace(int? enlaceId, int usuarioId, int? rolAnteriorId, int? rolNuevoId, bool exitoso, string? motivo, string? ipAddress, string? userAgent)
        {
            // Solo registrar si hay un enlace válido
            if (!enlaceId.HasValue || enlaceId.Value == 0)
                return;

            var uso = new enlaces_historial_uso
            {
                codigo = CodigoHelper.GenerarCodigo("USE", 10),
                id_enlace = enlaceId.Value,
                id_usuario = usuarioId,
                fecha_uso = DateTime.UtcNow,
                ip_address = ipAddress?.Length > 50 ? ipAddress[..50] : ipAddress,
                user_agent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
                id_rol_anterior = rolAnteriorId,
                id_rol_nuevo = rolNuevoId ?? 0,  // La BD requiere NOT NULL
                uso_exitoso = exitoso,
                motivo_fallo = motivo
            };

            _contexto.enlaces_historial_usos.Add(uso);
            await _contexto.SaveChangesAsync();
        }

        private EnlaceResponse MapearEnlaceResponse(enlaces_compartido enlace, string baseUrl)
        {
            var expirado = enlace.fecha_expiracion.HasValue && enlace.fecha_expiracion.Value < DateTime.UtcNow;
            var agotado = enlace.max_usos.HasValue && (enlace.usos_actuales ?? 0) >= enlace.max_usos.Value;

            return new EnlaceResponse
            {
                Id = enlace.id,
                Codigo = enlace.codigo,
                CodigoEnlace = enlace.codigo_enlace,
                UrlCompleta = $"{baseUrl}/api/enlaces/usar/{enlace.codigo_enlace}",
                IdTipoEnlace = enlace.id_tipo_enlace,
                TipoEnlace = enlace.id_tipo_enlaceNavigation?.nombre ?? "",
                IdUsuarioCreador = enlace.id_usuario_creador,
                UsuarioCreador = enlace.id_usuario_creadorNavigation != null
                    ? $"{enlace.id_usuario_creadorNavigation.nombres} {enlace.id_usuario_creadorNavigation.apellidos}"
                    : "",
                IdRolAsignado = enlace.id_rol_asignado,
                RolAsignado = enlace.id_rol_asignadoNavigation?.nombre ?? "",
                IdTorneo = enlace.id_torneo,
                Torneo = enlace.id_torneoNavigation?.nombre,
                IdEquipo = enlace.id_equipo,
                Equipo = enlace.id_equipoNavigation?.nombre,
                FechaCreacion = enlace.fecha_creacion ?? DateTime.UtcNow,
                FechaExpiracion = enlace.fecha_expiracion,
                MaxUsos = enlace.max_usos,
                UsosActuales = enlace.usos_actuales ?? 0,
                Estado = enlace.estado ?? "ACTIVO",
                EstaExpirado = expirado,
                EstaAgotado = agotado
            };
        }

        #endregion
    }
}