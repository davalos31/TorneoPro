using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.AccesoTemporal.Request;
using TorneoPro.API.DTOs.Jugadores.Request;
using TorneoPro.API.DTOs.Jugadores.Response;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces.AccesoTemporal;
using TorneoPro.API.Servicios.Interfaces.Email;
using TorneoPro.API.Servicios.Interfaces.Jugador;
using TorneoPro.API.Servicios.Interfaces.Notificacion;


namespace TorneoPro.API.Servicios.Implementaciones.Jugador
{
    public class JugadorService : IJugadorService
    {
        private readonly TorneoProContext _contexto;
        private readonly ILogger<JugadorService> _logger;
        private readonly IConfiguration _configuracion;
        private readonly INotificacionService _notificacionService;
        private readonly IEmailService _emailService;
        private readonly IAccesoTemporalService _accesoTemporalService;
        private readonly QRHelper _qrHelper;
        private readonly ArchivosHelper _archivosHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JugadorService(
            TorneoProContext contexto,
            ILogger<JugadorService> logger,
            IConfiguration configuracion,
            INotificacionService notificacionService,
            IEmailService emailService,
            IAccesoTemporalService accesoTemporalService,
            QRHelper qrHelper,
            ArchivosHelper archivosHelper,
            IHttpContextAccessor httpContextAccessor)
        {
            _contexto = contexto;
            _logger = logger;
            _configuracion = configuracion;
            _notificacionService = notificacionService;
            _emailService = emailService;
            _accesoTemporalService = accesoTemporalService;
            _qrHelper = qrHelper;
            _archivosHelper = archivosHelper;
            _httpContextAccessor = httpContextAccessor;
        }

        #region ========== MÉTODOS PRIVADOS ==========

        private string ObtenerBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            return request == null ? "http://localhost:5293" : $"{request.Scheme}://{request.Host}";
        }

        private string GenerarTokenUnico()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "")
                .ToLowerInvariant()[..32];
        }

        private async Task<bool> EsAdminAsync(int usuarioId)
        {
            return await _contexto.usuarios_roles
                .AnyAsync(ur => ur.id_usuario == usuarioId && (ur.id_rol == 1 || ur.id_rol == 2) && ur.estado == "ACTIVO");
        }

        private async Task<JugadorResponse> MapearJugadorResponseAsync(usuario usuario)
        {
            var metadata = string.IsNullOrEmpty(usuario.metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(usuario.metadata ?? "{}") ?? new();

            return new JugadorResponse
            {
                Id = usuario.id,
                Codigo = usuario.codigo,
                NombreCompleto = $"{usuario.nombres} {usuario.apellidos}",
                Email = usuario.email,
                Telefono = usuario.telefono,
                FotoPerfil = usuario.foto_perfil_url,
                PosicionPreferida = metadata.TryGetValue("posicion_preferida", out var posVal) && posVal is JsonElement posElem ? posElem.GetString() : null,
                NumeroCamisetaPreferido = metadata.TryGetValue("numero_preferido", out var numVal) && numVal is JsonElement numElem ? (numElem.TryGetInt32(out var num) ? num : null) : null,
                PesoKg = usuario.peso_kg,
                AlturaCm = usuario.altura_cm,
                Activo = usuario.activo ?? false,
                EmailVerificado = usuario.email_verificado ?? false,
                FechaRegistro = usuario.fecha_registro ?? DateTime.UtcNow,
                Equipos = await ObtenerEquiposAsync(usuario.id),
                Estadisticas = await ObtenerEstadisticasAsync(usuario.id)
            };
        }

        #endregion

        #region ========== CRUD PRINCIPAL ==========

        public async Task<ResultadoPaginado<JugadorResponse>> ObtenerTodosAsync(FiltrarJugadorRequest solicitud)
        {
            var query = _contexto.usuarios
                .Where(u => u.activo == true && u.id_tipo_usuario == 1)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(solicitud.Buscar))
                query = query.Where(u => u.nombres.Contains(solicitud.Buscar) || u.apellidos.Contains(solicitud.Buscar) || u.email.Contains(solicitud.Buscar) || u.codigo.Contains(solicitud.Buscar));

            if (!string.IsNullOrWhiteSpace(solicitud.Ciudad))
                query = query.Where(u => u.ciudad == solicitud.Ciudad);

            if (!string.IsNullOrWhiteSpace(solicitud.Pais))
                query = query.Where(u => u.pais == solicitud.Pais);

            if (solicitud.SoloActivos == true)
                query = query.Where(u => u.activo == true);

            if (solicitud.IdEquipo.HasValue)
                query = query.Where(u => _contexto.jugadores_equipos.Any(je => je.id_jugador == u.id && je.id_equipo == solicitud.IdEquipo.Value && je.activo == true));

            if (solicitud.SoloDisponibles == true)
                query = query.Where(u => !_contexto.jugadores_suspensiones.Any(js => js.id_jugador == u.id && js.estado == "ACTIVA"));

            if (solicitud.EdadMinima.HasValue || solicitud.EdadMaxima.HasValue)
            {
                var fechaActual = DateTime.UtcNow;
                if (solicitud.EdadMinima.HasValue)
                    query = query.Where(u => u.fecha_nacimiento <= DateOnly.FromDateTime(fechaActual.AddYears(-solicitud.EdadMinima.Value)));
                if (solicitud.EdadMaxima.HasValue)
                    query = query.Where(u => u.fecha_nacimiento >= DateOnly.FromDateTime(fechaActual.AddYears(-solicitud.EdadMaxima.Value - 1)));
            }

            var totalItems = await query.CountAsync();
            query = query.OrderByDescending(u => u.fecha_registro);

            var usuarios = await query.Skip((solicitud.Pagina - 1) * solicitud.TamanoPagina).Take(solicitud.TamanoPagina).ToListAsync();

            var items = new List<JugadorResponse>();
            foreach (var usuario in usuarios)
                items.Add(await MapearJugadorResponseAsync(usuario));

            return ResultadoPaginado<JugadorResponse>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
        }

        public async Task<JugadorResponse?> ObtenerPorIdAsync(int id)
        {
            var usuario = await _contexto.usuarios.FirstOrDefaultAsync(u => u.id == id && u.activo == true && u.id_tipo_usuario == 1);
            return usuario == null ? null : await MapearJugadorResponseAsync(usuario);
        }

        public async Task<JugadorResponse?> ObtenerPorEmailAsync(string email)
        {
            var usuario = await _contexto.usuarios.FirstOrDefaultAsync(u => u.email == email && u.activo == true && u.id_tipo_usuario == 1);
            return usuario == null ? null : await MapearJugadorResponseAsync(usuario);
        }

        public async Task<JugadorResponse> RegistrarJugadorAsync(int usuarioIdAdmin, RegistrarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            if (!await EsAdminAsync(usuarioIdAdmin))
                throw new UnauthorizedAccessException("No tiene permisos para registrar jugadores");

            if (await _contexto.usuarios.AnyAsync(u => u.email == solicitud.Email))
                throw new InvalidOperationException("El email ya está registrado");

            var salt = HashHelper.GenerateSalt();
            var passwordHash = HashHelper.HashPassword(solicitud.Password, salt);

            var usuario = new usuario
            {
                codigo = CodigoHelper.GenerarCodigo("JUG", 8),
                id_tipo_usuario = 1,
                nombres = solicitud.Nombres,
                apellidos = solicitud.Apellidos,
                email = solicitud.Email,
                telefono = solicitud.Telefono,
                id_tipo_documento = solicitud.IdTipoDocumento,
                numero_documento = solicitud.NumeroDocumento,
                peso_kg = solicitud.PesoKg,
                altura_cm = solicitud.AlturaCm,
                fecha_nacimiento = solicitud.FechaNacimiento.HasValue ? DateOnly.FromDateTime(solicitud.FechaNacimiento.Value) : null,
                genero = solicitud.Genero,
                ciudad = solicitud.Ciudad,
                pais = solicitud.Pais,
                password_hash = passwordHash,
                salt = salt,
                activo = true,
                email_verificado = true,
                fecha_registro = DateTime.UtcNow,
                metadata = JsonSerializer.Serialize(new { posicion_preferida = solicitud.PosicionPreferida, numero_preferido = solicitud.NumeroCamisetaPreferido })
            };

            _contexto.usuarios.Add(usuario);
            await _contexto.SaveChangesAsync();

            var rolJugador = await _contexto.tipos_rols.FirstOrDefaultAsync(r => r.codigo == "JUGADOR");
            if (rolJugador != null)
            {
                _contexto.usuarios_roles.Add(new usuarios_role
                {
                    codigo = CodigoHelper.GenerarCodigo("UR", 8),
                    id_usuario = usuario.id,
                    id_rol = rolJugador.id,
                    fecha_inicio = DateOnly.FromDateTime(DateTime.UtcNow),
                    estado = "ACTIVO",
                    origen_asignacion = "MANUAL",
                    activo = true
                });
                await _contexto.SaveChangesAsync();
            }

            await _emailService.EnviarNotificacionEmailAsync(usuario.email, "¡Bienvenido a TorneoPro!", $@"
            <div style='font-family: Arial; background:#f4f6f8; padding:20px;'>
                <div style='max-width:600px; margin:0 auto; background:#fff; border-radius:10px;'>
                    <div style='background:linear-gradient(135deg,#1E3A8A,#2563EB); padding:20px; text-align:center; color:#fff;'>
                        <h1>⚽ TorneoPro</h1>
                    </div>
                    <div style='padding:25px;'>
                        <h2>¡Hola {usuario.nombres}! 👋</h2>
                        <p>Tu cuenta ha sido creada exitosamente como <strong>jugador</strong>.</p>
                        <div style='background:#f1f5f9; padding:15px; border-radius:8px; margin:20px 0;'>
                            <p><strong>📧 Email:</strong> {usuario.email}</p>
                            <p><strong>🔑 Contraseña:</strong> {solicitud.Password}</p>
                        </div>
                        <p>Te recomendamos cambiar tu contraseña después de iniciar sesión.</p>
                    </div>
                    <div style='background:#f9fafb; text-align:center; padding:15px; font-size:12px; color:#888;'>
                        © {DateTime.UtcNow.Year} TorneoPro
                    </div>
                </div>
            </div>", usuario.nombres);

            return await MapearJugadorResponseAsync(usuario);
        }

        public async Task<JugadorResponse> ActualizarJugadorAsync(int id, int usuarioId, ActualizarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var usuario = await _contexto.usuarios.FindAsync(id);
            if (usuario == null) throw new KeyNotFoundException("Jugador no encontrado");

            if (!await EsAdminAsync(usuarioId) && usuarioId != id)
                throw new UnauthorizedAccessException("No tiene permisos para modificar este jugador");

            if (!string.IsNullOrWhiteSpace(solicitud.Nombres)) usuario.nombres = solicitud.Nombres;
            if (!string.IsNullOrWhiteSpace(solicitud.Apellidos)) usuario.apellidos = solicitud.Apellidos;
            if (!string.IsNullOrWhiteSpace(solicitud.Telefono)) usuario.telefono = solicitud.Telefono;
            if (solicitud.PesoKg.HasValue) usuario.peso_kg = solicitud.PesoKg.Value;
            if (solicitud.AlturaCm.HasValue) usuario.altura_cm = solicitud.AlturaCm.Value;
            if (!string.IsNullOrWhiteSpace(solicitud.Biografia)) usuario.biografia = solicitud.Biografia;
            if (!string.IsNullOrWhiteSpace(solicitud.Ciudad)) usuario.ciudad = solicitud.Ciudad;
            if (!string.IsNullOrWhiteSpace(solicitud.Pais)) usuario.pais = solicitud.Pais;
            if (!string.IsNullOrWhiteSpace(solicitud.Genero)) usuario.genero = solicitud.Genero;
            if (solicitud.FechaNacimiento.HasValue) usuario.fecha_nacimiento = DateOnly.FromDateTime(solicitud.FechaNacimiento.Value);
            if (!string.IsNullOrWhiteSpace(solicitud.TelefonoEmergencia)) usuario.telefono_emergencia = solicitud.TelefonoEmergencia;

            var metadata = string.IsNullOrEmpty(usuario.metadata) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(usuario.metadata ?? "{}") ?? new();
            if (!string.IsNullOrWhiteSpace(solicitud.PosicionPreferida)) metadata["posicion_preferida"] = solicitud.PosicionPreferida;
            if (solicitud.NumeroCamisetaPreferido.HasValue) metadata["numero_preferido"] = solicitud.NumeroCamisetaPreferido.Value;
            usuario.metadata = JsonSerializer.Serialize(metadata);
            usuario.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();
            return await MapearJugadorResponseAsync(usuario);
        }

        #endregion

        #region ========== INVITACIONES ==========

        public async Task<InvitarJugadorResponse> InvitarJugadorAsync(int usuarioIdAdmin, InvitarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            if (!await EsAdminAsync(usuarioIdAdmin))
                throw new UnauthorizedAccessException("No tiene permisos para invitar jugadores");

            var equipo = await _contexto.equipos.FindAsync(solicitud.IdEquipo);
            if (equipo == null) throw new KeyNotFoundException("Equipo no encontrado");

            var usuarioExistente = await _contexto.usuarios.FirstOrDefaultAsync(u => u.email == solicitud.Email);

            if (usuarioExistente != null)
            {
                var enlaceTemporal = await _accesoTemporalService.CrearEnlaceInvitacionEquipoAsync(
                    usuarioIdAdmin, solicitud.IdEquipo, usuarioExistente.id, ipAddress, userAgent);

                await _emailService.EnviarNotificacionEmailAsync(solicitud.Email, "Invitación a equipo - TorneoPro",
                    $"<h1>¡Has sido invitado al equipo {equipo.nombre}!</h1><p>Haz clic para unirte: <a href='{enlaceTemporal.InviteUrl}'>Unirse al equipo</a></p>", usuarioExistente.nombres);

                return new InvitarJugadorResponse { Exitoso = true, Mensaje = "Invitación enviada al jugador existente", EnlaceInvitacion = enlaceTemporal.InviteUrl, EsUsuarioExistente = true, IdJugador = usuarioExistente.id };
            }

            var tokenEquipo = GenerarTokenUnico();
            var metadata = new Dictionary<string, object>
            {
                ["tipo_entidad"] = TipoEntidad.EQUIPO.ToString(),
                ["id_entidad"] = solicitud.IdEquipo,
                ["nombre_equipo"] = equipo.nombre,
                ["tipo_invitacion"] = "REGISTRO_PENDIENTE",
                ["permite_unirse"] = true,
                ["creado_por"] = usuarioIdAdmin,
                ["email_destino"] = solicitud.Email
            };

            var enlacePendiente = new enlaces_compartido
            {
                codigo = CodigoHelper.GenerarCodigo("INV", 12),
                codigo_enlace = tokenEquipo,
                id_tipo_enlace = 10,
                id_usuario_creador = usuarioIdAdmin,
                id_rol_asignado = (int)TipoUsuario.JUGADOR,
                id_equipo = solicitud.IdEquipo,
                fecha_creacion = DateTime.UtcNow,
                fecha_expiracion = DateTime.UtcNow.AddDays(7),
                max_usos = 1,
                usos_actuales = 0,
                estado = "ACTIVO",
                metadata = JsonSerializer.Serialize(metadata),
                activo = true
            };

            _contexto.enlaces_compartidos.Add(enlacePendiente);
            await _contexto.SaveChangesAsync();

            var registroWebUrl = $"{ObtenerBaseUrl()}/registro?tokenEquipo={tokenEquipo}";

            await _emailService.EnviarNotificacionEmailAsync(solicitud.Email, "Invitación a TorneoPro - Registro de jugador",
                $"<h1>¡Has sido invitado al equipo {equipo.nombre}!</h1><p>Regístrate para unirte: <a href='{registroWebUrl}'>Registrarse</a></p>", "Nuevo usuario");

            return new InvitarJugadorResponse { Exitoso = true, Mensaje = "Invitación enviada para registro de nuevo jugador", EnlaceInvitacion = registroWebUrl, EsUsuarioExistente = false, TokenEquipo = tokenEquipo };
        }

        public async Task<JugadorResponse> AceptarInvitacionAsync(string token, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var resultado = await _accesoTemporalService.UsarEnlaceTemporalAsync(token, usuarioId, ipAddress, userAgent);

            if (!resultado.Exitoso) throw new InvalidOperationException(resultado.Mensaje);
            if (resultado.TipoEntidad != TipoEntidad.EQUIPO) throw new InvalidOperationException("Este enlace no es para unirse a un equipo");
            if (resultado.EquipoData == null) throw new InvalidOperationException("No se encontró información del equipo");
            if (resultado.EquipoData.IdJugadorInvitado != usuarioId) throw new UnauthorizedAccessException("Esta invitación no es para usted");

            if (await _contexto.jugadores_equipos.AnyAsync(je => je.id_jugador == usuarioId && je.id_equipo == resultado.EquipoData.IdEquipo && je.activo == true))
                throw new InvalidOperationException("Ya eres miembro de este equipo");

            _contexto.jugadores_equipos.Add(new jugadores_equipo
            {
                codigo = CodigoHelper.GenerarCodigo("JE", 8),
                id_jugador = usuarioId,
                id_equipo = resultado.EquipoData.IdEquipo,
                es_capitan = false,
                estado = "ACTIVO",
                fecha_inicio = DateTime.UtcNow,
                activo = true
            });

            await _contexto.SaveChangesAsync();
            return await MapearJugadorResponseAsync(await _contexto.usuarios.FindAsync(usuarioId));
        }

        #endregion

        #region ========== ESTADÍSTICAS ==========

        public async Task<EstadisticasJugadorResumen> ObtenerEstadisticasAsync(int id, int? idTorneo = null)
        {
            var query = _contexto.estadisticas_jugadores.Where(ej => ej.id_jugador == id);
            if (idTorneo.HasValue) query = query.Where(ej => ej.id_torneo == idTorneo.Value);

            var estadisticas = await query.ToListAsync();

            return new EstadisticasJugadorResumen
            {
                TotalPartidos = estadisticas.Sum(e => e.partidos_jugados ?? 0),
                TotalGoles = estadisticas.Sum(e => e.goles ?? 0),
                TotalAsistencias = estadisticas.Sum(e => e.asistencias ?? 0),
                TotalTarjetasAmarillas = estadisticas.Sum(e => e.tarjetas_amarillas ?? 0),
                TotalTarjetasRojas = estadisticas.Sum(e => e.tarjetas_rojas ?? 0),
                PromedioGolesPorPartido = estadisticas.Sum(e => e.partidos_jugados ?? 0) > 0 ? (decimal)estadisticas.Sum(e => e.goles ?? 0) / estadisticas.Sum(e => e.partidos_jugados ?? 0) : 0
            };
        }

        public async Task<List<EstadisticasPorTorneoResponse>> ObtenerEstadisticasPorTorneoAsync(int id)
        {
            var estadisticas = await _contexto.estadisticas_jugadores
                .Include(ej => ej.id_torneoNavigation)
                .Where(ej => ej.id_jugador == id)
                .ToListAsync();

            return estadisticas.Select(e => new EstadisticasPorTorneoResponse
            {
                IdTorneo = e.id_torneo,
                Torneo = e.id_torneoNavigation?.nombre ?? "",
                PartidosJugados = e.partidos_jugados ?? 0,
                Goles = e.goles ?? 0,
                Asistencias = e.asistencias ?? 0,
                TarjetasAmarillas = e.tarjetas_amarillas ?? 0,
                TarjetasRojas = e.tarjetas_rojas ?? 0,
                PromedioGoles = e.partidos_jugados > 0 ? (decimal)(e.goles ?? 0) / (e.partidos_jugados ?? 1) : 0,
                PromedioCalificacion = 0
            }).ToList();
        }

        public async Task<EstadisticasAvanzadasResponse> ObtenerEstadisticasAvanzadasAsync(int id, int? idTorneo = null)
        {
            var eventosQuery = _contexto.partidos_eventos.Where(e => e.id_jugador == id);
            if (idTorneo.HasValue)
                eventosQuery = eventosQuery.Where(e => e.id_partidoNavigation.id_torneo == idTorneo.Value);

            var eventos = await eventosQuery.ToListAsync();

            return new EstadisticasAvanzadasResponse
            {
                PartidosTitular = eventos.Count(e => e.tiempo == "TITULAR"),
                PartidosSuplente = eventos.Count(e => e.tiempo == "SUPLENTE"),
                MinutosPromedio = eventos.Count > 0 ? (int)eventos.Average(e => e.minuto) : 0,
                GolesPorPartido = eventos.Count(e => e.tipo_evento == "GOL"),
                EfectividadTiros = 0,
                PasesCompletados = 0,
                PrecisionPases = 0,
                Recuperaciones = eventos.Count(e => e.tipo_evento == "RECUPERACION"),
                FaltasCometidas = eventos.Count(e => e.tipo_evento == "FALTA_COMETIDA"),
                FaltasRecibidas = eventos.Count(e => e.tipo_evento == "FALTA_RECIBIDA"),
                ManOfTheMatch = 0
            };
        }

        public async Task<ResumenTemporadaResponse> ObtenerResumenTemporadaAsync(int id, int anio)
        {
            var partidosAnio = await _contexto.partidos
                .Where(p => p.fecha_hora.Year == anio && (p.id_equipo_localNavigation.id == id || p.id_equipo_visitanteNavigation.id == id))
                .ToListAsync();

            return new ResumenTemporadaResponse
            {
                Anio = anio,
                TorneosDisputados = partidosAnio.Select(p => p.id_torneo).Distinct().Count(),
                PartidosJugados = partidosAnio.Count,
                Goles = await _contexto.partidos_eventos.CountAsync(e => e.id_jugador == id && e.tipo_evento == "GOL" && e.id_partidoNavigation.fecha_hora.Year == anio),
                Asistencias = await _contexto.partidos_eventos.CountAsync(e => e.id_jugador == id && e.tipo_evento == "ASISTENCIA" && e.id_partidoNavigation.fecha_hora.Year == anio),
                TarjetasAmarillas = await _contexto.partidos_eventos.CountAsync(e => e.id_jugador == id && e.tipo_evento == "TARJETA_AMARILLA" && e.id_partidoNavigation.fecha_hora.Year == anio),
                TarjetasRojas = await _contexto.partidos_eventos.CountAsync(e => e.id_jugador == id && e.tipo_evento == "TARJETA_ROJA" && e.id_partidoNavigation.fecha_hora.Year == anio),
                TitulosGanados = 0,
                Titulos = new List<TituloResponse>()
            };
        }

        #endregion

        #region ========== EQUIPOS Y COMPAÑEROS ==========

        public async Task<List<EquipoJugadorResponse>> ObtenerEquiposAsync(int id)
        {
            var jugadoresEquipo = await _contexto.jugadores_equipos
                .Include(je => je.id_equipoNavigation)
                .Where(je => je.id_jugador == id && je.activo == true)
                .ToListAsync();

            return jugadoresEquipo.Select(je => new EquipoJugadorResponse
            {
                IdEquipo = je.id_equipo,
                Equipo = je.id_equipoNavigation?.nombre ?? "",
                EscudoUrl = je.id_equipoNavigation?.escudo_url,
                NumeroCamiseta = je.numero_camiseta,
                Posicion = je.posicion,
                EsCapitan = je.es_capitan ?? false,
                Estado = je.estado ?? "ACTIVO",
                FechaInicio = je.fecha_inicio ?? DateTime.UtcNow
            }).ToList();
        }

        public async Task<List<CompañeroEquipoResponse>> ObtenerCompanerosEquipoAsync(int id, int idTorneo)
        {
            var equipoJugador = await _contexto.equipos_torneos
                .Where(et => et.id_torneo == idTorneo && _contexto.jugadores_equipos.Any(je => je.id_jugador == id && je.id_equipo == et.id_equipo))
                .Select(et => et.id_equipo)
                .FirstOrDefaultAsync();

            if (equipoJugador == 0) return new List<CompañeroEquipoResponse>();

            var companeros = await _contexto.jugadores_equipos
                .Include(je => je.id_jugadorNavigation)
                .Where(je => je.id_equipo == equipoJugador && je.id_jugador != id && je.activo == true)
                .Select(je => new CompañeroEquipoResponse
                {
                    IdJugador = je.id_jugador,
                    NombreCompleto = je.id_jugadorNavigation.nombres + " " + je.id_jugadorNavigation.apellidos,
                    FotoPerfil = je.id_jugadorNavigation.foto_perfil_url,
                    Posicion = je.posicion ?? "",
                    NumeroCamiseta = je.numero_camiseta,
                    EsCapitan = je.es_capitan ?? false
                }).ToListAsync();

            foreach (var companero in companeros)
            {
                var stats = await _contexto.estadisticas_jugadores.FirstOrDefaultAsync(ej => ej.id_jugador == companero.IdJugador && ej.id_torneo == idTorneo);
                if (stats != null)
                {
                    companero.Goles = stats.goles ?? 0;
                    companero.Asistencias = stats.asistencias ?? 0;
                }
            }

            return companeros;
        }

        #endregion

        #region ========== PARTIDOS ==========

        public async Task<List<PartidoJugadorResponse>> ObtenerHistorialPartidosAsync(int id, int? idTorneo = null, int? limite = 10)
        {
            var query = _contexto.partidos_eventos
                .Include(e => e.id_partidoNavigation).ThenInclude(p => p.id_torneoNavigation)
                .Include(e => e.id_partidoNavigation).ThenInclude(p => p.id_equipo_localNavigation)
                .Include(e => e.id_partidoNavigation).ThenInclude(p => p.id_equipo_visitanteNavigation)
                .Where(e => e.id_jugador == id)
                .GroupBy(e => e.id_partido)
                .Select(g => g.First().id_partidoNavigation)
                .Where(p => p.estado == "FINALIZADO");

            if (idTorneo.HasValue) query = query.Where(p => p.id_torneo == idTorneo.Value);

            var partidos = await query.OrderByDescending(p => p.fecha_hora).Take(limite ?? 10).ToListAsync();

            var response = new List<PartidoJugadorResponse>();
            foreach (var partido in partidos)
            {
                var eventosJugador = await _contexto.partidos_eventos.Where(e => e.id_partido == partido.id && e.id_jugador == id).ToListAsync();

                response.Add(new PartidoJugadorResponse
                {
                    IdPartido = partido.id,
                    IdTorneo = partido.id_torneo,
                    Torneo = partido.id_torneoNavigation?.nombre ?? "",
                    Local = partido.id_equipo_localNavigation?.nombre ?? "",
                    Visitante = partido.id_equipo_visitanteNavigation?.nombre ?? "",
                    FechaHora = partido.fecha_hora,
                    Estado = partido.estado ?? "",
                    Goles = eventosJugador.Count(e => e.tipo_evento == "GOL"),
                    Asistencias = eventosJugador.Count(e => e.tipo_evento == "ASISTENCIA"),
                    TarjetasAmarillas = eventosJugador.Count(e => e.tipo_evento == "TARJETA_AMARILLA"),
                    TarjetasRojas = eventosJugador.Count(e => e.tipo_evento == "TARJETA_ROJA"),
                    MinutosJugados = eventosJugador.FirstOrDefault()?.minuto ?? 0
                });
            }

            return response;
        }

        public async Task<List<PartidoJugadorResponse>> ObtenerProximosPartidosAsync(int id, int limite = 5)
        {
            var equiposJugador = await _contexto.jugadores_equipos.Where(je => je.id_jugador == id && je.activo == true).Select(je => je.id_equipo).ToListAsync();

            var partidos = await _contexto.partidos
                .Include(p => p.id_torneoNavigation)
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .Where(p => (equiposJugador.Contains(p.id_equipo_local) || equiposJugador.Contains(p.id_equipo_visitante)) &&
                            p.fecha_hora >= DateTime.UtcNow && p.estado == "PROGRAMADO")
                .OrderBy(p => p.fecha_hora)
                .Take(limite)
                .ToListAsync();

            return partidos.Select(p => new PartidoJugadorResponse
            {
                IdPartido = p.id,
                IdTorneo = p.id_torneo,
                Torneo = p.id_torneoNavigation?.nombre ?? "",
                Local = p.id_equipo_localNavigation?.nombre ?? "",
                Visitante = p.id_equipo_visitanteNavigation?.nombre ?? "",
                FechaHora = p.fecha_hora,
                Estado = p.estado ?? ""
            }).ToList();
        }

        #endregion

        #region ========== SUSPENSIONES ==========

        public async Task SuspenderJugadorAsync(int id, int usuarioIdAdmin, string motivo, int partidosSuspension, int? idTorneo = null, string? ipAddress = null, string? userAgent = null)
        {
            if (!await EsAdminAsync(usuarioIdAdmin))
                throw new UnauthorizedAccessException("No tiene permisos para suspender jugadores");

            if (await _contexto.usuarios.FindAsync(id) == null)
                throw new KeyNotFoundException("Jugador no encontrado");

            if (await _contexto.jugadores_suspensiones.AnyAsync(js => js.id_jugador == id && js.estado == "ACTIVA"))
                throw new InvalidOperationException("El jugador ya tiene una suspensión activa");

            if (partidosSuspension < 1) throw new InvalidOperationException("Los partidos de suspensión deben ser al menos 1");
            if (string.IsNullOrWhiteSpace(motivo)) throw new InvalidOperationException("Debe especificar un motivo");

            if (idTorneo.HasValue && await _contexto.torneos.FindAsync(idTorneo.Value) == null)
                throw new KeyNotFoundException("Torneo no encontrado");

            _contexto.jugadores_suspensiones.Add(new jugadores_suspensione
            {
                codigo = CodigoHelper.GenerarCodigo("SUS", 10),
                id_jugador = id,
                id_equipo = 0,
                id_torneo = idTorneo ?? 0,
                motivo = motivo,
                partidos_suspension = partidosSuspension,
                partidos_cumplidos = 0,
                fecha_inicio = DateOnly.FromDateTime(DateTime.UtcNow),
                fecha_fin_estimada = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(partidosSuspension * 7)),
                estado = "ACTIVA",
                fecha_registro = DateTime.UtcNow
            });

            await _contexto.SaveChangesAsync();

            await _notificacionService.EnviarNotificacionAsync(usuarioIdAdmin, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = id,
                IdTipoNotificacion = 14,
                Titulo = "Suspensión de jugador",
                Mensaje = $"Has sido suspendido por {partidosSuspension} partidos. Motivo: {motivo}",
                Prioridad = "ALTA",
                Canales = new List<string> { "IN_APP", "EMAIL" }
            });
        }

        public async Task RehabilitarJugadorAsync(int id, int usuarioIdAdmin, string? motivo = null, string? ipAddress = null, string? userAgent = null)
        {
            if (!await EsAdminAsync(usuarioIdAdmin))
                throw new UnauthorizedAccessException("No tiene permisos para rehabilitar jugadores");

            var suspensionesActivas = await _contexto.jugadores_suspensiones.Where(js => js.id_jugador == id && js.estado == "ACTIVA").ToListAsync();

            foreach (var suspension in suspensionesActivas)
                suspension.estado = "CUMPLIDA";

            await _contexto.SaveChangesAsync();

            await _notificacionService.EnviarNotificacionAsync(usuarioIdAdmin, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = id,
                IdTipoNotificacion = 16,
                Titulo = "Rehabilitación de jugador",
                Mensaje = motivo ?? "Has sido rehabilitado. Ya puedes participar en partidos.",
                Prioridad = "MEDIA"
            });
        }

        public async Task<List<SuspensionJugadorResponse>> ObtenerSuspensionesAsync(int id)
        {
            var suspensiones = await _contexto.jugadores_suspensiones
                .Include(js => js.id_torneoNavigation)
                .Include(js => js.id_equipoNavigation)
                .Where(js => js.id_jugador == id)
                .OrderByDescending(js => js.fecha_registro)
                .ToListAsync();

            return suspensiones.Select(s => new SuspensionJugadorResponse
            {
                Id = s.id,
                Motivo = s.motivo,
                PartidosSuspension = s.partidos_suspension,
                PartidosCumplidos = s.partidos_cumplidos ?? 0,
                FechaInicio = s.fecha_inicio.ToDateTime(TimeOnly.MinValue),
                FechaFinEstimada = s.fecha_fin_estimada?.ToDateTime(TimeOnly.MinValue),
                Estado = s.estado ?? "ACTIVA",
                Torneo = s.id_torneoNavigation?.nombre,
                Equipo = s.id_equipoNavigation?.nombre,
                FechaRegistro = s.fecha_registro ?? DateTime.UtcNow
            }).ToList();
        }

        #endregion

        #region ========== TRANSFERENCIAS ==========

        public async Task<SolicitudTransferenciaResponse> SolicitarTransferenciaAsync(int id, SolicitarTransferenciaRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var jugador = await _contexto.usuarios.FindAsync(id);
            if (jugador == null) throw new KeyNotFoundException("Jugador no encontrado");

            var equipoDestino = await _contexto.equipos.FindAsync(solicitud.IdEquipoDestino);
            if (equipoDestino == null) throw new KeyNotFoundException("Equipo destino no encontrado");

            var equipoOrigen = await _contexto.jugadores_equipos.FirstOrDefaultAsync(je => je.id_jugador == id && je.activo == true);
            if (equipoOrigen == null) throw new InvalidOperationException("El jugador no pertenece a ningún equipo");

            // Implementar lógica de transferencia
            return new SolicitudTransferenciaResponse
            {
                Id = 0,
                IdJugador = id,
                IdEquipoOrigen = equipoOrigen.id_equipo,
                EquipoOrigen = equipoOrigen.id_equipoNavigation?.nombre ?? "",
                IdEquipoDestino = solicitud.IdEquipoDestino,
                EquipoDestino = equipoDestino.nombre ?? "",
                Estado = "PENDIENTE",
                FechaSolicitud = DateTime.UtcNow,
                Motivo = solicitud.Motivo
            };
        }

        public async Task ProcesarTransferenciaAsync(int solicitudId, int usuarioId, bool aprobada, string? comentario = null, string? ipAddress = null, string? userAgent = null)
        {
            // Implementar lógica de procesamiento de transferencia
            await Task.CompletedTask;
        }

        #endregion

        #region ========== CREDENCIALES ==========

        public async Task<byte[]> GenerarCredencialAsync(int id, int idTorneo, string baseUrl)
        {
            var jugador = await _contexto.usuarios.FindAsync(id);
            if (jugador == null) throw new KeyNotFoundException("Jugador no encontrado");

            if (await _contexto.torneos.FindAsync(idTorneo) == null)
                throw new KeyNotFoundException("Torneo no encontrado");

            var equipoJugador = await _contexto.jugadores_equipos
                .Include(je => je.id_equipoNavigation)
                .FirstOrDefaultAsync(je => je.id_jugador == id && je.activo == true);

            if (equipoJugador == null)
                throw new InvalidOperationException("Jugador no pertenece a ningún equipo");

            var qrData = $"{baseUrl}/api/credenciales/validar?jugador={id}&equipo={equipoJugador.id_equipo}&torneo={idTorneo}";
            return _qrHelper.GenerarQRCode(qrData, 15);
        }

        #endregion
    }
}