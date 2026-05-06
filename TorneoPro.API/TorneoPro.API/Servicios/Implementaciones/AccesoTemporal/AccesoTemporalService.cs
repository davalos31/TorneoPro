using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TorneoPro.API.Controllers;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.AccesoTemporal.Request;
using TorneoPro.API.DTOs.AccesoTemporal.Response;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces.AccesoTemporal;
using TorneoPro.API.Servicios.Interfaces.Email;
using DeepLinkInfoResponse = TorneoPro.API.DTOs.AccesoTemporal.Response.DeepLinkInfoResponse;

namespace TorneoPro.API.Servicios.Implementaciones.AccesoTemporal
{
    /// <summary>
    /// Servicio para la gestión de accesos temporales, invitaciones y enlaces compartidos
    /// </summary>
    public class AccesoTemporalService : IAccesoTemporalService
    {
        private readonly TorneoProContext _contexto;
        private readonly ILogger<AccesoTemporalService> _logger;
        private readonly IConfiguration _configuracion;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Caché para IDs
        private static Dictionary<string, int>? _cacheTiposEnlace;
        private static Dictionary<string, int>? _cacheRoles;
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);

        public AccesoTemporalService(
            TorneoProContext contexto,
            ILogger<AccesoTemporalService> logger,
            IConfiguration configuracion,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contexto = contexto;
            _logger = logger;
            _configuracion = configuracion;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Métodos Privados Helpers

        private string ObtenerBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return _configuracion["AppConfig:AppUrl"] ?? "https://torneopro.com";
            return $"{request.Scheme}://{request.Host}";
        }

        private (string BaseUrl, string DeepLinkScheme, string DeepLinkHost, string InviteUrl, string DeepLink) ObtenerUrls(string token)
        {
            var baseUrl = _configuracion["AppConfig:AppUrl"] ?? ObtenerBaseUrl();
            var deepLinkScheme = _configuracion["AppConfig:DeepLink:Scheme"] ?? "torneopro";
            var deepLinkHost = _configuracion["AppConfig:DeepLink:Host"] ?? "invite";

            var inviteUrl = string.IsNullOrEmpty(token) ? baseUrl : $"{baseUrl}/invite/{token}";
            var deepLink = string.IsNullOrEmpty(token) ? $"{deepLinkScheme}://{deepLinkHost}" : $"{deepLinkScheme}://{deepLinkHost}/open?token={token}";

            return (baseUrl, deepLinkScheme, deepLinkHost, inviteUrl, deepLink);
        }

        private string GenerarTokenUnico()
        {
            string token;
            bool existe;
            do
            {
                token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "")
                    .ToLowerInvariant();
                existe = _contexto.enlaces_compartidos.Any(e => e.codigo_enlace == token);
            } while (existe);

            return token;
        }

        private async Task<int> ObtenerIdTipoEnlaceAsync(string codigo)
        {
            await _cacheLock.WaitAsync();
            try
            {
                if (_cacheTiposEnlace == null)
                {
                    _cacheTiposEnlace = await _contexto.tipos_enlaces
                        .Where(t => t.activo == true)
                        .ToDictionaryAsync(t => t.codigo, t => t.id);
                }

                if (_cacheTiposEnlace.TryGetValue(codigo, out var id))
                    return id;

                throw new KeyNotFoundException($"Tipo de enlace '{codigo}' no encontrado");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task<int> ObtenerIdRolPorCodigoAsync(string codigo)
        {
            await _cacheLock.WaitAsync();
            try
            {
                if (_cacheRoles == null)
                {
                    _cacheRoles = await _contexto.tipos_rols
                        .Where(r => r.activo == true)
                        .ToDictionaryAsync(r => r.codigo, r => r.id);
                }

                if (_cacheRoles.TryGetValue(codigo, out var id))
                    return id;

                throw new KeyNotFoundException($"Rol '{codigo}' no encontrado");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task<bool> EsAdminAsync(int usuarioId)
        {
            return await _contexto.usuarios_roles
                .AnyAsync(ur => ur.id_usuario == usuarioId &&
                               (ur.id_rol == 1 || ur.id_rol == 2) &&
                               ur.estado == "ACTIVO");
        }

        private int ObtenerMaxUsosPorTipo(TipoEntidad tipoEntidad, TipoUsuario destinatario)
        {
            return (tipoEntidad, destinatario) switch
            {
                (TipoEntidad.EQUIPO, TipoUsuario.JUGADOR) => 1,
                (TipoEntidad.PARTIDO, TipoUsuario.ARBITRO) => 10,
                (TipoEntidad.PARTIDO, TipoUsuario.CAPITAN) => 5,
                (TipoEntidad.PARTIDO, TipoUsuario.JUGADOR) => 1,
                _ => 10
            };
        }

        private async Task ValidarEntidadAsync(TipoEntidad tipoEntidad, int idEntidad)
        {
            switch (tipoEntidad)
            {
                case TipoEntidad.PARTIDO:
                    if (!await _contexto.partidos.AnyAsync(p => p.id == idEntidad))
                        throw new KeyNotFoundException("Partido no encontrado");
                    break;
                case TipoEntidad.TORNEO:
                    if (!await _contexto.torneos.AnyAsync(t => t.id == idEntidad))
                        throw new KeyNotFoundException("Torneo no encontrado");
                    break;
                case TipoEntidad.ACTA_DIGITAL:
                    if (!await _contexto.actas_partidos.AnyAsync(a => a.id == idEntidad))
                        throw new KeyNotFoundException("Acta no encontrada");
                    break;
                case TipoEntidad.EQUIPO:
                    if (!await _contexto.equipos.AnyAsync(e => e.id == idEntidad))
                        throw new KeyNotFoundException("Equipo no encontrado");
                    break;
            }
        }

        private async Task<string> ObtenerNombreEntidadAsync(TipoEntidad tipoEntidad, int idEntidad)
        {
            return tipoEntidad switch
            {
                TipoEntidad.PARTIDO => await ObtenerNombrePartidoAsync(idEntidad),
                TipoEntidad.TORNEO => await ObtenerNombreTorneoAsync(idEntidad),
                TipoEntidad.ACTA_DIGITAL => await ObtenerNombreActaAsync(idEntidad),
                TipoEntidad.EQUIPO => await ObtenerNombreEquipoAsync(idEntidad),
                _ => "Entidad no especificada"
            };
        }

        private async Task<string> ObtenerNombrePartidoAsync(int idPartido)
        {
            var partido = await _contexto.partidos
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .FirstOrDefaultAsync(p => p.id == idPartido);
            return partido != null
                ? $"{partido.id_equipo_localNavigation?.nombre} vs {partido.id_equipo_visitanteNavigation?.nombre}"
                : "Partido no encontrado";
        }

        private async Task<string> ObtenerNombreTorneoAsync(int idTorneo)
        {
            var torneo = await _contexto.torneos.FindAsync(idTorneo);
            return torneo?.nombre ?? "Torneo no encontrado";
        }

        private async Task<string> ObtenerNombreActaAsync(int idActa)
        {
            var acta = await _contexto.actas_partidos
                .Include(a => a.id_partidoNavigation)
                    .ThenInclude(p => p.id_equipo_localNavigation)
                .FirstOrDefaultAsync(a => a.id == idActa);
            return acta?.id_partidoNavigation != null
                ? $"Acta - {acta.id_partidoNavigation.id_equipo_localNavigation?.nombre} vs {acta.id_partidoNavigation.id_equipo_visitanteNavigation?.nombre}"
                : "Acta no encontrada";
        }

        private async Task<string> ObtenerNombreEquipoAsync(int idEquipo)
        {
            var equipo = await _contexto.equipos.FindAsync(idEquipo);
            return equipo?.nombre ?? "Equipo no encontrado";
        }

        private string ObtenerTituloInvitacion(TipoEntidad tipoEntidad, string nombreEntidad)
        {
            return tipoEntidad switch
            {
                TipoEntidad.PARTIDO => $"Partido: {nombreEntidad}",
                TipoEntidad.TORNEO => $"Torneo: {nombreEntidad}",
                TipoEntidad.EQUIPO => $"Invitación al equipo {nombreEntidad}",
                TipoEntidad.ACTA_DIGITAL => $"Acta Digital - {nombreEntidad}",
                _ => "Invitación TorneoPro"
            };
        }

        private string ObtenerMensajeInvitacion(TipoEntidad tipoEntidad)
        {
            return tipoEntidad switch
            {
                TipoEntidad.PARTIDO => "Has sido asignado a este partido",
                TipoEntidad.TORNEO => "Has sido invitado a este torneo",
                TipoEntidad.EQUIPO => "Un administrador te ha invitado a unirte a este equipo",
                TipoEntidad.ACTA_DIGITAL => "El acta de este partido está disponible para tu revisión",
                _ => "Tienes una invitación pendiente"
            };
        }

        private async Task RegistrarUsoEnlace(int? enlaceId, int usuarioId, bool exitoso, string? motivo, string? ipAddress, string? userAgent)
        {
            if (!enlaceId.HasValue || enlaceId.Value == 0) return;

            var rolesUsuario = await _contexto.usuarios_roles
                .Where(ur => ur.id_usuario == usuarioId && ur.estado == "ACTIVO")
                .Select(ur => ur.id_rol)
                .ToListAsync();

            var rolJugador = await ObtenerIdRolPorCodigoAsync("JUGADOR");
            int? rolAnterior = rolesUsuario.Any() ? rolesUsuario.First() : null;
            int rolNuevo = rolesUsuario.Any() ? rolesUsuario.First() : rolJugador;

            var uso = new enlaces_historial_uso
            {
                codigo = CodigoHelper.GenerarCodigo("USE", 10),
                id_enlace = enlaceId.Value,
                id_usuario = usuarioId,
                fecha_uso = DateTime.UtcNow,
                ip_address = ipAddress?.Length > 50 ? ipAddress[..50] : ipAddress,
                user_agent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
                uso_exitoso = exitoso,
                motivo_fallo = motivo,
                id_rol_anterior = rolAnterior,
                id_rol_nuevo = rolNuevo
            };

            await _contexto.enlaces_historial_usos.AddAsync(uso);
            await _contexto.SaveChangesAsync();
        }

        #endregion

        #region Creación de Enlaces

        public async Task<EnlaceTemporalResponse> CrearEnlaceTemporalAsync(
            int usuarioIdCreador,
            EnlaceTemporalRequest solicitud,
            string? ipAddress = null,
            string? userAgent = null)
        {
            _logger.LogInformation("Creando enlace temporal - CreadorId: {CreadorId}, TipoEntidad: {TipoEntidad}, IdEntidad: {IdEntidad}",
                usuarioIdCreador, solicitud.TipoEntidad, solicitud.IdEntidad);

            if (!await EsAdminAsync(usuarioIdCreador))
                throw new UnauthorizedAccessException("No tiene permisos para crear enlaces temporales");

            await ValidarEntidadAsync(solicitud.TipoEntidad, solicitud.IdEntidad);

            if (solicitud.IdUsuarioDestino.HasValue && !await _contexto.usuarios.AnyAsync(u => u.id == solicitud.IdUsuarioDestino.Value))
                throw new KeyNotFoundException("Usuario destino no encontrado");

            if (solicitud.HorasValidez < 1 || solicitud.HorasValidez > 720)
                throw new InvalidOperationException("Las horas de validez deben estar entre 1 y 720");

            var token = GenerarTokenUnico();
            var fechaExpiracion = DateTime.UtcNow.AddHours(solicitud.HorasValidez);
            var idTipoEnlace = await ObtenerIdTipoEnlaceAsync("ENLACE_TEMPORAL");
            var idRolAsignado = await ObtenerIdRolPorCodigoAsync(solicitud.Destinatario.ToString());
            var maxUsos = ObtenerMaxUsosPorTipo(solicitud.TipoEntidad, solicitud.Destinatario);

            var metadata = new Dictionary<string, object>
            {
                ["tipo_entidad"] = solicitud.TipoEntidad.ToString(),
                ["id_entidad"] = solicitud.IdEntidad,
                ["destinatario"] = solicitud.Destinatario.ToString(),
                ["horas_validez"] = solicitud.HorasValidez,
                ["creado_por"] = usuarioIdCreador,
                ["fecha_creacion"] = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(solicitud.Metadatos))
            {
                try
                {
                    var extra = JsonSerializer.Deserialize<Dictionary<string, object>>(solicitud.Metadatos);
                    if (extra != null)
                        foreach (var item in extra) metadata[item.Key] = item.Value;
                }
                catch { }
            }

            var enlace = new enlaces_compartido
            {
                codigo = CodigoHelper.GenerarCodigo("TMP", 12),
                codigo_enlace = token,
                id_tipo_enlace = idTipoEnlace,
                id_usuario_creador = usuarioIdCreador,
                id_rol_asignado = idRolAsignado,
                id_torneo = solicitud.TipoEntidad == TipoEntidad.TORNEO ? solicitud.IdEntidad : null,
                id_equipo = solicitud.TipoEntidad == TipoEntidad.EQUIPO ? solicitud.IdEntidad : null,
                fecha_creacion = DateTime.UtcNow,
                fecha_expiracion = fechaExpiracion,
                max_usos = maxUsos,
                usos_actuales = 0,
                estado = "ACTIVO",
                metadata = JsonSerializer.Serialize(metadata),
                activo = true
            };

            _contexto.enlaces_compartidos.Add(enlace);
            await _contexto.SaveChangesAsync();

            var urls = ObtenerUrls(token);
            var entidadNombre = await ObtenerNombreEntidadAsync(solicitud.TipoEntidad, solicitud.IdEntidad);

            return new EnlaceTemporalResponse
            {
                Id = enlace.id,
                Token = token,
                EnlaceUnico = token,
                InviteUrl = urls.InviteUrl,
                DeepLink = urls.DeepLink,
                FechaExpiracion = fechaExpiracion,
                TipoEntidad = solicitud.TipoEntidad,
                IdEntidad = solicitud.IdEntidad,
                EntidadNombre = entidadNombre,
                Destinatario = solicitud.Destinatario,
                IdUsuarioDestino = solicitud.IdUsuarioDestino,
                EsActivo = true
            };
        }

        public async Task<EnlaceTemporalResponse> CrearEnlaceArbitroPartidoAsync(
            int usuarioIdCreador,
            int idPartido,
            int? idUsuarioDestino = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var partido = await _contexto.partidos
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .FirstOrDefaultAsync(p => p.id == idPartido && p.activo == true);

            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
            if (partido.estado == "FINALIZADO" || partido.estado == "CANCELADO")
                throw new InvalidOperationException("No se pueden crear enlaces para partidos finalizados o cancelados");

            var horasHastaPartido = (partido.fecha_hora - DateTime.UtcNow).TotalHours;
            var horasValidez = Math.Max(48, horasHastaPartido + 4);

            var metadatos = new
            {
                permite_editar_eventos = true,
                permite_firmar_acta = true,
                partido_info = $"{partido.id_equipo_localNavigation?.nombre} vs {partido.id_equipo_visitanteNavigation?.nombre}"
            };

            var solicitud = new EnlaceTemporalRequest
            {
                IdEntidad = idPartido,
                TipoEntidad = TipoEntidad.PARTIDO,
                Destinatario = TipoUsuario.ARBITRO,
                IdUsuarioDestino = idUsuarioDestino,
                HorasValidez = (int)Math.Ceiling(horasValidez),
                Metadatos = JsonSerializer.Serialize(metadatos)
            };

            return await CrearEnlaceTemporalAsync(usuarioIdCreador, solicitud, ipAddress, userAgent);
        }

        public async Task<EnlaceTemporalResponse> CrearEnlaceCapitanAlineacionAsync(
            int usuarioIdCreador,
            int idPartido,
            int idEquipo,
            int? idUsuarioDestino = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var partido = await _contexto.partidos.FindAsync(idPartido);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");

            var equipo = await _contexto.equipos.FindAsync(idEquipo);
            if (equipo == null) throw new KeyNotFoundException("Equipo no encontrado");

            var horasHastaPartido = (partido.fecha_hora - DateTime.UtcNow).TotalHours;
            var horasValidez = Math.Max(1, horasHastaPartido - 1);

            if (horasValidez <= 0)
                throw new InvalidOperationException("Ya pasó la fecha límite para enviar la alineación");

            var metadatos = new
            {
                permite_registrar_alineacion = true,
                id_equipo = idEquipo,
                nombre_equipo = equipo.nombre
            };

            var solicitud = new EnlaceTemporalRequest
            {
                IdEntidad = idPartido,
                TipoEntidad = TipoEntidad.PARTIDO,
                Destinatario = TipoUsuario.CAPITAN,
                IdUsuarioDestino = idUsuarioDestino,
                HorasValidez = (int)Math.Ceiling(horasValidez),
                Metadatos = JsonSerializer.Serialize(metadatos)
            };

            return await CrearEnlaceTemporalAsync(usuarioIdCreador, solicitud, ipAddress, userAgent);
        }

        public async Task<EnlaceTemporalResponse> CrearEnlaceJugadorAsistenciaAsync(
            int usuarioIdCreador,
            int idPartido,
            int idJugador,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var partido = await _contexto.partidos.FindAsync(idPartido);
            if (partido == null) throw new KeyNotFoundException("Partido no encontrado");

            var jugador = await _contexto.usuarios.FindAsync(idJugador);
            if (jugador == null) throw new KeyNotFoundException("Jugador no encontrado");

            var horasHastaPartido = (partido.fecha_hora - DateTime.UtcNow).TotalHours;
            var horasValidez = Math.Max(1, horasHastaPartido - 24);

            if (horasValidez <= 0)
                throw new InvalidOperationException("Ya pasó la fecha límite para confirmar asistencia");

            var metadatos = new
            {
                permite_confirmar_asistencia = true,
                id_jugador = idJugador
            };

            var solicitud = new EnlaceTemporalRequest
            {
                IdEntidad = idPartido,
                TipoEntidad = TipoEntidad.PARTIDO,
                Destinatario = TipoUsuario.JUGADOR,
                IdUsuarioDestino = idJugador,
                HorasValidez = (int)Math.Ceiling(horasValidez),
                Metadatos = JsonSerializer.Serialize(metadatos)
            };

            return await CrearEnlaceTemporalAsync(usuarioIdCreador, solicitud, ipAddress, userAgent);
        }

        public async Task<EnlaceTemporalResponse> CrearEnlaceInvitacionEquipoAsync(
            int usuarioIdCreador,
            int idEquipo,
            int idUsuarioDestino,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var equipo = await _contexto.equipos.FindAsync(idEquipo);
            if (equipo == null) throw new KeyNotFoundException("Equipo no encontrado");

            var usuarioDestino = await _contexto.usuarios.FindAsync(idUsuarioDestino);
            if (usuarioDestino == null) throw new KeyNotFoundException("Usuario destino no encontrado");

            var metadatos = new
            {
                tipo_invitacion = "EQUIPO",
                nombre_equipo = equipo.nombre,
                permite_unirse = true
            };

            var solicitud = new EnlaceTemporalRequest
            {
                IdEntidad = idEquipo,
                TipoEntidad = TipoEntidad.EQUIPO,
                Destinatario = TipoUsuario.JUGADOR,
                IdUsuarioDestino = idUsuarioDestino,
                HorasValidez = 168,
                Metadatos = JsonSerializer.Serialize(metadatos)
            };

            return await CrearEnlaceTemporalAsync(usuarioIdCreador, solicitud, ipAddress, userAgent);
        }

        #endregion

        #region Uso y Consulta

        public async Task<UsarEnlaceTemporalResponse> UsarEnlaceTemporalAsync(
            string token,
            int usuarioId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var enlace = await _contexto.enlaces_compartidos
                .FirstOrDefaultAsync(e => e.codigo_enlace == token && e.activo == true);

            if (enlace == null)
            {
                await RegistrarUsoEnlace(null, usuarioId, false, "Enlace no encontrado", ipAddress, userAgent);
                return new UsarEnlaceTemporalResponse { Exitoso = false, Mensaje = "Enlace no encontrado" };
            }

            if (enlace.estado != "ACTIVO")
            {
                await RegistrarUsoEnlace(enlace.id, usuarioId, false, $"Enlace {enlace.estado}", ipAddress, userAgent);
                return new UsarEnlaceTemporalResponse { Exitoso = false, Mensaje = $"El enlace está {enlace.estado?.ToLowerInvariant()}" };
            }

            if (enlace.fecha_expiracion.HasValue && enlace.fecha_expiracion.Value < DateTime.UtcNow)
            {
                enlace.estado = "EXPIRADO";
                await _contexto.SaveChangesAsync();
                await RegistrarUsoEnlace(enlace.id, usuarioId, false, "Enlace expirado", ipAddress, userAgent);
                return new UsarEnlaceTemporalResponse { Exitoso = false, Mensaje = "El enlace ha expirado" };
            }

            if (enlace.max_usos.HasValue && (enlace.usos_actuales ?? 0) >= enlace.max_usos.Value)
            {
                enlace.estado = "AGOTADO";
                await _contexto.SaveChangesAsync();
                await RegistrarUsoEnlace(enlace.id, usuarioId, false, "Máximo de usos alcanzado", ipAddress, userAgent);
                return new UsarEnlaceTemporalResponse { Exitoso = false, Mensaje = "El enlace ha alcanzado el número máximo de usos" };
            }

            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(enlace.metadata ?? "{}");
            var tipoEntidadStr = metadata?.GetValueOrDefault("tipo_entidad")?.ToString();
            var tipoEntidad = tipoEntidadStr switch
            {
                "PARTIDO" => TipoEntidad.PARTIDO,
                "TORNEO" => TipoEntidad.TORNEO,
                "EQUIPO" => TipoEntidad.EQUIPO,
                "ACTA_DIGITAL" => TipoEntidad.ACTA_DIGITAL,
                _ => TipoEntidad.PARTIDO
            };

            var idEntidad = 0;
            if (metadata?.TryGetValue("id_entidad", out var idVal) == true && idVal is JsonElement idElem)
                idEntidad = idElem.TryGetInt32(out var idInt) ? idInt : 0;

            enlace.usos_actuales = (enlace.usos_actuales ?? 0) + 1;
            if (enlace.max_usos.HasValue && enlace.usos_actuales >= enlace.max_usos.Value)
                enlace.estado = "AGOTADO";

            await _contexto.SaveChangesAsync();
            await RegistrarUsoEnlace(enlace.id, usuarioId, true, null, ipAddress, userAgent);

            return new UsarEnlaceTemporalResponse
            {
                Exitoso = true,
                TipoEntidad = tipoEntidad,
                IdEntidad = idEntidad,
                Mensaje = "Acceso concedido"
            };
        }

        public async Task<EnlaceTemporalResponse?> ObtenerInfoEnlaceAsync(string token)
        {
            var enlace = await _contexto.enlaces_compartidos
                .FirstOrDefaultAsync(e => e.codigo_enlace == token && e.activo == true);

            if (enlace == null) return null;

            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(enlace.metadata ?? "{}");
            var tipoEntidadStr = metadata?.GetValueOrDefault("tipo_entidad")?.ToString();
            var tipoEntidad = tipoEntidadStr switch
            {
                "PARTIDO" => TipoEntidad.PARTIDO,
                "TORNEO" => TipoEntidad.TORNEO,
                "EQUIPO" => TipoEntidad.EQUIPO,
                _ => TipoEntidad.PARTIDO
            };

            var idEntidad = 0;
            if (metadata?.TryGetValue("id_entidad", out var idVal) == true && idVal is JsonElement idElem)
                idEntidad = idElem.TryGetInt32(out var idInt) ? idInt : 0;

            var destinatarioStr = metadata?.GetValueOrDefault("destinatario")?.ToString();
            var destinatario = destinatarioStr?.ToUpper() switch
            {
                "SUPER_ADMIN" => TipoUsuario.SUPER_ADMIN,
                "ADMIN" => TipoUsuario.ADMIN,
                "SUB_ADMIN" => TipoUsuario.SUB_ADMIN,
                "ARBITRO" => TipoUsuario.ARBITRO,
                "CAPITAN" => TipoUsuario.CAPITAN,
                "JUGADOR" => TipoUsuario.JUGADOR,
                _ => TipoUsuario.JUGADOR
            };

            var entidadNombre = await ObtenerNombreEntidadAsync(tipoEntidad, idEntidad);
            var urls = ObtenerUrls(token);

            return new EnlaceTemporalResponse
            {
                Id = enlace.id,
                Token = enlace.codigo_enlace,
                EnlaceUnico = enlace.codigo_enlace,
                InviteUrl = urls.InviteUrl,
                DeepLink = urls.DeepLink,
                FechaExpiracion = enlace.fecha_expiracion ?? DateTime.UtcNow.AddDays(1),
                TipoEntidad = tipoEntidad,
                IdEntidad = idEntidad,
                EntidadNombre = entidadNombre,
                Destinatario = destinatario,
                EsActivo = enlace.estado == "ACTIVO"
            };
        }

        public async Task<InviteInfoResponse?> ObtenerInfoInvitacionAsync(string token)
        {
            var enlace = await _contexto.enlaces_compartidos
                .Include(e => e.id_tipo_enlaceNavigation)
                .Include(e => e.id_rol_asignadoNavigation)
                .Include(e => e.id_torneoNavigation)
                .Include(e => e.id_equipoNavigation)
                .FirstOrDefaultAsync(e => e.codigo_enlace == token && e.activo == true);

            if (enlace == null)
                return new InviteInfoResponse { EsValido = false, ErrorMensaje = "Enlace no encontrado" };

            if (enlace.estado != "ACTIVO")
                return new InviteInfoResponse { EsValido = false, ErrorMensaje = $"El enlace está {enlace.estado?.ToLowerInvariant()}" };

            if (enlace.fecha_expiracion.HasValue && enlace.fecha_expiracion.Value < DateTime.UtcNow)
                return new InviteInfoResponse { EsValido = false, ErrorMensaje = "El enlace ha expirado" };

            if (enlace.max_usos.HasValue && (enlace.usos_actuales ?? 0) >= enlace.max_usos.Value)
                return new InviteInfoResponse { EsValido = false, ErrorMensaje = "El enlace ha alcanzado el número máximo de usos" };

            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(enlace.metadata ?? "{}");
            var tipoEntidadStr = metadata?.GetValueOrDefault("tipo_entidad")?.ToString();
            var tipoEntidad = tipoEntidadStr switch
            {
                "PARTIDO" => TipoEntidad.PARTIDO,
                "TORNEO" => TipoEntidad.TORNEO,
                "EQUIPO" => TipoEntidad.EQUIPO,
                "ACTA_DIGITAL" => TipoEntidad.ACTA_DIGITAL,
                _ => TipoEntidad.PARTIDO
            };

            var idEntidad = 0;
            if (metadata?.TryGetValue("id_entidad", out var idVal) == true && idVal is JsonElement idElem)
                idEntidad = idElem.TryGetInt32(out var idInt) ? idInt : 0;

            var entidadNombre = await ObtenerNombreEntidadAsync(tipoEntidad, idEntidad);
            var urls = ObtenerUrls(token);

            return new InviteInfoResponse
            {
                Token = token,
                Tipo = tipoEntidad.ToString(),
                Titulo = ObtenerTituloInvitacion(tipoEntidad, entidadNombre),
                Mensaje = ObtenerMensajeInvitacion(tipoEntidad),
                NombreEntidad = entidadNombre,
                DeepLink = urls.DeepLink,
                RequiereAutenticacion = true,
                FechaExpiracion = enlace.fecha_expiracion ?? DateTime.UtcNow.AddDays(1),
                EsValido = true
            };
        }

        public async Task<DeepLinkInfoResponse?> ObtenerInfoDeepLinkAsync(string token)
        {
            var info = await ObtenerInfoEnlaceAsync(token);
            if (info == null) return null;

            var urls = ObtenerUrls(token);

            return new DeepLinkInfoResponse
            {
                Token = token,
                Tipo = info.TipoEntidad.ToString(),
                Titulo = info.TipoEntidad == TipoEntidad.EQUIPO ? $"Invitación al equipo {info.EntidadNombre}" : $"Invitación a {info.EntidadNombre}",
                NombreEntidad = info.EntidadNombre,
                FechaExpiracion = info.FechaExpiracion,
                EsValido = info.EsActivo && info.FechaExpiracion > DateTime.UtcNow,
                DeepLink = urls.DeepLink
            };
        }

        #endregion

        #region Registro Público

        public async Task<RegistroPublicoResponse> RegistrarJugadorDesdeInvitacionAsync(
            RegistroPublicoRequest request,
            string? ipAddress = null,
            string? userAgent = null)
        {
            // Primero validar el token de invitación
            var enlace = await _contexto.enlaces_compartidos
                .FirstOrDefaultAsync(e => e.codigo_enlace == request.TokenEquipo && e.activo == true);

            if (enlace == null)
                throw new KeyNotFoundException("Token de invitación no válido");

            if (enlace.estado != "ACTIVO")
                throw new InvalidOperationException($"La invitación está {enlace.estado?.ToLowerInvariant()}");

            if (enlace.fecha_expiracion.HasValue && enlace.fecha_expiracion.Value < DateTime.UtcNow)
                throw new InvalidOperationException("La invitación ha expirado");

            if (enlace.max_usos.HasValue && (enlace.usos_actuales ?? 0) >= enlace.max_usos.Value)
                throw new InvalidOperationException("La invitación ya ha sido utilizada");

            // Verificar si el email ya existe
            if (await _contexto.usuarios.AnyAsync(u => u.email == request.Email))
                throw new InvalidOperationException("El email ya está registrado");

            // Crear usuario
            var salt = HashHelper.GenerateSalt();
            var passwordHash = HashHelper.HashPassword(request.Password, salt);

            var usuario = new usuario
            {
                codigo = CodigoHelper.GenerarCodigo("USR", 8),
                id_tipo_usuario = 2,
                nombres = request.Nombres,
                apellidos = request.Apellidos,
                email = request.Email,
                telefono = request.Telefono,
                password_hash = passwordHash,
                salt = salt,
                activo = true,
                email_verificado = true,
                fecha_registro = DateTime.UtcNow
            };

            _contexto.usuarios.Add(usuario);
            await _contexto.SaveChangesAsync();

            // Asignar rol JUGADOR
            var rolJugador = await ObtenerIdRolPorCodigoAsync("JUGADOR");
            var usuarioRol = new usuarios_role
            {
                codigo = CodigoHelper.GenerarCodigo("UR", 8),
                id_usuario = usuario.id,
                id_rol = rolJugador,
                fecha_inicio = DateOnly.FromDateTime(DateTime.UtcNow),
                estado = "ACTIVO",
                origen_asignacion = "INVITACION",
                id_enlace_origen = enlace.id,
                activo = true
            };
            _contexto.usuarios_roles.Add(usuarioRol);

            // Obtener el equipo del enlace
            var equipo = await _contexto.equipos.FindAsync(enlace.id_equipo);
            if (equipo != null)
            {
                var jugadorEquipo = new jugadores_equipo
                {
                    codigo = CodigoHelper.GenerarCodigo("JE", 8),
                    id_jugador = usuario.id,
                    id_equipo = equipo.id,
                    fecha_inicio = DateTime.UtcNow,
                    estado = "ACTIVO",
                    activo = true
                };
                _contexto.jugadores_equipos.Add(jugadorEquipo);
            }

            // Marcar enlace como usado
            enlace.usos_actuales = (enlace.usos_actuales ?? 0) + 1;
            if (enlace.max_usos.HasValue && enlace.usos_actuales >= enlace.max_usos.Value)
                enlace.estado = "AGOTADO";

            await _contexto.SaveChangesAsync();
            await RegistrarUsoEnlace(enlace.id, usuario.id, true, null, ipAddress, userAgent);

            return new RegistroPublicoResponse
            {
                Id = usuario.id,
                Email = usuario.email,
                NombreCompleto = $"{usuario.nombres} {usuario.apellidos}",
                IdEquipo = equipo?.id ?? 0,
                NombreEquipo = equipo?.nombre ?? "",
                Mensaje = "Bienvenido a TorneoPro"
            };
        }

        public async Task<UnirseEquipoResponse> UnirseAEquipoAsync(
            string tokenEquipo,
            int usuarioId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var enlace = await _contexto.enlaces_compartidos
                .FirstOrDefaultAsync(e => e.codigo_enlace == tokenEquipo && e.activo == true);

            if (enlace == null)
                throw new KeyNotFoundException("Token de invitación no válido");

            if (enlace.estado != "ACTIVO")
                throw new InvalidOperationException($"La invitación está {enlace.estado?.ToLowerInvariant()}");

            if (enlace.fecha_expiracion.HasValue && enlace.fecha_expiracion.Value < DateTime.UtcNow)
                throw new InvalidOperationException("La invitación ha expirado");

            if (enlace.max_usos.HasValue && (enlace.usos_actuales ?? 0) >= enlace.max_usos.Value)
                throw new InvalidOperationException("La invitación ya ha sido utilizada");

            var usuario = await _contexto.usuarios.FindAsync(usuarioId);
            if (usuario == null)
                throw new KeyNotFoundException("Usuario no encontrado");

            var equipo = await _contexto.equipos.FindAsync(enlace.id_equipo);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            // Verificar si ya es miembro
            var yaMiembro = await _contexto.jugadores_equipos
                .AnyAsync(je => je.id_jugador == usuarioId && je.id_equipo == equipo.id && je.activo == true);

            if (yaMiembro)
                throw new InvalidOperationException("El usuario ya es miembro del equipo");

            var jugadorEquipo = new jugadores_equipo
            {
                codigo = CodigoHelper.GenerarCodigo("JE", 8),
                id_jugador = usuarioId,
                id_equipo = equipo.id,
                fecha_inicio = DateTime.UtcNow,
                estado = "ACTIVO",
                activo = true
            };
            _contexto.jugadores_equipos.Add(jugadorEquipo);

            enlace.usos_actuales = (enlace.usos_actuales ?? 0) + 1;
            if (enlace.max_usos.HasValue && enlace.usos_actuales >= enlace.max_usos.Value)
                enlace.estado = "AGOTADO";

            await _contexto.SaveChangesAsync();
            await RegistrarUsoEnlace(enlace.id, usuarioId, true, null, ipAddress, userAgent);

            return new UnirseEquipoResponse
            {
                Id = usuarioId,
                Email = usuario.email,
                NombreCompleto = $"{usuario.nombres} {usuario.apellidos}",
                IdEquipo = equipo.id,
                NombreEquipo = equipo.nombre ?? ""
            };
        }

        #endregion

        #region Gestión

        public async Task<ResultadoPaginado<EnlaceTemporalResponse>> ObtenerMisEnlacesAsync(
            int usuarioId,
            FiltrarEnlaceTemporalRequest solicitud)
        {
            var idTipoEnlaceTemporal = await ObtenerIdTipoEnlaceAsync("ENLACE_TEMPORAL");

            var query = _contexto.enlaces_compartidos
                .Where(e => e.id_usuario_creador == usuarioId && e.id_tipo_enlace == idTipoEnlaceTemporal && e.activo == true)
                .AsQueryable();

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

            var urls = ObtenerUrls("");
            var items = new List<EnlaceTemporalResponse>();

            foreach (var enlace in enlaces)
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(enlace.metadata ?? "{}");
                var tipoEntidadStr = metadata?.GetValueOrDefault("tipo_entidad")?.ToString();
                var tipoEntidad = tipoEntidadStr switch
                {
                    "PARTIDO" => TipoEntidad.PARTIDO,
                    "TORNEO" => TipoEntidad.TORNEO,
                    "EQUIPO" => TipoEntidad.EQUIPO,
                    _ => TipoEntidad.PARTIDO
                };

                var idEntidad = 0;
                if (metadata?.TryGetValue("id_entidad", out var idVal) == true && idVal is JsonElement idElem)
                    idEntidad = idElem.TryGetInt32(out var idInt) ? idInt : 0;

                var entidadNombre = await ObtenerNombreEntidadAsync(tipoEntidad, idEntidad);
                var inviteUrl = $"{urls.BaseUrl}/invite/{enlace.codigo_enlace}";
                var deepLink = $"{urls.DeepLinkScheme}://{urls.DeepLinkHost}/open?token={enlace.codigo_enlace}";

                items.Add(new EnlaceTemporalResponse
                {
                    Id = enlace.id,
                    Token = enlace.codigo_enlace,
                    EnlaceUnico = enlace.codigo_enlace,
                    InviteUrl = inviteUrl,
                    DeepLink = deepLink,
                    FechaExpiracion = enlace.fecha_expiracion ?? DateTime.UtcNow.AddDays(1),
                    TipoEntidad = tipoEntidad,
                    IdEntidad = idEntidad,
                    EntidadNombre = entidadNombre,
                    EsActivo = enlace.estado == "ACTIVO"
                });
            }

            return ResultadoPaginado<EnlaceTemporalResponse>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
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

        public async Task<EnlaceTemporalResponse> RenovarEnlaceAsync(
            int id,
            int usuarioId,
            int horasExtra,
            string? ipAddress = null)
        {
            var enlace = await _contexto.enlaces_compartidos
                .FirstOrDefaultAsync(e => e.id == id && e.id_usuario_creador == usuarioId);

            if (enlace == null)
                throw new KeyNotFoundException("Enlace no encontrado o no tiene permisos");

            if (enlace.estado != "EXPIRADO" && enlace.estado != "ACTIVO" && enlace.estado != "DESACTIVADO")
                throw new InvalidOperationException($"No se puede renovar un enlace en estado {enlace.estado}");

            if (horasExtra < 1 || horasExtra > 720)
                throw new InvalidOperationException("Las horas extra deben estar entre 1 y 720");

            enlace.estado = "ACTIVO";
            enlace.fecha_expiracion = DateTime.UtcNow.AddHours(horasExtra);
            enlace.fecha_desactivacion = null;
            enlace.motivo_desactivacion = null;

            await _contexto.SaveChangesAsync();

            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(enlace.metadata ?? "{}");
            var tipoEntidadStr = metadata?.GetValueOrDefault("tipo_entidad")?.ToString();
            var tipoEntidad = tipoEntidadStr switch
            {
                "PARTIDO" => TipoEntidad.PARTIDO,
                "TORNEO" => TipoEntidad.TORNEO,
                "EQUIPO" => TipoEntidad.EQUIPO,
                _ => TipoEntidad.PARTIDO
            };

            var idEntidad = 0;
            if (metadata?.TryGetValue("id_entidad", out var idVal) == true && idVal is JsonElement idElem)
                idEntidad = idElem.TryGetInt32(out var idInt) ? idInt : 0;

            var entidadNombre = await ObtenerNombreEntidadAsync(tipoEntidad, idEntidad);
            var urls = ObtenerUrls(enlace.codigo_enlace);

            return new EnlaceTemporalResponse
            {
                Id = enlace.id,
                Token = enlace.codigo_enlace,
                EnlaceUnico = enlace.codigo_enlace,
                InviteUrl = urls.InviteUrl,
                DeepLink = urls.DeepLink,
                FechaExpiracion = enlace.fecha_expiracion ?? DateTime.UtcNow.AddHours(horasExtra),
                TipoEntidad = tipoEntidad,
                IdEntidad = idEntidad,
                EntidadNombre = entidadNombre,
                EsActivo = true
            };
        }

        public async Task<List<UsoEnlaceTemporalResponse>> ObtenerHistorialUsosAsync(int enlaceId, int usuarioId)
        {
            var enlace = await _contexto.enlaces_compartidos
                .FirstOrDefaultAsync(e => e.id == enlaceId && e.id_usuario_creador == usuarioId);

            if (enlace == null)
                throw new KeyNotFoundException("Enlace no encontrado o no tiene permisos");

            var usos = await _contexto.enlaces_historial_usos
                .Include(u => u.id_usuarioNavigation)
                .Where(u => u.id_enlace == enlaceId)
                .OrderByDescending(u => u.fecha_uso)
                .ToListAsync();

            return usos.Select(u => new UsoEnlaceTemporalResponse
            {
                Id = u.id,
                IdUsuario = u.id_usuario,
                Usuario = $"{u.id_usuarioNavigation?.nombres} {u.id_usuarioNavigation?.apellidos}",
                Email = u.id_usuarioNavigation?.email,
                FechaUso = u.fecha_uso ?? DateTime.UtcNow,
                IpAddress = u.ip_address,
                UsoExitoso = u.uso_exitoso ?? true,
                MotivoFallo = u.motivo_fallo
            }).ToList();
        }

        public async Task EnviarEnlacesAutomaticosAsync()
        {
            var manana = DateTime.UtcNow.Date.AddDays(1);
            var partidosManana = await _contexto.partidos
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .Where(p => p.fecha_hora.Date == manana && p.estado == "PROGRAMADO")
                .ToListAsync();

            foreach (var partido in partidosManana)
            {
                if (partido.id_arbitro_principal.HasValue)
                {
                    await CrearEnlaceArbitroPartidoAsync(1, partido.id, partido.id_arbitro_principal);
                }
            }

            _logger.LogInformation("Enlaces automáticos enviados para {Cantidad} partidos", partidosManana.Count);
        }

        #endregion
    }
}