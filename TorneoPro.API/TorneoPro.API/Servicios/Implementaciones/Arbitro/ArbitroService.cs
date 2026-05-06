using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.Arbitros;
using TorneoPro.API.DTOs.Arbitros.Request;
using TorneoPro.API.DTOs.Arbitros.Response;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Arbitro;
using TorneoPro.API.Servicios.Interfaces.Email;
using TorneoPro.API.Servicios.Interfaces.Notificacion;

namespace TorneoPro.API.Servicios.Implementaciones.Arbitro
{
    public class ArbitroService : IArbitroService
    {
        private readonly TorneoProContext _contexto;
        private readonly ILogger<ArbitroService> _logger;
        private readonly INotificacionService _notificacionService;
        private readonly IEmailService _emailService;

        public ArbitroService(
            TorneoProContext contexto,
            ILogger<ArbitroService> logger,
            INotificacionService notificacionService,
            IEmailService emailService)
        {
            _contexto = contexto;
            _logger = logger;
            _notificacionService = notificacionService;
            _emailService = emailService;
        }

        public async Task<ResultadoPaginado<ArbitroResponse>> ObtenerTodosAsync(FiltrarArbitroRequest solicitud)
        {
            var query = _contexto.usuarios
                .Where(u => u.activo == true && u.usuarios_roleid_usuarioNavigations.Any(ur => ur.id_rol == 4 && ur.estado == "ACTIVO"))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(solicitud.Buscar))
            {
                query = query.Where(u =>
                    u.nombres.Contains(solicitud.Buscar) ||
                    u.apellidos.Contains(solicitud.Buscar) ||
                    u.email.Contains(solicitud.Buscar) ||
                    u.codigo.Contains(solicitud.Buscar));
            }

            if (solicitud.Activo.HasValue)
                query = query.Where(u => u.activo == solicitud.Activo.Value);

            if (solicitud.IdTorneo.HasValue)
            {
                query = query.Where(u => _contexto.partidos
                    .Any(p => (p.id_arbitro_principal == u.id ||
                              p.id_arbitro_asistente_1 == u.id ||
                              p.id_arbitro_asistente_2 == u.id ||
                              p.id_cuarto_arbitro == u.id) &&
                              p.id_torneo == solicitud.IdTorneo.Value));
            }

            if (solicitud.Disponibles == true && solicitud.FechaDisponibilidad.HasValue)
            {
                var fecha = solicitud.FechaDisponibilidad.Value;
                query = query.Where(u => !_contexto.partidos
                    .Any(p => (p.id_arbitro_principal == u.id ||
                              p.id_arbitro_asistente_1 == u.id ||
                              p.id_arbitro_asistente_2 == u.id ||
                              p.id_cuarto_arbitro == u.id) &&
                              p.fecha_hora == fecha &&
                              p.estado != "CANCELADO"));
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
                query = query.OrderBy(u => u.apellidos);
            }

            var arbitros = await query
                .Skip((solicitud.Pagina - 1) * solicitud.TamanoPagina)
                .Take(solicitud.TamanoPagina)
                .ToListAsync();

            var items = new List<ArbitroResponse>();
            foreach (var arbitro in arbitros)
            {
                var metadata = string.IsNullOrEmpty(arbitro.metadata)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(arbitro.metadata ?? "{}") ?? new();

                items.Add(new ArbitroResponse
                {
                    Id = arbitro.id,
                    Codigo = arbitro.codigo,
                    NombreCompleto = $"{arbitro.nombres} {arbitro.apellidos}",
                    Email = arbitro.email,
                    Telefono = arbitro.telefono,
                    FotoPerfil = arbitro.foto_perfil_url,
                    Especialidad = metadata.GetValueOrDefault("especialidad")?.ToString(),
                    AniosExperiencia = metadata.GetValueOrDefault("anios_experiencia") != null
                        ? Convert.ToInt32(metadata["anios_experiencia"])
                        : null,
                    Categoria = metadata.GetValueOrDefault("categoria")?.ToString(),
                    Activo = arbitro.activo ?? false,
                    FechaRegistro = arbitro.fecha_registro ?? DateTime.UtcNow
                });
            }

            return ResultadoPaginado<ArbitroResponse>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
        }

        public async Task<ArbitroResponse?> ObtenerPorIdAsync(int id)
        {
            var arbitro = await _contexto.usuarios
                .FirstOrDefaultAsync(u => u.id == id && u.activo == true &&
                    u.usuarios_roleid_usuarioNavigations.Any(ur => ur.id_rol == 4 && ur.estado == "ACTIVO"));

            if (arbitro == null)
                return null;

            var metadata = string.IsNullOrEmpty(arbitro.metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(arbitro.metadata ?? "{}") ?? new();

            var response = new ArbitroResponse
            {
                Id = arbitro.id,
                Codigo = arbitro.codigo,
                NombreCompleto = $"{arbitro.nombres} {arbitro.apellidos}",
                Email = arbitro.email,
                Telefono = arbitro.telefono,
                FotoPerfil = arbitro.foto_perfil_url,
                Especialidad = metadata.GetValueOrDefault("especialidad")?.ToString(),
                AniosExperiencia = metadata.GetValueOrDefault("anios_experiencia") != null
                    ? Convert.ToInt32(metadata["anios_experiencia"])
                    : null,
                Categoria = metadata.GetValueOrDefault("categoria")?.ToString(),
                Activo = arbitro.activo ?? false,
                FechaRegistro = arbitro.fecha_registro ?? DateTime.UtcNow,
                PartidosAsignados = await ObtenerPartidosAsignadosAsync(id),
                Estadisticas = await ObtenerEstadisticasAsync(id)
            };

            return response;
        }

        public async Task<ArbitroResponse> RegistrarArbitroAsync(int usuarioIdAdmin, RegistrarArbitroRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioIdAdmin);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para registrar árbitros");

            var emailExiste = await _contexto.usuarios.AnyAsync(u => u.email == solicitud.Email);
            if (emailExiste)
                throw new InvalidOperationException("El email ya está registrado");

            var salt = HashHelper.GenerateSalt();
            var passwordHash = HashHelper.HashPassword(solicitud.Password, salt);

            var metadata = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(solicitud.Especialidad))
                metadata["especialidad"] = solicitud.Especialidad;
            if (solicitud.AniosExperiencia.HasValue)
                metadata["anios_experiencia"] = solicitud.AniosExperiencia.Value;
            if (!string.IsNullOrWhiteSpace(solicitud.Categoria))
                metadata["categoria"] = solicitud.Categoria;

            var usuario = new usuario
            {
                codigo = CodigoHelper.GenerarCodigo("ARB", 8),
                id_tipo_usuario = 2,
                nombres = solicitud.Nombres,
                apellidos = solicitud.Apellidos,
                email = solicitud.Email,
                telefono = solicitud.Telefono,
                password_hash = passwordHash,
                salt = salt,
                activo = true,
                email_verificado = true,
                fecha_registro = DateTime.UtcNow,
                metadata = JsonSerializer.Serialize(metadata)
            };

            _contexto.usuarios.Add(usuario);
            await _contexto.SaveChangesAsync();

            // Asignar rol ARBITRO (id_rol = 4)
            var rolArbitro = await _contexto.tipos_rols.FirstOrDefaultAsync(r => r.codigo == "ARBITRO");
            if (rolArbitro != null)
            {
                var usuarioRol = new usuarios_role
                {
                    codigo = CodigoHelper.GenerarCodigo("UR", 8),
                    id_usuario = usuario.id,
                    id_rol = rolArbitro.id,
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
                "Registro de Árbitro - TorneoPro",
                $@"
                <h1>¡Bienvenido {usuario.nombres}!</h1>
                <p>Has sido registrado como árbitro en TorneoPro.</p>
                <p>Email: {usuario.email}</p>
                <p>Contraseña: {solicitud.Password}</p>
                <p>Te recomendamos cambiar tu contraseña después de iniciar sesión.</p>",
                usuario.nombres);

            return new ArbitroResponse
            {
                Id = usuario.id,
                Codigo = usuario.codigo,
                NombreCompleto = $"{usuario.nombres} {usuario.apellidos}",
                Email = usuario.email,
                Telefono = usuario.telefono,
                Especialidad = solicitud.Especialidad,
                AniosExperiencia = solicitud.AniosExperiencia,
                Categoria = solicitud.Categoria,
                Activo = true,
                FechaRegistro = usuario.fecha_registro ?? DateTime.UtcNow
            };
        }

        public async Task<ArbitroResponse> ActualizarArbitroAsync(int id, int usuarioId, ActualizarArbitroRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            var esMismoUsuario = usuarioId == id;

            if (!esAdmin && !esMismoUsuario)
                throw new UnauthorizedAccessException("No tiene permisos para modificar este árbitro");

            var arbitro = await _contexto.usuarios.FindAsync(id);
            if (arbitro == null)
                throw new KeyNotFoundException("Árbitro no encontrado");

            if (!string.IsNullOrWhiteSpace(solicitud.Nombres))
                arbitro.nombres = solicitud.Nombres;

            if (!string.IsNullOrWhiteSpace(solicitud.Apellidos))
                arbitro.apellidos = solicitud.Apellidos;

            if (!string.IsNullOrWhiteSpace(solicitud.Telefono))
                arbitro.telefono = solicitud.Telefono;

            if (solicitud.Activo.HasValue)
                arbitro.activo = solicitud.Activo.Value;

            var metadata = string.IsNullOrEmpty(arbitro.metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(arbitro.metadata ?? "{}") ?? new();

            if (!string.IsNullOrWhiteSpace(solicitud.Especialidad))
                metadata["especialidad"] = solicitud.Especialidad;

            if (solicitud.AniosExperiencia.HasValue)
                metadata["anios_experiencia"] = solicitud.AniosExperiencia.Value;

            if (!string.IsNullOrWhiteSpace(solicitud.Categoria))
                metadata["categoria"] = solicitud.Categoria;

            arbitro.metadata = JsonSerializer.Serialize(metadata);
            arbitro.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            return new ArbitroResponse
            {
                Id = arbitro.id,
                Codigo = arbitro.codigo,
                NombreCompleto = $"{arbitro.nombres} {arbitro.apellidos}",
                Email = arbitro.email,
                Telefono = arbitro.telefono,
                Especialidad = solicitud.Especialidad,
                AniosExperiencia = solicitud.AniosExperiencia,
                Categoria = solicitud.Categoria,
                Activo = arbitro.activo ?? false,
                FechaRegistro = arbitro.fecha_registro ?? DateTime.UtcNow
            };
        }

        public async Task AsignarAPartidoAsync(int arbitroId, AsignarArbitroRequest solicitud, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para asignar árbitros");

            var partido = await _contexto.partidos
                .Include(p => p.id_torneoNavigation)
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .FirstOrDefaultAsync(p => p.id == solicitud.IdPartido && p.activo == true);

            if (partido == null)
                throw new KeyNotFoundException("Partido no encontrado");

            var arbitro = await _contexto.usuarios.FindAsync(arbitroId);
            if (arbitro == null)
                throw new KeyNotFoundException("Árbitro no encontrado");

            var tienePartido = await _contexto.partidos
                .AnyAsync(p => (p.id_arbitro_principal == arbitroId ||
                               p.id_arbitro_asistente_1 == arbitroId ||
                               p.id_arbitro_asistente_2 == arbitroId ||
                               p.id_cuarto_arbitro == arbitroId) &&
                               p.fecha_hora == partido.fecha_hora &&
                               p.id != solicitud.IdPartido &&
                               p.estado != "CANCELADO");

            if (tienePartido)
                throw new InvalidOperationException("El árbitro ya tiene un partido asignado en ese horario");

            switch (solicitud.Tipo?.ToUpperInvariant())
            {
                case "PRINCIPAL":
                    partido.id_arbitro_principal = arbitroId;
                    break;
                case "ASISTENTE1":
                    partido.id_arbitro_asistente_1 = arbitroId;
                    break;
                case "ASISTENTE2":
                    partido.id_arbitro_asistente_2 = arbitroId;
                    break;
                case "CUARTO":
                    partido.id_cuarto_arbitro = arbitroId;
                    break;
                default:
                    partido.id_arbitro_principal = arbitroId;
                    break;
            }

            partido.fecha_modificacion = DateTime.UtcNow;
            await _contexto.SaveChangesAsync();

            await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = arbitroId,
                IdTipoNotificacion = 6,
                Titulo = "Partido asignado",
                Mensaje = $"Has sido asignado como {solicitud.Tipo} del partido {partido.id_equipo_localNavigation?.nombre} vs {partido.id_equipo_visitanteNavigation?.nombre} el {partido.fecha_hora:dd/MM/yyyy HH:mm}",
                IdPartido = solicitud.IdPartido,
                Prioridad = "ALTA",
                Canales = new() { "IN_APP", "EMAIL" }
            });
        }

        public async Task DesasignarDePartidoAsync(int arbitroId, int partidoId, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para desasignar árbitros");

            var partido = await _contexto.partidos
                .FirstOrDefaultAsync(p => p.id == partidoId && p.activo == true);

            if (partido == null)
                throw new KeyNotFoundException("Partido no encontrado");

            if (partido.id_arbitro_principal == arbitroId)
                partido.id_arbitro_principal = null;
            else if (partido.id_arbitro_asistente_1 == arbitroId)
                partido.id_arbitro_asistente_1 = null;
            else if (partido.id_arbitro_asistente_2 == arbitroId)
                partido.id_arbitro_asistente_2 = null;
            else if (partido.id_cuarto_arbitro == arbitroId)
                partido.id_cuarto_arbitro = null;
            else
                throw new InvalidOperationException("El árbitro no está asignado a este partido");

            partido.fecha_modificacion = DateTime.UtcNow;
            await _contexto.SaveChangesAsync();

            await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = arbitroId,
                IdTipoNotificacion = 7,
                Titulo = "Partido desasignado",
                Mensaje = $"Has sido desasignado del partido programado para {partido.fecha_hora:dd/MM/yyyy HH:mm}",
                IdPartido = partidoId,
                Prioridad = "MEDIA"
            });
        }

        public async Task<List<PartidoAsignadoResponse>> ObtenerPartidosAsignadosAsync(int arbitroId, string? estado = null)
        {
            var query = _contexto.partidos
                .Include(p => p.id_torneoNavigation)
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .Where(p => (p.id_arbitro_principal == arbitroId ||
                            p.id_arbitro_asistente_1 == arbitroId ||
                            p.id_arbitro_asistente_2 == arbitroId ||
                            p.id_cuarto_arbitro == arbitroId) &&
                            p.activo == true);

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(p => p.estado == estado);

            var partidos = await query
                .OrderBy(p => p.fecha_hora)
                .ToListAsync();

            return partidos.Select(p =>
            {
                string tipo = "";
                if (p.id_arbitro_principal == arbitroId) tipo = "PRINCIPAL";
                else if (p.id_arbitro_asistente_1 == arbitroId) tipo = "ASISTENTE1";
                else if (p.id_arbitro_asistente_2 == arbitroId) tipo = "ASISTENTE2";
                else if (p.id_cuarto_arbitro == arbitroId) tipo = "CUARTO";

                return new PartidoAsignadoResponse
                {
                    IdPartido = p.id,
                    Partido = $"{p.id_equipo_localNavigation?.nombre} vs {p.id_equipo_visitanteNavigation?.nombre}",
                    Local = p.id_equipo_localNavigation?.nombre ?? "",
                    Visitante = p.id_equipo_visitanteNavigation?.nombre ?? "",
                    FechaHora = p.fecha_hora,
                    Tipo = tipo,
                    Estado = p.estado ?? "PROGRAMADO"
                };
            }).ToList();
        }

        public async Task RegistrarDisponibilidadAsync(int arbitroId, DisponibilidadArbitroRequest solicitud, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            var esMismoUsuario = usuarioId == arbitroId;

            if (!esAdmin && !esMismoUsuario)
                throw new UnauthorizedAccessException("No tiene permisos para registrar disponibilidad");

            var arbitro = await _contexto.usuarios.FindAsync(arbitroId);
            if (arbitro == null)
                throw new KeyNotFoundException("Árbitro no encontrado");

            var metadata = string.IsNullOrEmpty(arbitro.metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(arbitro.metadata ?? "{}") ?? new();

            var disponibilidades = metadata.ContainsKey("disponibilidad")
                ? JsonSerializer.Deserialize<List<DisponibilidadEntry>>(metadata["disponibilidad"]?.ToString() ?? "[]") ?? new List<DisponibilidadEntry>()
                : new List<DisponibilidadEntry>();

            disponibilidades.Add(new DisponibilidadEntry
            {
                FechaHora = solicitud.FechaHora,
                Disponible = solicitud.Disponible,
                MotivoNoDisponibilidad = solicitud.MotivoNoDisponibilidad,
                FechaRegistro = DateTime.UtcNow
            });

            disponibilidades = disponibilidades.Where(d => d.FechaHora > DateTime.UtcNow.AddDays(-30)).ToList();

            metadata["disponibilidad"] = disponibilidades;
            arbitro.metadata = JsonSerializer.Serialize(metadata);
            await _contexto.SaveChangesAsync();
        }

        public async Task<List<DisponibilidadArbitroResponse>> ObtenerDisponibilidadAsync(int arbitroId, DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            var arbitro = await _contexto.usuarios.FindAsync(arbitroId);
            if (arbitro == null)
                return new List<DisponibilidadArbitroResponse>();

            var metadata = string.IsNullOrEmpty(arbitro.metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(arbitro.metadata ?? "{}") ?? new();

            var disponibilidades = metadata.ContainsKey("disponibilidad")
                ? JsonSerializer.Deserialize<List<DisponibilidadEntry>>(metadata["disponibilidad"]?.ToString() ?? "[]") ?? new List<DisponibilidadEntry>()
                : new List<DisponibilidadEntry>();

            var query = disponibilidades.AsQueryable();

            if (fechaDesde.HasValue)
                query = query.Where(d => d.FechaHora >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(d => d.FechaHora <= fechaHasta.Value);

            return query.Select(d => new DisponibilidadArbitroResponse
            {
                FechaHora = d.FechaHora,
                Disponible = d.Disponible,
                MotivoNoDisponibilidad = d.MotivoNoDisponibilidad,
                FechaRegistro = d.FechaRegistro
            }).ToList();
        }

        public async Task<CalificacionArbitroResponse> CalificarArbitroAsync(int arbitroId, CalificacionArbitroRequest solicitud, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var partido = await _contexto.partidos
                .Include(p => p.id_torneoNavigation)
                .FirstOrDefaultAsync(p => p.id == solicitud.IdPartido);

            if (partido == null)
                throw new KeyNotFoundException("Partido no encontrado");

            var arbitro = await _contexto.usuarios.FindAsync(arbitroId);
            if (arbitro == null)
                throw new KeyNotFoundException("Árbitro no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para calificar árbitros");

            var metadata = string.IsNullOrEmpty(arbitro.metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(arbitro.metadata ?? "{}") ?? new();

            var calificaciones = metadata.ContainsKey("calificaciones")
                ? JsonSerializer.Deserialize<List<CalificacionEntry>>(metadata["calificaciones"]?.ToString() ?? "[]") ?? new List<CalificacionEntry>()
                : new List<CalificacionEntry>();

            var calificacionEntry = new CalificacionEntry
            {
                IdPartido = solicitud.IdPartido,
                Puntuacion = solicitud.Puntuacion,
                Comentario = solicitud.Comentario,
                IdUsuarioCalificador = usuarioId,
                FechaCalificacion = DateTime.UtcNow
            };

            calificaciones.Add(calificacionEntry);
            metadata["calificaciones"] = calificaciones;

            var promedio = calificaciones.Average(c => (double)c.Puntuacion);
            metadata["promedio_calificacion"] = promedio;

            arbitro.metadata = JsonSerializer.Serialize(metadata);
            await _contexto.SaveChangesAsync();

            var usuarioCalificador = await _contexto.usuarios.FindAsync(usuarioId);

            return new CalificacionArbitroResponse
            {
                Id = calificaciones.Count,
                IdPartido = solicitud.IdPartido,
                Partido = $"{partido.id_equipo_localNavigation?.nombre} vs {partido.id_equipo_visitanteNavigation?.nombre}",
                Puntuacion = solicitud.Puntuacion,
                Comentario = solicitud.Comentario,
                FechaCalificacion = calificacionEntry.FechaCalificacion,
                CalificadoPor = usuarioCalificador != null ? $"{usuarioCalificador.nombres} {usuarioCalificador.apellidos}" : ""
            };
        }

        public async Task<List<CalificacionArbitroResponse>> ObtenerCalificacionesAsync(int arbitroId)
        {
            var arbitro = await _contexto.usuarios.FindAsync(arbitroId);
            if (arbitro == null)
                return new List<CalificacionArbitroResponse>();

            var metadata = string.IsNullOrEmpty(arbitro.metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(arbitro.metadata ?? "{}") ?? new();

            var calificaciones = metadata.ContainsKey("calificaciones")
                ? JsonSerializer.Deserialize<List<CalificacionEntry>>(metadata["calificaciones"]?.ToString() ?? "[]") ?? new List<CalificacionEntry>()
                : new List<CalificacionEntry>();

            var result = new List<CalificacionArbitroResponse>();

            foreach (var cal in calificaciones.OrderByDescending(c => c.FechaCalificacion))
            {
                var partido = await _contexto.partidos
                    .Include(p => p.id_equipo_localNavigation)
                    .Include(p => p.id_equipo_visitanteNavigation)
                    .FirstOrDefaultAsync(p => p.id == cal.IdPartido);

                var usuarioCalificador = await _contexto.usuarios.FindAsync(cal.IdUsuarioCalificador);

                result.Add(new CalificacionArbitroResponse
                {
                    Id = calificaciones.IndexOf(cal) + 1,
                    IdPartido = cal.IdPartido,
                    Partido = partido != null ? $"{partido.id_equipo_localNavigation?.nombre} vs {partido.id_equipo_visitanteNavigation?.nombre}" : "Partido no encontrado",
                    Puntuacion = cal.Puntuacion,
                    Comentario = cal.Comentario,
                    FechaCalificacion = cal.FechaCalificacion,
                    CalificadoPor = usuarioCalificador != null ? $"{usuarioCalificador.nombres} {usuarioCalificador.apellidos}" : ""
                });
            }

            return result;
        }

        public async Task<EstadisticasArbitroResponse> ObtenerEstadisticasAsync(int arbitroId)
        {
            var partidosAsignados = await _contexto.partidos
                .Where(p => (p.id_arbitro_principal == arbitroId ||
                            p.id_arbitro_asistente_1 == arbitroId ||
                            p.id_arbitro_asistente_2 == arbitroId ||
                            p.id_cuarto_arbitro == arbitroId) &&
                            p.estado == "FINALIZADO")
                .ToListAsync();

            var partidosComoPrincipal = partidosAsignados.Count(p => p.id_arbitro_principal == arbitroId);
            var partidosComoAsistente = partidosAsignados.Count(p => p.id_arbitro_principal != arbitroId);

            var eventosPartidos = await _contexto.partidos_eventos
                .Where(e => partidosAsignados.Select(p => p.id).Contains(e.id_partido))
                .ToListAsync();

            var totalAmarillas = eventosPartidos.Count(e => e.tipo_evento == "TARJETA_AMARILLA");
            var totalRojas = eventosPartidos.Count(e => e.tipo_evento == "TARJETA_ROJA");

            var arbitro = await _contexto.usuarios.FindAsync(arbitroId);
            double? promedioCalificacion = null;

            if (arbitro != null && !string.IsNullOrEmpty(arbitro.metadata))
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(arbitro.metadata ?? "{}");
                if (metadata != null && metadata.TryGetValue("promedio_calificacion", out var promedioObj))
                {
                    promedioCalificacion = Convert.ToDouble(promedioObj);
                }
            }

            return new EstadisticasArbitroResponse
            {
                TotalPartidos = partidosAsignados.Count,
                PartidosComoPrincipal = partidosComoPrincipal,
                PartidosComoAsistente = partidosComoAsistente,
                TotalTarjetasAmarillas = totalAmarillas,
                TotalTarjetasRojas = totalRojas,
                PromedioTarjetasPorPartido = partidosAsignados.Count > 0
                    ? (double)(totalAmarillas + totalRojas) / partidosAsignados.Count
                    : 0,
                PromedioCalificacion = promedioCalificacion
            };
        }

        public async Task<List<ArbitroResponse>> BuscarArbitrosDisponiblesAsync(DateTime fechaHora, string? especialidad = null)
        {
            var query = _contexto.usuarios
                .Where(u => u.activo == true &&
                           u.usuarios_roleid_usuarioNavigations.Any(ur => ur.id_rol == 4 && ur.estado == "ACTIVO"));

            if (!string.IsNullOrWhiteSpace(especialidad))
            {
                query = query.Where(u => u.metadata.Contains(especialidad));
            }

            query = query.Where(u => !_contexto.partidos
                .Any(p => (p.id_arbitro_principal == u.id ||
                          p.id_arbitro_asistente_1 == u.id ||
                          p.id_arbitro_asistente_2 == u.id ||
                          p.id_cuarto_arbitro == u.id) &&
                          p.fecha_hora == fechaHora &&
                          p.estado != "CANCELADO"));

            var arbitros = await query.ToListAsync();

            var response = new List<ArbitroResponse>();
            foreach (var arbitro in arbitros)
            {
                var metadata = string.IsNullOrEmpty(arbitro.metadata)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(arbitro.metadata ?? "{}") ?? new();

                response.Add(new ArbitroResponse
                {
                    Id = arbitro.id,
                    Codigo = arbitro.codigo,
                    NombreCompleto = $"{arbitro.nombres} {arbitro.apellidos}",
                    Email = arbitro.email,
                    Telefono = arbitro.telefono,
                    Especialidad = metadata.GetValueOrDefault("especialidad")?.ToString(),
                    AniosExperiencia = metadata.GetValueOrDefault("anios_experiencia") != null
                        ? Convert.ToInt32(metadata["anios_experiencia"])
                        : null,
                    Categoria = metadata.GetValueOrDefault("categoria")?.ToString(),
                    Activo = arbitro.activo ?? false
                });
            }

            return response;
        }

        #region Métodos Privados

        private async Task<bool> EsAdminAsync(int usuarioId)
        {
            return await _contexto.usuarios_roles
                .AnyAsync(ur => ur.id_usuario == usuarioId &&
                               (ur.id_rol == 1 || ur.id_rol == 2) &&
                               ur.estado == "ACTIVO");
        }

        public async Task EliminarDisponibilidadAsync(int arbitroId, DateTime fechaHora, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para eliminar disponibilidad");

            var arbitro = await _contexto.usuarios.FindAsync(arbitroId);
            if (arbitro == null)
                throw new KeyNotFoundException("Árbitro no encontrado");

            var metadata = string.IsNullOrEmpty(arbitro.metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(arbitro.metadata ?? "{}") ?? new();

            var disponibilidades = metadata.ContainsKey("disponibilidad")
                ? JsonSerializer.Deserialize<List<DisponibilidadEntry>>(metadata["disponibilidad"]?.ToString() ?? "[]") ?? new List<DisponibilidadEntry>()
                : new List<DisponibilidadEntry>();

            var itemToRemove = disponibilidades.FirstOrDefault(d => d.FechaHora == fechaHora);
            if (itemToRemove == null)
                throw new KeyNotFoundException("Disponibilidad no encontrada");

            disponibilidades.Remove(itemToRemove);
            metadata["disponibilidad"] = disponibilidades;
            arbitro.metadata = JsonSerializer.Serialize(metadata);
            await _contexto.SaveChangesAsync();
        }

        #endregion
    }

    // Clases auxiliares para almacenar datos en metadata
    public class DisponibilidadEntry
    {
        public DateTime FechaHora { get; set; }
        public bool Disponible { get; set; }
        public string? MotivoNoDisponibilidad { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class CalificacionEntry
    {
        public int IdPartido { get; set; }
        public int Puntuacion { get; set; }
        public string? Comentario { get; set; }
        public int IdUsuarioCalificador { get; set; }
        public DateTime FechaCalificacion { get; set; }
    }
}