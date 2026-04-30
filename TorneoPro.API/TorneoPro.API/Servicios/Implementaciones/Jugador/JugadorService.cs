using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TorneoPro.API.Controllers;
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

        private string ObtenerBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return "http://localhost:5293";
            return $"{request.Scheme}://{request.Host}";
        }

        public async Task<ResultadoPaginado<JugadorResponse>> ObtenerTodosAsync(FiltrarJugadorRequest solicitud)
        {
            var query = _contexto.usuarios
                .Include(u => u.id_tipo_usuarioNavigation)
                .Where(u => u.activo == true && u.id_tipo_usuario == 1)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(solicitud.Buscar))
            {
                query = query.Where(u =>
                    u.nombres.Contains(solicitud.Buscar) ||
                    u.apellidos.Contains(solicitud.Buscar) ||
                    u.email.Contains(solicitud.Buscar) ||
                    u.codigo.Contains(solicitud.Buscar));
            }

            if (!string.IsNullOrWhiteSpace(solicitud.Ciudad))
                query = query.Where(u => u.ciudad == solicitud.Ciudad);

            if (!string.IsNullOrWhiteSpace(solicitud.Pais))
                query = query.Where(u => u.pais == solicitud.Pais);

            if (solicitud.SoloActivos == true)
                query = query.Where(u => u.activo == true);

            if (solicitud.IdEquipo.HasValue)
            {
                query = query.Where(u => _contexto.jugadores_equipos
                    .Any(je => je.id_jugador == u.id && je.id_equipo == solicitud.IdEquipo.Value && je.activo == true));
            }

            if (solicitud.SoloDisponibles == true)
            {
                query = query.Where(u => !_contexto.jugadores_suspensiones
                    .Any(js => js.id_jugador == u.id && js.estado == "ACTIVA"));
            }

            if (solicitud.EdadMinima.HasValue || solicitud.EdadMaxima.HasValue)
            {
                var fechaActual = DateTime.UtcNow;
                if (solicitud.EdadMinima.HasValue)
                {
                    var fechaMaxNacimiento = fechaActual.AddYears(-solicitud.EdadMinima.Value);
                    query = query.Where(u => u.fecha_nacimiento <= DateOnly.FromDateTime(fechaMaxNacimiento));
                }
                if (solicitud.EdadMaxima.HasValue)
                {
                    var fechaMinNacimiento = fechaActual.AddYears(-solicitud.EdadMaxima.Value - 1);
                    query = query.Where(u => u.fecha_nacimiento >= DateOnly.FromDateTime(fechaMinNacimiento));
                }
            }

            var totalItems = await query.CountAsync();
            query = query.OrderByDescending(u => u.fecha_registro);

            var usuarios = await query
                .Skip((solicitud.Pagina - 1) * solicitud.TamanoPagina)
                .Take(solicitud.TamanoPagina)
                .ToListAsync();

            var items = new List<JugadorResponse>();
            foreach (var usuario in usuarios)
            {
                var response = await MapearJugadorResponseAsync(usuario);
                items.Add(response);
            }

            return ResultadoPaginado<JugadorResponse>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
        }

        public async Task<JugadorResponse?> ObtenerPorIdAsync(int id)
        {
            var usuario = await _contexto.usuarios
                .FirstOrDefaultAsync(u => u.id == id && u.activo == true && u.id_tipo_usuario == 1);

            if (usuario == null)
                return null;

            return await MapearJugadorResponseAsync(usuario);
        }

        public async Task<JugadorResponse?> ObtenerPorEmailAsync(string email)
        {
            var usuario = await _contexto.usuarios
                .FirstOrDefaultAsync(u => u.email == email && u.activo == true && u.id_tipo_usuario == 1);

            if (usuario == null)
                return null;

            return await MapearJugadorResponseAsync(usuario);
        }

        public async Task<JugadorResponse> RegistrarJugadorAsync(int usuarioIdAdmin, RegistrarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioIdAdmin);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para registrar jugadores");

            var emailExiste = await _contexto.usuarios.AnyAsync(u => u.email == solicitud.Email);
            if (emailExiste)
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
                var usuarioRol = new usuarios_role
                {
                    codigo = CodigoHelper.GenerarCodigo("UR", 8),
                    id_usuario = usuario.id,
                    id_rol = rolJugador.id,
                    fecha_inicio = DateOnly.FromDateTime(DateTime.UtcNow),
                    estado = "ACTIVO",
                    origen_asignacion = "MANUAL",
                    activo = true
                };
                _contexto.usuarios_roles.Add(usuarioRol);
                await _contexto.SaveChangesAsync();
            }

            await _emailService.EnviarNotificacionEmailAsync(
    usuario.email,
    "¡Bienvenido a TorneoPro!",
    $@"
    <div style='font-family: Arial, sans-serif; background-color:#f4f6f8; padding:20px;'>
        <div style='max-width:600px; margin:0 auto; background:#ffffff; border-radius:10px; overflow:hidden; box-shadow:0 4px 10px rgba(0,0,0,0.05);'>
            <div style='background:linear-gradient(135deg,#1E3A8A,#2563EB); color:#ffffff; padding:20px; text-align:center;'>
                <h1 style='margin:0;'>⚽ TorneoPro</h1>
                <p style='margin:5px 0 0;'>Bienvenido a la competencia</p>
            </div>
            <div style='padding:25px; color:#333;'>
                <h2 style='margin-top:0;'>¡Hola {usuario.nombres}! 👋</h2>
                <p>Tu cuenta ha sido creada exitosamente como <strong>jugador</strong> en TorneoPro.</p>

                <div style='background:#f1f5f9; padding:15px; border-radius:8px; margin:20px 0;'>
                    <p style='margin:5px 0;'><strong>📧 Email:</strong> {usuario.email}</p>
                    <p style='margin:5px 0;'><strong>🔑 Contraseña:</strong> {solicitud.Password}</p>
                </div>

                <p style='color:#b91c1c; font-weight:bold;'>
                    ⚠️ Por seguridad, te recomendamos cambiar tu contraseña después de iniciar sesión.
                </p>

                <p>Ya puedes ingresar a la plataforma utilizando tus credenciales.</p>
                <p>¡Nos alegra tenerte en TorneoPro! 🚀</p>
            </div>
            <div style='background:#f9fafb; text-align:center; padding:15px; font-size:12px; color:#888;'>
                © {DateTime.UtcNow.Year} TorneoPro. Todos los derechos reservados.
            </div>
        </div>
    </div>",
    usuario.nombres
);

            return await MapearJugadorResponseAsync(usuario);
        }

        public async Task<JugadorResponse> ActualizarJugadorAsync(int id, int usuarioId, ActualizarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var usuario = await _contexto.usuarios.FindAsync(id);
            if (usuario == null)
                throw new KeyNotFoundException("Jugador no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esMismoUsuario = usuarioId == id;

            if (!esAdmin && !esMismoUsuario)
                throw new UnauthorizedAccessException("No tiene permisos para modificar este jugador");

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
                usuario.fecha_nacimiento = DateOnly.FromDateTime(solicitud.FechaNacimiento.Value);

            if (!string.IsNullOrWhiteSpace(solicitud.TelefonoEmergencia))
                usuario.telefono_emergencia = solicitud.TelefonoEmergencia;

            var metadata = string.IsNullOrEmpty(usuario.metadata) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(usuario.metadata ?? "{}") ?? new();
            if (!string.IsNullOrWhiteSpace(solicitud.PosicionPreferida))
                metadata["posicion_preferida"] = solicitud.PosicionPreferida;
            if (solicitud.NumeroCamisetaPreferido.HasValue)
                metadata["numero_preferido"] = solicitud.NumeroCamisetaPreferido.Value;
            usuario.metadata = JsonSerializer.Serialize(metadata);

            usuario.fecha_modificacion = DateTime.UtcNow;
            await _contexto.SaveChangesAsync();

            return await MapearJugadorResponseAsync(usuario);
        }

        public async Task<InvitarJugadorResponse> InvitarJugadorAsync(int usuarioIdAdmin, InvitarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioIdAdmin);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para invitar jugadores");

            var equipo = await _contexto.equipos.FindAsync(solicitud.IdEquipo);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var usuarioExistente = await _contexto.usuarios.FirstOrDefaultAsync(u => u.email == solicitud.Email);

            if (usuarioExistente != null)
            {
                // Usuario YA existe - crear enlace temporal para unirse directamente
                var enlaceTemporal = await _accesoTemporalService.CrearEnlaceInvitacionEquipoAsync(
                    usuarioIdAdmin,
                    solicitud.IdEquipo,
                    usuarioExistente.id,
                    ipAddress,
                    userAgent);

                var inviteUrl = enlaceTemporal.InviteUrl;

                await _emailService.EnviarNotificacionEmailAsync(
                    solicitud.Email,
                    "Invitación a equipo - TorneoPro",
                    $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h1>¡Has sido invitado a un equipo!</h1>
                        <p>Has sido invitado al equipo <strong>{equipo.nombre}</strong>.</p>
                        <p>Haz clic en el siguiente enlace para unirte al equipo:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{inviteUrl}' style='background: #2563EB; color: white; padding: 12px 25px; border-radius: 8px; text-decoration: none; display: inline-block;'>
                                📱 Unirse al equipo
                            </a>
                        </div>
                        <p>Este enlace expirará en 7 días.</p>
                    </div>",
                    usuarioExistente.nombres);

                return new InvitarJugadorResponse
                {
                    Exitoso = true,
                    Mensaje = "Invitación enviada al jugador existente",
                    EnlaceInvitacion = inviteUrl,
                    EsUsuarioExistente = true,
                    IdJugador = usuarioExistente.id
                };
            }

            // 1. Crear enlace temporal para el equipo (asociado al email)
            var tokenEquipo = GenerarTokenUnico();
            var fechaExpiracion = DateTime.UtcNow.AddDays(7);

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
                fecha_expiracion = fechaExpiracion,
                max_usos = 1,
                usos_actuales = 0,
                estado = "ACTIVO",
                metadata = JsonSerializer.Serialize(metadata),
                activo = true
            };

            _contexto.enlaces_compartidos.Add(enlacePendiente);
            await _contexto.SaveChangesAsync();

            // 2. Crear URL de registro (web intermedia, NO es la API directamente)
            var baseUrl = ObtenerBaseUrl();
            var registroWebUrl = $"{baseUrl}/registro?tokenEquipo={tokenEquipo}";

            // 3. Enviar email con enlace de registro
            await _emailService.EnviarNotificacionEmailAsync(
                solicitud.Email,
                "Invitación a TorneoPro - Registro de jugador",
                $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='text-align: center; margin-bottom: 20px;'>
                        <h1>⚽ TorneoPro</h1>
                    </div>
                    
                    <h2>¡Has sido invitado a un equipo!</h2>
                    <p>Has sido invitado al equipo <strong>{equipo.nombre}</strong>.</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{registroWebUrl}' 
                           style='background: #2563EB; color: white; padding: 14px 28px; 
                                  border-radius: 8px; text-decoration: none; display: inline-block; font-weight: bold;'>
                            📱 Registrarse y unirse al equipo
                        </a>
                    </div>
                    
                    <p style='font-size: 12px; color: #666;'>Este enlace expirará en 7 días.</p>
                    <p style='font-size: 12px; color: #666;'>Si ya tienes cuenta, inicia sesión y te unirás automáticamente al equipo.</p>
                </div>",
                "Nuevo usuario");

            return new InvitarJugadorResponse
            {
                Exitoso = true,
                Mensaje = "Invitación enviada para registro de nuevo jugador",
                EnlaceInvitacion = registroWebUrl,
                EsUsuarioExistente = false,
                TokenEquipo = tokenEquipo
            };
        }

        private string GenerarTokenUnico()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "")
                .ToLowerInvariant()
                .Substring(0, 32);
        }

        public async Task<JugadorResponse> AceptarInvitacionAsync(string token, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            // Usar el enlace temporal del servicio de acceso temporal
            var resultado = await _accesoTemporalService.UsarEnlaceTemporalAsync(token, usuarioId, ipAddress, userAgent);

            if (!resultado.Exitoso)
                throw new InvalidOperationException(resultado.Mensaje);

            if (resultado.TipoEntidad != TipoEntidad.EQUIPO)
                throw new InvalidOperationException("Este enlace no es para unirse a un equipo");

            if (resultado.EquipoData == null)
                throw new InvalidOperationException("No se encontró información del equipo");

            if (resultado.EquipoData.IdJugadorInvitado != usuarioId)
                throw new UnauthorizedAccessException("Esta invitación no es para usted");

            var yaEnEquipo = await _contexto.jugadores_equipos
                .AnyAsync(je => je.id_jugador == usuarioId && je.id_equipo == resultado.EquipoData.IdEquipo && je.activo == true);

            if (yaEnEquipo)
                throw new InvalidOperationException("Ya eres miembro de este equipo");

            var jugadorEquipo = new jugadores_equipo
            {
                codigo = CodigoHelper.GenerarCodigo("JE", 8),
                id_jugador = usuarioId,
                id_equipo = resultado.EquipoData.IdEquipo,
                es_capitan = false,
                estado = "ACTIVO",
                fecha_inicio = DateTime.UtcNow,
                activo = true
            };

            _contexto.jugadores_equipos.Add(jugadorEquipo);
            await _contexto.SaveChangesAsync();

            return await MapearJugadorResponseAsync(await _contexto.usuarios.FindAsync(usuarioId));
        }

        public async Task<EstadisticasJugadorResumen> ObtenerEstadisticasAsync(int id, int? idTorneo = null)
        {
            var query = _contexto.estadisticas_jugadores
                .Where(ej => ej.id_jugador == id);

            if (idTorneo.HasValue)
                query = query.Where(ej => ej.id_torneo == idTorneo.Value);

            var estadisticas = await query.ToListAsync();

            return new EstadisticasJugadorResumen
            {
                TotalPartidos = estadisticas.Sum(e => e.partidos_jugados ?? 0),
                TotalGoles = estadisticas.Sum(e => e.goles ?? 0),
                TotalAsistencias = estadisticas.Sum(e => e.asistencias ?? 0),
                TotalTarjetasAmarillas = estadisticas.Sum(e => e.tarjetas_amarillas ?? 0),
                TotalTarjetasRojas = estadisticas.Sum(e => e.tarjetas_rojas ?? 0),
                PromedioGolesPorPartido = estadisticas.Sum(e => e.partidos_jugados ?? 0) > 0
                    ? (decimal)estadisticas.Sum(e => e.goles ?? 0) / estadisticas.Sum(e => e.partidos_jugados ?? 0)
                    : 0
            };
        }

        public async Task<List<EquipoJugadorResponse>> ObtenerEquiposAsync(int id)
        {
            var jugadoresEquipo = await _contexto.jugadores_equipos
                .Include(je => je.id_equipoNavigation)
                .Where(je => je.id_jugador == id && je.activo == true)
                .ToListAsync();

            var response = new List<EquipoJugadorResponse>();
            foreach (var je in jugadoresEquipo)
            {
                response.Add(new EquipoJugadorResponse
                {
                    IdEquipo = je.id_equipo,
                    Equipo = je.id_equipoNavigation?.nombre ?? "",
                    EscudoUrl = je.id_equipoNavigation?.escudo_url,
                    NumeroCamiseta = je.numero_camiseta,
                    Posicion = je.posicion,
                    EsCapitan = je.es_capitan ?? false,
                    Estado = je.estado ?? "ACTIVO",
                    FechaInicio = je.fecha_inicio ?? DateTime.UtcNow
                });
            }

            return response;
        }

        public async Task SuspenderJugadorAsync(int id, int usuarioIdAdmin, string motivo, int partidosSuspension, int? idTorneo = null, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioIdAdmin);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para suspender jugadores");

            var jugador = await _contexto.usuarios.FindAsync(id);
            if (jugador == null)
                throw new KeyNotFoundException("Jugador no encontrado");

            var suspension = new jugadores_suspensione
            {
                codigo = CodigoHelper.GenerarCodigo("SUS", 10),
                id_jugador = id,
                id_equipo = 0,
                id_torneo = idTorneo ?? 0,
                motivo = motivo,
                partidos_suspension = partidosSuspension,
                fecha_inicio = DateOnly.FromDateTime(DateTime.UtcNow),
                fecha_fin_estimada = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(partidosSuspension * 7)),
                estado = "ACTIVA",
                fecha_registro = DateTime.UtcNow
            };

            _contexto.jugadores_suspensiones.Add(suspension);
            await _contexto.SaveChangesAsync();

            await _notificacionService.EnviarNotificacionAsync(usuarioIdAdmin, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = id,
                IdTipoNotificacion = 14,
                Titulo = "Suspensión de jugador",
                Mensaje = $"Has sido suspendido por {partidosSuspension} partidos. Motivo: {motivo}",
                Prioridad = "ALTA"
            });
        }

        public async Task RehabilitarJugadorAsync(int id, int usuarioIdAdmin, string? motivo = null, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioIdAdmin);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para rehabilitar jugadores");

            var suspensionesActivas = await _contexto.jugadores_suspensiones
                .Where(js => js.id_jugador == id && js.estado == "ACTIVA")
                .ToListAsync();

            foreach (var suspension in suspensionesActivas)
            {
                suspension.estado = "CUMPLIDA";
            }

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

        public async Task<byte[]> GenerarCredencialAsync(int id, int idTorneo, string baseUrl)
        {
            var jugador = await _contexto.usuarios.FindAsync(id);
            if (jugador == null)
                throw new KeyNotFoundException("Jugador no encontrado");

            var torneo = await _contexto.torneos.FindAsync(idTorneo);
            if (torneo == null)
                throw new KeyNotFoundException("Torneo no encontrado");

            var equipoJugador = await _contexto.jugadores_equipos
                .Include(je => je.id_equipoNavigation)
                .FirstOrDefaultAsync(je => je.id_jugador == id && je.activo == true);

            if (equipoJugador == null)
                throw new InvalidOperationException("Jugador no pertenece a ningún equipo");

            var qrData = $"{baseUrl}/api/credenciales/validar?jugador={id}&equipo={equipoJugador.id_equipo}&torneo={idTorneo}";
            var qrBytes = _qrHelper.GenerarQRCode(qrData, 15);
            return qrBytes;
        }

        

        #region Métodos Privados

        private async Task<bool> EsAdminAsync(int usuarioId)
        {
            return await _contexto.usuarios_roles
                .AnyAsync(ur => ur.id_usuario == usuarioId &&
                               (ur.id_rol == 1 || ur.id_rol == 2) &&
                               ur.estado == "ACTIVO");
        }

        private async Task<JugadorResponse> MapearJugadorResponseAsync(usuario usuario)
        {
            var metadata = string.IsNullOrEmpty(usuario.metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(usuario.metadata ?? "{}") ?? new();

            var response = new JugadorResponse
            {
                Id = usuario.id,
                Codigo = usuario.codigo,
                NombreCompleto = $"{usuario.nombres} {usuario.apellidos}",
                Email = usuario.email,
                Telefono = usuario.telefono,
                FotoPerfil = usuario.foto_perfil_url,
                PosicionPreferida = metadata.TryGetValue("posicion_preferida", out var posVal) && posVal is JsonElement posElem
                    ? posElem.GetString()
                    : null,
                NumeroCamisetaPreferido = metadata.TryGetValue("numero_preferido", out var numVal) && numVal is JsonElement numElem
                    ? numElem.TryGetInt32(out var num) ? num : null
                    : null,
                PesoKg = usuario.peso_kg,
                AlturaCm = usuario.altura_cm,
                Activo = usuario.activo ?? false,
                EmailVerificado = usuario.email_verificado ?? false,
                FechaRegistro = usuario.fecha_registro ?? DateTime.UtcNow,
                Equipos = await ObtenerEquiposAsync(usuario.id),
                Estadisticas = await ObtenerEstadisticasAsync(usuario.id)
            };

            return response;
        }

        #endregion
    }
}