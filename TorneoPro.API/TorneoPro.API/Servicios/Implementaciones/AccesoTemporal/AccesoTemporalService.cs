using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TorneoPro.API.Controllers;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.AccesoTemporal.Request;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces.AccesoTemporal;
using TorneoPro.API.Servicios.Interfaces.Email;

namespace TorneoPro.API.Servicios.Implementaciones.AccesoTemporal
{
    public class AccesoTemporalService : IAccesoTemporalService
    {
        private readonly TorneoProContext _contexto;
        private readonly ILogger<AccesoTemporalService> _logger;
        private readonly IConfiguration _configuracion;
        private readonly IEmailService _emailService;
        private readonly IAccesoTemporalService _accesoTemporalService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccesoTemporalService(
            TorneoProContext contexto,
            ILogger<AccesoTemporalService> logger,
            IConfiguration configuracion,
            IEmailService emailService,
            IAccesoTemporalService accesoTemporalService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contexto = contexto;
            _logger = logger;
            _configuracion = configuracion;
            _emailService = emailService;
            _accesoTemporalService = accesoTemporalService;
            _httpContextAccessor = httpContextAccessor;
        }

        private string ObtenerBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return "http://localhost:5293";
            return $"{request.Scheme}://{request.Host}";
        }


        public async Task<EnlaceTemporalResponse> CrearEnlaceTemporalAsync(int usuarioIdCreador, EnlaceTemporalRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            // Validar permisos del creador
            var esAdmin = await EsAdminAsync(usuarioIdCreador);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para crear enlaces temporales");

            // Validar entidad según tipo
            await ValidarEntidadAsync(solicitud.TipoEntidad, solicitud.IdEntidad);

            // Validar usuario destino si se especificó
            if (solicitud.IdUsuarioDestino.HasValue)
            {
                var usuarioDestino = await _contexto.usuarios.FindAsync(solicitud.IdUsuarioDestino.Value);
                if (usuarioDestino == null)
                    throw new KeyNotFoundException("Usuario destino no encontrado");
            }

            var token = GenerarTokenUnico();
            var fechaExpiracion = DateTime.UtcNow.AddHours(solicitud.HorasValidez);

            var metadata = new Dictionary<string, object>
            {
                ["tipo_entidad"] = solicitud.TipoEntidad.ToString(),
                ["id_entidad"] = solicitud.IdEntidad,
                ["destinatario"] = solicitud.Destinatario.ToString(),
                ["horas_validez"] = solicitud.HorasValidez,
                ["creado_por"] = usuarioIdCreador
            };

            if (!string.IsNullOrEmpty(solicitud.Metadatos))
            {
                try
                {
                    var extraMetadata = JsonSerializer.Deserialize<Dictionary<string, object>>(solicitud.Metadatos);
                    if (extraMetadata != null)
                    {
                        foreach (var item in extraMetadata)
                            metadata[item.Key] = item.Value;
                    }
                }
                catch { }
            }

            var enlace = new enlaces_compartido
            {
                codigo = CodigoHelper.GenerarCodigo("TMP", 12),
                codigo_enlace = token,
                id_tipo_enlace = 10,
                id_usuario_creador = usuarioIdCreador,
                id_rol_asignado = (int)solicitud.Destinatario,
                id_torneo = solicitud.TipoEntidad == TipoEntidad.TORNEO ? solicitud.IdEntidad : null,
                id_equipo = null,
                fecha_creacion = DateTime.UtcNow,
                fecha_expiracion = fechaExpiracion,
                max_usos = 10,
                usos_actuales = 0,
                estado = "ACTIVO",
                metadata = JsonSerializer.Serialize(metadata),
                activo = true
            };

            _contexto.enlaces_compartidos.Add(enlace);
            await _contexto.SaveChangesAsync();

            var baseUrl = _configuracion["AppConfig:AppUrl"] ?? ObtenerBaseUrl();
            var deepLinkScheme = _configuracion["AppConfig:DeepLink:Scheme"] ?? "torneopro";
            var deepLinkHost = _configuracion["AppConfig:DeepLink:Host"] ?? "invite";
            var entidadNombre = await ObtenerNombreEntidadAsync(solicitud.TipoEntidad, solicitud.IdEntidad);

            // URL ÚNICA QUE SE ENVÍA AL USUARIO (web intermedia)
            var inviteUrl = $"{baseUrl}/invite/{token}";
            var deepLink = $"{deepLinkScheme}://{deepLinkHost}/open?token={token}";

            return new EnlaceTemporalResponse
            {
                Id = enlace.id,
                Token = token,
                EnlaceUnico = token,
                InviteUrl = inviteUrl,
                DeepLink = deepLink,
                FechaExpiracion = fechaExpiracion,
                TipoEntidad = solicitud.TipoEntidad,
                IdEntidad = solicitud.IdEntidad,
                EntidadNombre = entidadNombre,
                Destinatario = solicitud.Destinatario,
                IdUsuarioDestino = solicitud.IdUsuarioDestino,
                EsActivo = true,
                InviteInfo = new InviteInfoResponse
                {
                    Token = token,
                    Tipo = solicitud.TipoEntidad.ToString(),
                    Titulo = ObtenerTituloInvitacion(solicitud.TipoEntidad, entidadNombre),
                    Mensaje = ObtenerMensajeInvitacion(solicitud.TipoEntidad),
                    NombreEntidad = entidadNombre,
                    DeepLink = deepLink,
                    RequiereAutenticacion = !(solicitud.Destinatario == TipoUsuario.JUGADOR && solicitud.IdUsuarioDestino.HasValue),
                    FechaExpiracion = fechaExpiracion,
                    EsValido = true
                }
            };
        }

        public async Task<InviteInfoResponse?> ObtenerInfoInvitacionAsync(string token)
        {
            var enlace = await _contexto.enlaces_compartidos
                .FirstOrDefaultAsync(e => e.codigo_enlace == token && e.activo == true);

            if (enlace == null)
            {
                return new InviteInfoResponse
                {
                    Token = token,
                    EsValido = false,
                    ErrorMensaje = "Enlace no encontrado"
                };
            }

            if (enlace.estado != "ACTIVO")
            {
                return new InviteInfoResponse
                {
                    Token = token,
                    EsValido = false,
                    ErrorMensaje = $"El enlace está {enlace.estado.ToLowerInvariant()}"
                };
            }

            if (enlace.fecha_expiracion.HasValue && enlace.fecha_expiracion.Value < DateTime.UtcNow)
            {
                enlace.estado = "EXPIRADO";
                await _contexto.SaveChangesAsync();
                return new InviteInfoResponse
                {
                    Token = token,
                    EsValido = false,
                    ErrorMensaje = "El enlace ha expirado"
                };
            }

            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(enlace.metadata ?? "{}");
            var tipoEntidadStr = metadata?.GetValueOrDefault("tipo_entidad")?.ToString();
            var tipoEntidad = tipoEntidadStr switch
            {
                "PARTIDO" => TipoEntidad.PARTIDO,
                "TORNEO" => TipoEntidad.TORNEO,
                "EQUIPO" => TipoEntidad.EQUIPO,
                _ => TipoEntidad.PARTIDO
            };
            var idEntidad = metadata?.TryGetValue("id_entidad", out var idVal) == true && idVal is JsonElement idElem
    ? idElem.TryGetInt32(out var idInt) ? idInt : 0
    : 0;

            var entidadNombre = await ObtenerNombreEntidadAsync(tipoEntidad, idEntidad);
            var deepLinkScheme = _configuracion["AppConfig:DeepLink:Scheme"] ?? "torneopro";
            var deepLinkHost = _configuracion["AppConfig:DeepLink:Host"] ?? "invite";
            var deepLink = $"{deepLinkScheme}://{deepLinkHost}/open?token={token}";

            return new InviteInfoResponse
            {
                Token = token,
                Tipo = tipoEntidad.ToString(),
                Titulo = ObtenerTituloInvitacion(tipoEntidad, entidadNombre),
                Mensaje = ObtenerMensajeInvitacion(tipoEntidad),
                NombreEntidad = entidadNombre,
                DeepLink = deepLink,
                RequiereAutenticacion = true,
                FechaExpiracion = enlace.fecha_expiracion ?? DateTime.UtcNow.AddDays(1),
                EsValido = true
            };
        }

        public async Task<EnlaceTemporalResponse> CrearEnlaceArbitroPartidoAsync(int usuarioIdCreador, int idPartido, int? idUsuarioDestino = null, string? ipAddress = null, string? userAgent = null)
        {
            var partido = await _contexto.partidos
                .Include(p => p.id_torneoNavigation)
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .FirstOrDefaultAsync(p => p.id == idPartido && p.activo == true);

            if (partido == null)
                throw new KeyNotFoundException("Partido no encontrado");

            if (partido.estado == "FINALIZADO" || partido.estado == "CANCELADO")
                throw new InvalidOperationException("No se pueden crear enlaces para partidos finalizados o cancelados");

            var horasHastaPartido = (partido.fecha_hora - DateTime.UtcNow).TotalHours;
            var horasValidez = Math.Max(48, horasHastaPartido + 4);

            var solicitud = new EnlaceTemporalRequest
            {
                IdEntidad = idPartido,
                TipoEntidad = TipoEntidad.PARTIDO,
                Destinatario = TipoUsuario.ARBITRO,
                IdUsuarioDestino = idUsuarioDestino,
                HorasValidez = (int)Math.Ceiling(horasValidez),
                Metadatos = JsonSerializer.Serialize(new
                {
                    permite_editar_eventos = true,
                    permite_firmar_acta = true,
                    id_arbitro_asignado = idUsuarioDestino,
                    fecha_hora_partido = partido.fecha_hora
                })
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
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var usuarioDestino = await _contexto.usuarios.FindAsync(idUsuarioDestino);
            if (usuarioDestino == null)
                throw new KeyNotFoundException("Usuario destino no encontrado");

            var token = GenerarTokenUnico();
            var fechaExpiracion = DateTime.UtcNow.AddDays(7);

            var metadata = new Dictionary<string, object>
            {
                ["tipo_entidad"] = TipoEntidad.EQUIPO.ToString(),
                ["id_entidad"] = idEquipo,
                ["nombre_equipo"] = equipo.nombre,
                ["destinatario"] = TipoUsuario.JUGADOR.ToString(),
                ["tipo_invitacion"] = "EQUIPO",
                ["permite_unirse"] = true,
                ["creado_por"] = usuarioIdCreador,
                ["id_usuario_destino"] = idUsuarioDestino
            };

            var enlace = new enlaces_compartido
            {
                codigo = CodigoHelper.GenerarCodigo("INV", 12),
                codigo_enlace = token,
                id_tipo_enlace = 5,
                id_usuario_creador = usuarioIdCreador,
                id_rol_asignado = (int)TipoUsuario.JUGADOR,
                id_equipo = idEquipo,
                fecha_creacion = DateTime.UtcNow,
                fecha_expiracion = fechaExpiracion,
                max_usos = 1,
                usos_actuales = 0,
                estado = "ACTIVO",
                metadata = JsonSerializer.Serialize(metadata),
                activo = true
            };

            _contexto.enlaces_compartidos.Add(enlace);
            await _contexto.SaveChangesAsync();

            var baseUrl = _configuracion["AppConfig:AppUrl"] ?? ObtenerBaseUrl();
            var deepLinkScheme = _configuracion["AppConfig:DeepLink:Scheme"] ?? "torneopro";
            var deepLinkHost = _configuracion["AppConfig:DeepLink:Host"] ?? "invite";

            var inviteUrl = $"{baseUrl}/invite/{token}";
            var deepLink = $"{deepLinkScheme}://{deepLinkHost}/open?token={token}";

            return new EnlaceTemporalResponse
            {
                Id = enlace.id,
                Token = token,
                EnlaceUnico = token,
                InviteUrl = inviteUrl,
                DeepLink = deepLink,
                FechaExpiracion = fechaExpiracion,
                TipoEntidad = TipoEntidad.EQUIPO,
                IdEntidad = idEquipo,
                EntidadNombre = equipo.nombre,
                Destinatario = TipoUsuario.JUGADOR,
                IdUsuarioDestino = idUsuarioDestino,
                EsActivo = true,
                InviteInfo = new InviteInfoResponse
                {
                    Token = token,
                    Tipo = "EQUIPO",
                    Titulo = $"Invitación al equipo {equipo.nombre}",
                    Mensaje = "Has sido invitado a unirte a este equipo",
                    NombreEntidad = equipo.nombre,
                    DeepLink = deepLink,
                    RequiereAutenticacion = true,
                    FechaExpiracion = fechaExpiracion,
                    EsValido = true
                }
            };
        }

        public async Task<EnlaceTemporalResponse> CrearEnlaceCapitanAlineacionAsync(int usuarioIdCreador, int idPartido, int idEquipo, int? idUsuarioDestino = null, string? ipAddress = null, string? userAgent = null)
        {
            var partido = await _contexto.partidos
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .FirstOrDefaultAsync(p => p.id == idPartido && p.activo == true);

            if (partido == null)
                throw new KeyNotFoundException("Partido no encontrado");

            var equipo = await _contexto.equipos.FindAsync(idEquipo);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var horasHastaPartido = (partido.fecha_hora - DateTime.UtcNow).TotalHours;
            var horasValidez = Math.Max(1, horasHastaPartido - 1);

            if (horasValidez <= 0)
                throw new InvalidOperationException("Ya pasó la fecha límite para enviar la alineación");

            var solicitud = new EnlaceTemporalRequest
            {
                IdEntidad = idPartido,
                TipoEntidad = TipoEntidad.PARTIDO,
                Destinatario = TipoUsuario.CAPITAN,
                IdUsuarioDestino = idUsuarioDestino,
                HorasValidez = (int)Math.Ceiling(horasValidez),
                Metadatos = JsonSerializer.Serialize(new
                {
                    permite_registrar_alineacion = true,
                    id_equipo = idEquipo,
                    nombre_equipo = equipo.nombre,
                    fecha_limite = partido.fecha_hora.AddHours(-1)
                })
            };

            return await CrearEnlaceTemporalAsync(usuarioIdCreador, solicitud, ipAddress, userAgent);
        }

        public async Task<EnlaceTemporalResponse> CrearEnlaceJugadorAsistenciaAsync(int usuarioIdCreador, int idPartido, int idJugador, string? ipAddress = null, string? userAgent = null)
        {
            var partido = await _contexto.partidos
                .FirstOrDefaultAsync(p => p.id == idPartido && p.activo == true);

            if (partido == null)
                throw new KeyNotFoundException("Partido no encontrado");

            var jugador = await _contexto.usuarios.FindAsync(idJugador);
            if (jugador == null)
                throw new KeyNotFoundException("Jugador no encontrado");

            var horasHastaPartido = (partido.fecha_hora - DateTime.UtcNow).TotalHours;
            var horasValidez = Math.Max(1, horasHastaPartido - 24);

            if (horasValidez <= 0)
                throw new InvalidOperationException("Ya pasó la fecha límite para confirmar asistencia");

            var solicitud = new EnlaceTemporalRequest
            {
                IdEntidad = idPartido,
                TipoEntidad = TipoEntidad.PARTIDO,
                Destinatario = TipoUsuario.JUGADOR,
                IdUsuarioDestino = idJugador,
                HorasValidez = (int)Math.Ceiling(horasValidez),
                Metadatos = JsonSerializer.Serialize(new
                {
                    permite_confirmar_asistencia = true,
                    id_jugador = idJugador,
                    nombre_jugador = $"{jugador.nombres} {jugador.apellidos}"
                })
            };

            return await CrearEnlaceTemporalAsync(usuarioIdCreador, solicitud, ipAddress, userAgent);
        }

        public async Task<UsarEnlaceTemporalResponse> UsarEnlaceTemporalAsync(string token, int usuarioId, string? ipAddress = null, string? userAgent = null)
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
                return new UsarEnlaceTemporalResponse { Exitoso = false, Mensaje = $"El enlace está {enlace.estado.ToLowerInvariant()}" };
            }

            if (enlace.fecha_expiracion.HasValue && enlace.fecha_expiracion.Value < DateTime.UtcNow)
            {
                enlace.estado = "EXPIRADO";
                await _contexto.SaveChangesAsync();
                await RegistrarUsoEnlace(enlace.id, usuarioId, false, "Enlace expirado", ipAddress, userAgent);
                return new UsarEnlaceTemporalResponse { Exitoso = false, Mensaje = "El enlace ha expirado" };
            }

            if (enlace.max_usos.HasValue && enlace.usos_actuales >= enlace.max_usos.Value)
            {
                enlace.estado = "AGOTADO";
                await _contexto.SaveChangesAsync();
                await RegistrarUsoEnlace(enlace.id, usuarioId, false, "Máximo de usos alcanzado", ipAddress, userAgent);
                return new UsarEnlaceTemporalResponse { Exitoso = false, Mensaje = "El enlace ha alcanzado el número máximo de usos" };
            }

            Dictionary<string, object>? metadata = null;
            if (!string.IsNullOrEmpty(enlace.metadata))
            {
                try
                {
                    metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(enlace.metadata);
                }
                catch { }
            }

            var tipoEntidadStr = metadata?.GetValueOrDefault("tipo_entidad")?.ToString();
            var tipoEntidad = tipoEntidadStr switch
            {
                "PARTIDO" => TipoEntidad.PARTIDO,
                "TORNEO" => TipoEntidad.TORNEO,
                "PLANILLA" => TipoEntidad.PLANILLA,
                "INFORME" => TipoEntidad.INFORME,
                "SANCION" => TipoEntidad.SANCION,
                "CONVOCATORIA" => TipoEntidad.CONVOCATORIA,
                "ACTA_DIGITAL" => TipoEntidad.ACTA_DIGITAL,
                "EQUIPO" => TipoEntidad.EQUIPO,
                _ => TipoEntidad.PARTIDO
            };

            var idEntidad = metadata?.TryGetValue("id_entidad", out var idEntVal) == true && idEntVal is JsonElement idEntElem
    ? idEntElem.TryGetInt32(out var idEntInt) ? idEntInt : 0
    : 0;

            enlace.usos_actuales++;
            await _contexto.SaveChangesAsync();

            await RegistrarUsoEnlace(enlace.id, usuarioId, true, null, ipAddress, userAgent);

            var respuesta = new UsarEnlaceTemporalResponse
            {
                Exitoso = true,
                TipoEntidad = tipoEntidad,
                IdEntidad = idEntidad,
                Mensaje = "Acceso concedido"
            };

            switch (tipoEntidad)
            {
                case TipoEntidad.PARTIDO:
                    respuesta.PartidoData = await ConstruirAccesoPartidoAsync(idEntidad, metadata);
                    break;
                case TipoEntidad.TORNEO:
                    respuesta.TorneoData = await ConstruirAccesoTorneoAsync(idEntidad, metadata);
                    break;
                case TipoEntidad.ACTA_DIGITAL:
                    respuesta.ActaData = await ConstruirAccesoActaAsync(idEntidad, metadata);
                    break;
                case TipoEntidad.EQUIPO:
                    respuesta.EquipoData = await ConstruirAccesoEquipoAsync(idEntidad, metadata);
                    break;
            }

            respuesta.DatosAcceso = metadata;
            return respuesta;
        }

        public async Task<EnlaceTemporalResponse?> ObtenerInfoEnlaceAsync(string token)
        {
            var enlace = await _contexto.enlaces_compartidos
                .Include(e => e.id_usuario_creadorNavigation)
                .FirstOrDefaultAsync(e => e.codigo_enlace == token && e.activo == true);

            if (enlace == null)
                return null;

            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(enlace.metadata ?? "{}");
            var tipoEntidadStr = metadata?.GetValueOrDefault("tipo_entidad")?.ToString();
            var tipoEntidad = tipoEntidadStr switch
            {
                "PARTIDO" => TipoEntidad.PARTIDO,
                "TORNEO" => TipoEntidad.TORNEO,
                "EQUIPO" => TipoEntidad.EQUIPO,
                _ => TipoEntidad.PARTIDO
            };
            var idEntidad = metadata?.TryGetValue("id_entidad", out var idEntVal) == true && idEntVal is JsonElement idEntElem
    ? idEntElem.TryGetInt32(out var idEntInt) ? idEntInt : 0
    : 0;
            var destinatarioStr = metadata?.GetValueOrDefault("destinatario")?.ToString();
            var destinatario = destinatarioStr?.ToUpper() switch
            {
                "SUPER_ADMIN" => TipoUsuario.SUPER_ADMIN,
                "ADMIN" => TipoUsuario.ADMIN,
                "SUB_ADMIN" => TipoUsuario.SUB_ADMIN,
                "ARBITRO" => TipoUsuario.ARBITRO,
                "CAPITAN" => TipoUsuario.CAPITAN,
                "JUGADOR" => TipoUsuario.JUGADOR,
                _ => throw new Exception($"Rol no válido: {destinatarioStr}")
            };
            var entidadNombre = await ObtenerNombreEntidadAsync(tipoEntidad, idEntidad);
            var baseUrl = _configuracion["AppConfig:AppUrl"] ?? ObtenerBaseUrl();
            var deepLinkScheme = _configuracion["AppConfig:DeepLink:Scheme"] ?? "torneopro";
            var deepLinkHost = _configuracion["AppConfig:DeepLink:Host"] ?? "invite";

            var inviteUrl = $"{baseUrl}/invite/{token}";
            var deepLink = $"{deepLinkScheme}://{deepLinkHost}/open?token={token}";

            return new EnlaceTemporalResponse
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
                Destinatario = destinatario,
                EsActivo = enlace.estado == "ACTIVO"
            };
        }

        public async Task<ResultadoPaginado<EnlaceTemporalResponse>> ObtenerMisEnlacesAsync(int usuarioId, FiltrarEnlaceTemporalRequest solicitud)
        {
            var query = _contexto.enlaces_compartidos
                .Where(e => e.id_usuario_creador == usuarioId && e.id_tipo_enlace == 10 && e.activo == true)
                .AsQueryable();

            if (solicitud.TipoEntidad.HasValue)
                query = query.Where(e => e.metadata.Contains(solicitud.TipoEntidad.Value.ToString()));

            if (solicitud.IdEntidad.HasValue)
                query = query.Where(e => e.metadata.Contains($"\"id_entidad\":{solicitud.IdEntidad.Value}"));

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

            var baseUrl = _configuracion["AppConfig:AppUrl"] ?? ObtenerBaseUrl();
            var deepLinkScheme = _configuracion["AppConfig:DeepLink:Scheme"] ?? "torneopro";
            var deepLinkHost = _configuracion["AppConfig:DeepLink:Host"] ?? "invite";
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
                var idEntidad = metadata?.TryGetValue("id_entidad", out var idEntVal) == true && idEntVal is JsonElement idEntElem
     ? idEntElem.TryGetInt32(out var idEntInt) ? idEntInt : 0
     : 0;

                var entidadNombre = await ObtenerNombreEntidadAsync(tipoEntidad, idEntidad);
                var inviteUrl = $"{baseUrl}/invite/{enlace.codigo_enlace}";
                var deepLink = $"{deepLinkScheme}://{deepLinkHost}/open?token={enlace.codigo_enlace}";

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

        public async Task<EnlaceTemporalResponse> RenovarEnlaceAsync(int id, int usuarioId, int horasExtra, string? ipAddress = null, string? userAgent = null)
        {
            var enlace = await _contexto.enlaces_compartidos
                .FirstOrDefaultAsync(e => e.id == id && e.id_usuario_creador == usuarioId);

            if (enlace == null)
                throw new KeyNotFoundException("Enlace no encontrado o no tiene permisos");

            if (enlace.estado != "EXPIRADO" && enlace.estado != "ACTIVO")
                throw new InvalidOperationException($"No se puede renovar un enlace en estado {enlace.estado}");

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
            var idEntidad = metadata?.TryGetValue("id_entidad", out var idEntVal) == true && idEntVal is JsonElement idEntElem
      ? idEntElem.TryGetInt32(out var idEntInt) ? idEntInt : 0
      : 0;

            var entidadNombre = await ObtenerNombreEntidadAsync(tipoEntidad, idEntidad);
            var baseUrl = _configuracion["AppConfig:AppUrl"] ?? ObtenerBaseUrl();
            var deepLinkScheme = _configuracion["AppConfig:DeepLink:Scheme"] ?? "torneopro";
            var deepLinkHost = _configuracion["AppConfig:DeepLink:Host"] ?? "invite";

            var inviteUrl = $"{baseUrl}/invite/{enlace.codigo_enlace}";
            var deepLink = $"{deepLinkScheme}://{deepLinkHost}/open?token={enlace.codigo_enlace}";

            return new EnlaceTemporalResponse
            {
                Id = enlace.id,
                Token = enlace.codigo_enlace,
                EnlaceUnico = enlace.codigo_enlace,
                InviteUrl = inviteUrl,
                DeepLink = deepLink,
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
                .Include(p => p.id_torneoNavigation)
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .Where(p => p.fecha_hora.Date == manana && p.estado == "PROGRAMADO")
                .ToListAsync();

            foreach (var partido in partidosManana)
            {
                if (partido.id_arbitro_principal.HasValue)
                {
                    var enlace = await CrearEnlaceArbitroPartidoAsync(1, partido.id, partido.id_arbitro_principal);

                    var arbitro = await _contexto.usuarios.FindAsync(partido.id_arbitro_principal);
                    if (arbitro != null && !string.IsNullOrEmpty(arbitro.email))
                    {
                        await _emailService.EnviarNotificacionEmailAsync(
                            arbitro.email,
                            "📋 Enlace para el acta del partido - TorneoPro",
                            $@"
                            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                <h1>Estimado árbitro</h1>
                                <p>Se le ha asignado el siguiente partido:</p>
                                <p><strong>{partido.id_equipo_localNavigation?.nombre} vs {partido.id_equipo_visitanteNavigation?.nombre}</strong></p>
                                <p>Fecha: {partido.fecha_hora:dd/MM/yyyy HH:mm}</p>
                                <p>Cancha: {partido.id_canchaNavigation?.nombre ?? "Por definir"}</p>
                                <br/>
                                <p>📱 Abre la app:</p>
                                <a href='{enlace.DeepLink}' style='background:#2563EB; color:white; padding:10px 20px; text-decoration:none; border-radius:5px; display:inline-block;'>
                                    Abrir en TorneoPro
                                </a>
                                <br/>
                                <p>🔗 O usa este enlace web: <a href='{enlace.InviteUrl}'>{enlace.InviteUrl}</a></p>
                                <p>Este enlace expirará 2 horas después del partido.</p>
                            </div>
                            ",
                            arbitro.nombres);
                    }
                }
            }

            _logger.LogInformation("Enlaces automáticos enviados para {Cantidad} partidos", partidosManana.Count);
        }

        public async Task<DeepLinkInfoResponse?> ObtenerInfoDeepLinkAsync(string token)
        {
            var enlace = await _accesoTemporalService.ObtenerInfoEnlaceAsync(token);

            if (enlace == null)
            {
                return new DeepLinkInfoResponse
                {
                    Token = token,
                    EsValido = false,
                    ErrorMensaje = "Enlace no encontrado"
                };
            }

            var deepLinkScheme = _configuracion["AppConfig:DeepLink:Scheme"] ?? "torneopro";
            var deepLinkHost = _configuracion["AppConfig:DeepLink:Host"] ?? "invite";

            return new DeepLinkInfoResponse
            {
                Token = token,
                Tipo = enlace.TipoEntidad.ToString(),
                Titulo = enlace.TipoEntidad == TipoEntidad.EQUIPO ? $"Invitación al equipo {enlace.EntidadNombre}" : $"Invitación a {enlace.EntidadNombre}",
                NombreEntidad = enlace.EntidadNombre,
                FechaExpiracion = enlace.FechaExpiracion,
                EsValido = enlace.EsActivo && enlace.FechaExpiracion > DateTime.UtcNow,
                DeepLink = $"{deepLinkScheme}://{deepLinkHost}/open?token={token}"
            };
        }

        #region Métodos Privados

        private string ObtenerTituloInvitacion(TipoEntidad tipoEntidad, string nombreEntidad)
        {
            return tipoEntidad switch
            {
                TipoEntidad.PARTIDO => $"Partido: {nombreEntidad}",
                TipoEntidad.TORNEO => $"Torneo: {nombreEntidad}",
                TipoEntidad.EQUIPO => $"Invitación al equipo {nombreEntidad}",
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
                _ => "Tienes una invitación pendiente"
            };
        }

        private async Task<bool> EsAdminAsync(int usuarioId)
        {
            return await _contexto.usuarios_roles
                .AnyAsync(ur => ur.id_usuario == usuarioId &&
                               (ur.id_rol == 1 || ur.id_rol == 2) &&
                               ur.estado == "ACTIVO");
        }

        private string GenerarTokenUnico()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "")
                .ToLowerInvariant();
        }

        private async Task ValidarEntidadAsync(TipoEntidad tipoEntidad, int idEntidad)
        {
            switch (tipoEntidad)
            {
                case TipoEntidad.PARTIDO:
                    var partido = await _contexto.partidos.FindAsync(idEntidad);
                    if (partido == null) throw new KeyNotFoundException("Partido no encontrado");
                    break;
                case TipoEntidad.TORNEO:
                    var torneo = await _contexto.torneos.FindAsync(idEntidad);
                    if (torneo == null) throw new KeyNotFoundException("Torneo no encontrado");
                    break;
                case TipoEntidad.ACTA_DIGITAL:
                    var acta = await _contexto.actas_partidos.FindAsync(idEntidad);
                    if (acta == null) throw new KeyNotFoundException("Acta no encontrada");
                    break;
                case TipoEntidad.EQUIPO:
                    var equipo = await _contexto.equipos.FindAsync(idEntidad);
                    if (equipo == null) throw new KeyNotFoundException("Equipo no encontrado");
                    break;
            }
        }

        private async Task<string> ObtenerNombreEntidadAsync(TipoEntidad tipoEntidad, int idEntidad)
        {
            switch (tipoEntidad)
            {
                case TipoEntidad.PARTIDO:
                    var partido = await _contexto.partidos
                        .Include(p => p.id_equipo_localNavigation)
                        .Include(p => p.id_equipo_visitanteNavigation)
                        .FirstOrDefaultAsync(p => p.id == idEntidad);
                    return partido != null
                        ? $"{partido.id_equipo_localNavigation?.nombre} vs {partido.id_equipo_visitanteNavigation?.nombre}"
                        : "Partido no encontrado";
                case TipoEntidad.TORNEO:
                    var torneo = await _contexto.torneos.FindAsync(idEntidad);
                    return torneo?.nombre ?? "Torneo no encontrado";
                case TipoEntidad.ACTA_DIGITAL:
                    var acta = await _contexto.actas_partidos
                        .Include(a => a.id_partidoNavigation)
                        .ThenInclude(p => p.id_equipo_localNavigation)
                        .Include(a => a.id_partidoNavigation)
                        .ThenInclude(p => p.id_equipo_visitanteNavigation)
                        .FirstOrDefaultAsync(a => a.id == idEntidad);
                    if (acta?.id_partidoNavigation != null)
                    {
                        return $"Acta - {acta.id_partidoNavigation.id_equipo_localNavigation?.nombre} vs {acta.id_partidoNavigation.id_equipo_visitanteNavigation?.nombre}";
                    }
                    return "Acta no encontrada";
                case TipoEntidad.EQUIPO:
                    var equipo = await _contexto.equipos.FindAsync(idEntidad);
                    return equipo?.nombre ?? "Equipo no encontrado";
                default:
                    return "Entidad no especificada";
            }
        }

        private async Task<AccesoEquipoData?> ConstruirAccesoEquipoAsync(int idEquipo, Dictionary<string, object>? metadata)
        {
            var equipo = await _contexto.equipos.FirstOrDefaultAsync(e => e.id == idEquipo && e.activo == true);
            if (equipo == null) return null;

            var idJugadorInvitado = metadata?.TryGetValue("id_usuario_destino", out var jugVal) == true && jugVal is JsonElement jugElem
    ? jugElem.TryGetInt32(out var jugInt) ? jugInt : 0
    : 0;

            return new AccesoEquipoData
            {
                IdEquipo = equipo.id,
                NombreEquipo = equipo.nombre,
                EscudoUrl = equipo.escudo_url,
                PuedeUnirse = metadata?.GetValueOrDefault("permite_unirse") as bool? ?? true,
                IdJugadorInvitado = idJugadorInvitado,
                MensajeBienvenida = $"Bienvenido al equipo {equipo.nombre}"
            };
        }

        private async Task<AccesoPartidoData?> ConstruirAccesoPartidoAsync(int idPartido, Dictionary<string, object>? metadata)
        {
            var partido = await _contexto.partidos
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .Include(p => p.id_canchaNavigation)
                .FirstOrDefaultAsync(p => p.id == idPartido);

            if (partido == null) return null;

            return new AccesoPartidoData
            {
                IdPartido = partido.id,
                Local = partido.id_equipo_localNavigation?.nombre ?? "",
                Visitante = partido.id_equipo_visitanteNavigation?.nombre ?? "",
                FechaHora = partido.fecha_hora,
                Cancha = partido.id_canchaNavigation?.nombre ?? "Por definir",
                PuedeEditarEventos = metadata?.GetValueOrDefault("permite_editar_eventos") as bool? ?? false,
                PuedeFirmarActa = metadata?.GetValueOrDefault("permite_firmar_acta") as bool? ?? false,
                PuedeVerAlineaciones = metadata?.GetValueOrDefault("permite_ver_alineaciones") as bool? ?? true,
                PuedeRegistrarResultado = metadata?.GetValueOrDefault("permite_registrar_resultado") as bool? ?? false,
                PuedeConfirmarAsistencia = metadata?.GetValueOrDefault("permite_confirmar_asistencia") as bool? ?? false
            };
        }

        private async Task<AccesoTorneoData?> ConstruirAccesoTorneoAsync(int idTorneo, Dictionary<string, object>? metadata)
        {
            var torneo = await _contexto.torneos.FindAsync(idTorneo);
            if (torneo == null) return null;

            return new AccesoTorneoData
            {
                IdTorneo = torneo.id,
                Nombre = torneo.nombre,
                Estado = torneo.estado ?? "PLANIFICACION",
                PuedeVerTabla = true,
                PuedeVerCalendario = true,
                PuedeVerEstadisticas = true,
                PuedeInscribirEquipo = metadata?.GetValueOrDefault("permite_inscribir_equipo") as bool? ?? false
            };
        }

        private async Task<AccesoActaData?> ConstruirAccesoActaAsync(int idActa, Dictionary<string, object>? metadata)
        {
            var acta = await _contexto.actas_partidos.FindAsync(idActa);
            if (acta == null) return null;

            var baseUrl = _configuracion["AppConfig:AppUrl"] ?? ObtenerBaseUrl();

            return new AccesoActaData
            {
                IdActa = acta.id,
                IdPartido = acta.id_partido,
                ActaUrl = $"{baseUrl}/api/actas/{acta.id}",
                PuedeFirmar = metadata?.GetValueOrDefault("permite_firmar_acta") as bool? ?? false,
                PuedeDescargarPdf = true,
                FechaLimiteFirma = acta.fecha_firma.HasValue ? acta.fecha_firma.Value.AddDays(1) : DateTime.UtcNow.AddDays(1)
            };
        }

        private async Task RegistrarUsoEnlace(int? enlaceId, int usuarioId, bool exitoso, string? motivo, string? ipAddress, string? userAgent)
        {
            // Obtener TODOS los roles activos del usuario
            var rolesUsuario = await _contexto.usuarios_roles
                .Where(ur => ur.id_usuario == usuarioId && ur.estado == "ACTIVO")
                .Select(ur => ur.id_rol)
                .ToListAsync();

            // Obtener el rol JUGADOR como fallback (dinámicamente por código)
            var rolJugador = await _contexto.tipos_rols
                .Where(r => r.codigo == "JUGADOR")
                .Select(r => r.id)
                .FirstOrDefaultAsync();

            // Si no existe rol JUGADOR, obtener el rol con el nivel_jerarquia más bajo
            if (rolJugador == 0)
            {
                rolJugador = await _contexto.tipos_rols
                    .OrderByDescending(r => r.nivel_jerarquia)
                    .Select(r => r.id)
                    .FirstOrDefaultAsync();
            }

            // Determinar el rol para id_rol_anterior (puede ser NULL o el primer rol)
            int? rolAnterior = rolesUsuario.Any() ? rolesUsuario.First() : null;

            // Determinar el rol para id_rol_nuevo (NUNCA puede ser NULL)
            int rolNuevo;

            if (rolesUsuario.Any())
            {
                // Obtenemos el rol de MENOR jerarquía (el menos privilegiado) para el historial
                // Esto evita privilegios elevados en el log
                var rolesConJerarquia = await _contexto.tipos_rols
                    .Where(r => rolesUsuario.Contains(r.id))
                    .OrderBy(r => r.nivel_jerarquia)  // Menor número = mayor jerarquía? Ajusta según tu lógica
                    .Select(r => r.id)
                    .ToListAsync();

                // Tomar el rol de mayor jerarquía o el primero según tu necesidad
                rolNuevo = rolesConJerarquia.FirstOrDefault();

                if (rolNuevo == 0)
                    rolNuevo = rolesUsuario.First();
            }
            else
            {
                // Usuario sin roles - usar el rol de menor jerarquía (JUGADOR por defecto)
                rolNuevo = rolJugador;
                _logger.LogWarning("Usuario {UsuarioId} sin roles asignados, se usa rol {RolId} por defecto", usuarioId, rolNuevo);
            }

            // Verificar que el rol existe en la tabla tipos_rol
            var rolExiste = await _contexto.tipos_rols.AnyAsync(r => r.id == rolNuevo);
            if (!rolExiste)
            {
                _logger.LogError("Rol {RolId} no existe en tipos_rol. Usuario: {UsuarioId}", rolNuevo, usuarioId);

                // Último recurso: obtener el primer rol disponible
                rolNuevo = await _contexto.tipos_rols
                    .Select(r => r.id)
                    .FirstOrDefaultAsync();

                if (rolNuevo == 0)
                {
                    throw new InvalidOperationException("No hay roles disponibles en la base de datos");
                }
            }

            var uso = new enlaces_historial_uso
            {
                codigo = CodigoHelper.GenerarCodigo("USE", 10),
                id_enlace = enlaceId ?? 0,
                id_usuario = usuarioId,
                fecha_uso = DateTime.UtcNow,
                ip_address = ipAddress,
                user_agent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
                uso_exitoso = exitoso,
                motivo_fallo = motivo,
                id_rol_anterior = rolAnterior,
                id_rol_nuevo = rolNuevo
            };

            await _contexto.enlaces_historial_usos.AddAsync(uso);
            await _contexto.SaveChangesAsync();

            // Log para debugging: mostrar todos los roles del usuario
            if (rolesUsuario.Count > 1)
            {
                _logger.LogDebug("Usuario {UsuarioId} tiene múltiples roles: {Roles}. Se usó rol {RolUsado} para el historial",
                    usuarioId, string.Join(", ", rolesUsuario), rolNuevo);
            }
        }

        #endregion
    }
}