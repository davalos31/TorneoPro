using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.Equipo.Response;
using TorneoPro.API.DTOs.Equipos;
using TorneoPro.API.DTOs.Equipos.Request;
using TorneoPro.API.DTOs.Equipos.Response;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Email;
using TorneoPro.API.Servicios.Interfaces.Equipo;
using TorneoPro.API.Servicios.Interfaces.Notificacion;
using ActualizarJugadorRequest = TorneoPro.API.DTOs.Equipos.Request.ActualizarJugadorRequest;

namespace TorneoPro.API.Servicios.Implementaciones.Equipo
{
    public class EquipoService : IEquipoService
    {
        private readonly TorneoProContext _contexto;
        private readonly ArchivosHelper _archivosHelper;
        private readonly ILogger<EquipoService> _logger;
        private readonly IConfiguration _configuracion;
        private readonly INotificacionService _notificacionService;
        private readonly IEmailService _emailService;

        public EquipoService(
            TorneoProContext contexto,
            ArchivosHelper archivosHelper,
            ILogger<EquipoService> logger,
            IConfiguration configuracion,
            INotificacionService notificacionService,
            IEmailService emailService)
        {
            _contexto = contexto;
            _archivosHelper = archivosHelper;
            _logger = logger;
            _configuracion = configuracion;
            _notificacionService = notificacionService;
            _emailService = emailService;
        }

        #region ========== MÉTODOS PRIVADOS ==========

        private async Task<bool> EsAdminAsync(int usuarioId)
        {
            return await _contexto.usuarios_roles
                .AnyAsync(ur => ur.id_usuario == usuarioId &&
                               (ur.id_rol == 1 || ur.id_rol == 2) &&
                               ur.estado == "ACTIVO");
        }

        private async Task<int> ObtenerIdRolPorCodigoAsync(string codigo)
        {
            var rol = await _contexto.tipos_rols
                .FirstOrDefaultAsync(r => r.codigo == codigo && r.activo == true);

            if (rol == null)
                throw new KeyNotFoundException($"Rol '{codigo}' no encontrado");

            return rol.id;
        }

        private async Task<int> ObtenerIdTipoNotificacionPorCodigoAsync(string codigo)
        {
            var tipo = await _contexto.tipos_notificacions
                .FirstOrDefaultAsync(t => t.codigo == codigo && t.activo == true);

            return tipo?.id ?? 0;
        }

        private EquipoResponse MapearEquipoResponse(equipo equipo)
        {
            return new EquipoResponse
            {
                Id = equipo.id,
                Codigo = equipo.codigo,
                Nombre = equipo.nombre,
                NombreCorto = equipo.nombre_corto,
                EscudoUrl = equipo.escudo_url,
                ColorPrimario = equipo.color_primario,
                ColorSecundario = equipo.color_secundario,
                Ciudad = equipo.ciudad,
                Pais = equipo.pais,
                Estado = equipo.estado ?? "ACTIVO",
                Verificado = equipo.verificado ?? false,
                IdCapitan = equipo.id_capitan,
                Capitan = equipo.id_capitanNavigation != null
                    ? $"{equipo.id_capitanNavigation.nombres} {equipo.id_capitanNavigation.apellidos}"
                    : null,
                TotalJugadores = 0
            };
        }

        private EquipoDetalleResponse MapearEquipoDetalleResponse(equipo equipo)
        {
            return new EquipoDetalleResponse
            {
                Id = equipo.id,
                Codigo = equipo.codigo,
                Nombre = equipo.nombre,
                NombreCorto = equipo.nombre_corto,
                EscudoUrl = equipo.escudo_url,
                ColorPrimario = equipo.color_primario,
                ColorSecundario = equipo.color_secundario,
                Ciudad = equipo.ciudad,
                Pais = equipo.pais,
                Estado = equipo.estado ?? "ACTIVO",
                Verificado = equipo.verificado ?? false,
                IdCapitan = equipo.id_capitan,
                Capitan = equipo.id_capitanNavigation != null
                    ? $"{equipo.id_capitanNavigation.nombres} {equipo.id_capitanNavigation.apellidos}"
                    : null,
                FechaFundacion = equipo.fecha_fundacion.HasValue
                    ? equipo.fecha_fundacion.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,
                EstadioHabitual = equipo.estadio_habitual,
                Email = equipo.email,
                Telefono = equipo.telefono,
                SitioWeb = equipo.sitio_web,
                IdCreador = equipo.id_creador,
                Creador = equipo.id_creadorNavigation != null
                    ? $"{equipo.id_creadorNavigation.nombres} {equipo.id_creadorNavigation.apellidos}"
                    : string.Empty,
                FechaCreacion = equipo.fecha_creacion ?? DateTime.UtcNow,
                Jugadores = new List<JugadorEquipoResponse>(),
                Torneos = new List<EquipoTorneoResponse>(),
                TotalJugadores = 0
            };
        }

        private EquipoTorneoResponse MapearEquipoTorneoResponse(equipos_torneo et)
        {
            return new EquipoTorneoResponse
            {
                Id = et.id,
                IdTorneo = et.id_torneo,
                Torneo = et.id_torneoNavigation?.nombre ?? "",
                TorneoLogo = et.id_torneoNavigation?.logo_url,
                FechaInscripcion = et.fecha_inscripcion ?? DateTime.UtcNow,
                Aprobado = et.aprobado ?? false,
                FechaAprobacion = et.fecha_aprobacion,
                Estado = et.estado ?? "PENDIENTE",
                IdFase = et.id_torneo_fase,
                Fase = et.id_torneo_faseNavigation?.nombre,
                Grupo = et.id_torneo_faseNavigation?.nombre_grupo,
                TieneBye = et.tiene_bye ?? false
            };
        }

        private JugadorEquipoResponse MapearJugadorEquipoResponse(jugadores_equipo je)
        {
            return new JugadorEquipoResponse
            {
                Id = je.id,
                IdJugador = je.id_jugador,
                NombreCompleto = je.id_jugadorNavigation != null
                    ? $"{je.id_jugadorNavigation.nombres} {je.id_jugadorNavigation.apellidos}"
                    : "",
                FotoPerfil = je.id_jugadorNavigation?.foto_perfil_url,
                NumeroCamiseta = je.numero_camiseta,
                Posicion = je.posicion,
                EsCapitan = je.es_capitan ?? false,
                Estado = je.estado ?? "ACTIVO",
                FechaInicio = je.fecha_inicio ?? DateTime.UtcNow,
                FechaFin = je.fecha_fin
            };
        }

        private SuspensionResponse MapearSuspensionResponse(jugadores_suspensione suspension)
        {
            return new SuspensionResponse
            {
                Id = suspension.id,
                Codigo = suspension.codigo,
                IdJugador = suspension.id_jugador,
                Jugador = suspension.id_jugadorNavigation != null
                    ? $"{suspension.id_jugadorNavigation.nombres} {suspension.id_jugadorNavigation.apellidos}"
                    : "",
                IdEquipo = suspension.id_equipo,
                Equipo = suspension.id_equipoNavigation?.nombre ?? "",
                IdTorneo = suspension.id_torneo,
                Torneo = suspension.id_torneoNavigation?.nombre ?? "",
                Motivo = suspension.motivo,
                PartidosSuspension = suspension.partidos_suspension,
                PartidosCumplidos = suspension.partidos_cumplidos ?? 0,
                FechaInicio = suspension.fecha_inicio.ToDateTime(TimeOnly.MinValue),
                FechaFinEstimada = suspension.fecha_fin_estimada?.ToDateTime(TimeOnly.MinValue),
                Estado = suspension.estado ?? "ACTIVA",
                GeneradaAutomaticamente = suspension.generada_automaticamente ?? false,
                ReglaAplicada = suspension.regla_aplicada,
                FechaRegistro = suspension.fecha_registro ?? DateTime.UtcNow
            };
        }

        #endregion

        #region ========== CRUD PRINCIPAL ==========

        public async Task<ResultadoPaginado<EquipoResponse>> ObtenerTodosAsync(EquipoFilterRequest solicitud)
        {
            var query = _contexto.equipos
                .Include(e => e.id_capitanNavigation)
                .Where(e => e.activo == true)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(solicitud.Buscar))
            {
                query = query.Where(e =>
                    e.nombre.Contains(solicitud.Buscar) ||
                    (e.nombre_corto != null && e.nombre_corto.Contains(solicitud.Buscar)) ||
                    (e.ciudad != null && e.ciudad.Contains(solicitud.Buscar)));
            }

            if (!string.IsNullOrWhiteSpace(solicitud.Ciudad))
                query = query.Where(e => e.ciudad == solicitud.Ciudad);

            if (!string.IsNullOrWhiteSpace(solicitud.Estado))
                query = query.Where(e => e.estado == solicitud.Estado);

            if (solicitud.Verificado.HasValue)
                query = query.Where(e => e.verificado == solicitud.Verificado.Value);

            if (solicitud.IdTorneo.HasValue)
            {
                query = query.Where(e => _contexto.equipos_torneos
                    .Any(et => et.id_equipo == e.id && et.id_torneo == solicitud.IdTorneo.Value));
            }

            if (solicitud.IdJugador.HasValue)
            {
                query = query.Where(e => _contexto.jugadores_equipos
                    .Any(je => je.id_equipo == e.id && je.id_jugador == solicitud.IdJugador.Value && je.activo == true));
            }

            var totalItems = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(solicitud.OrdenarPor))
            {
                query = solicitud.OrdenDescendente
                    ? query.OrderByDescending(e => EF.Property<object>(e, solicitud.OrdenarPor))
                    : query.OrderBy(e => EF.Property<object>(e, solicitud.OrdenarPor));
            }
            else
            {
                query = query.OrderByDescending(e => e.fecha_creacion);
            }

            var equipos = await query
                .Skip((solicitud.Pagina - 1) * solicitud.TamanoPagina)
                .Take(solicitud.TamanoPagina)
                .ToListAsync();

            var items = new List<EquipoResponse>();
            foreach (var equipo in equipos)
            {
                var response = MapearEquipoResponse(equipo);
                response.TotalJugadores = await _contexto.jugadores_equipos
                    .CountAsync(je => je.id_equipo == equipo.id && je.activo == true);
                items.Add(response);
            }

            return ResultadoPaginado<EquipoResponse>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
        }

        public async Task<EquipoDetalleResponse?> ObtenerPorIdAsync(int id)
        {
            var equipo = await _contexto.equipos
                .Include(e => e.id_capitanNavigation)
                .Include(e => e.id_creadorNavigation)
                .FirstOrDefaultAsync(e => e.id == id && e.activo == true);

            if (equipo == null)
                return null;

            var response = MapearEquipoDetalleResponse(equipo);

            // Obtener jugadores
            var jugadoresEquipo = await _contexto.jugadores_equipos
                .Include(je => je.id_jugadorNavigation)
                .Where(je => je.id_equipo == id && je.activo == true)
                .ToListAsync();

            foreach (var je in jugadoresEquipo)
            {
                response.Jugadores.Add(MapearJugadorEquipoResponse(je));
            }

            // Obtener torneos
            var torneosEquipo = await _contexto.equipos_torneos
                .Include(et => et.id_torneoNavigation)
                .Include(et => et.id_torneo_faseNavigation)
                .Where(et => et.id_equipo == id && et.activo == true)
                .ToListAsync();

            foreach (var et in torneosEquipo)
            {
                response.Torneos.Add(MapearEquipoTorneoResponse(et));
            }

            response.TotalJugadores = response.Jugadores.Count;

            return response;
        }

        public async Task<EquipoResponse> CrearAsync(int usuarioId, CrearEquipoRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var equipoExistente = await _contexto.equipos
                .FirstOrDefaultAsync(e => e.nombre == solicitud.Nombre && e.activo == true);

            if (equipoExistente != null)
                throw new InvalidOperationException("Ya existe un equipo con ese nombre");

            var equipo = new equipo
            {
                codigo = CodigoHelper.GenerarCodigoEquipo(solicitud.Nombre),
                nombre = solicitud.Nombre,
                nombre_corto = solicitud.NombreCorto,
                color_primario = solicitud.ColorPrimario,
                color_secundario = solicitud.ColorSecundario,
                fecha_fundacion = solicitud.FechaFundacion.HasValue ? DateOnly.FromDateTime(solicitud.FechaFundacion.Value) : null,
                ciudad = solicitud.Ciudad,
                pais = solicitud.Pais,
                estadio_habitual = solicitud.EstadioHabitual,
                email = solicitud.Email,
                telefono = solicitud.Telefono,
                sitio_web = solicitud.SitioWeb,
                id_creador = usuarioId,
                estado = "ACTIVO",
                verificado = false,
                fecha_creacion = DateTime.UtcNow,
                activo = true
            };

            _contexto.equipos.Add(equipo);
            await _contexto.SaveChangesAsync();

            // Asignar al creador como capitán por defecto
            var rolJugadorId = await ObtenerIdRolPorCodigoAsync("JUGADOR");

            var jugadorEquipo = new jugadores_equipo
            {
                codigo = CodigoHelper.GenerarCodigo("JE", 8),
                id_jugador = usuarioId,
                id_equipo = equipo.id,
                es_capitan = true,
                estado = "ACTIVO",
                fecha_inicio = DateTime.UtcNow,
                activo = true
            };

            _contexto.jugadores_equipos.Add(jugadorEquipo);

            // Asignar rol de jugador al usuario si no lo tiene
            var tieneRolJugador = await _contexto.usuarios_roles
                .AnyAsync(ur => ur.id_usuario == usuarioId && ur.id_rol == rolJugadorId && ur.estado == "ACTIVO");

            if (!tieneRolJugador)
            {
                var usuarioRol = new usuarios_role
                {
                    codigo = CodigoHelper.GenerarCodigo("UR", 8),
                    id_usuario = usuarioId,
                    id_rol = rolJugadorId,
                    fecha_inicio = DateOnly.FromDateTime(DateTime.UtcNow),
                    estado = "ACTIVO",
                    origen_asignacion = "MANUAL",
                    activo = true
                };
                _contexto.usuarios_roles.Add(usuarioRol);
            }

            await _contexto.SaveChangesAsync();

            equipo.id_capitan = usuarioId;
            await _contexto.SaveChangesAsync();

            var response = MapearEquipoResponse(equipo);
            response.Capitan = $"{equipo.id_capitanNavigation?.nombres} {equipo.id_capitanNavigation?.apellidos}";
            response.TotalJugadores = 1;

            return response;
        }

        public async Task<EquipoResponse> ActualizarAsync(int id, int usuarioId, ActualizarEquipoRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var equipo = await _contexto.equipos.FindAsync(id);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = equipo.id_capitan == usuarioId;

            if (!esAdmin && !esCapitan)
                throw new UnauthorizedAccessException("No tiene permisos para modificar este equipo");

            if (!string.IsNullOrWhiteSpace(solicitud.Nombre) && solicitud.Nombre != equipo.nombre)
            {
                var nombreExistente = await _contexto.equipos
                    .AnyAsync(e => e.nombre == solicitud.Nombre && e.id != id && e.activo == true);
                if (nombreExistente)
                    throw new InvalidOperationException("Ya existe un equipo con ese nombre");
            }

            if (!string.IsNullOrWhiteSpace(solicitud.Nombre))
                equipo.nombre = solicitud.Nombre;

            if (!string.IsNullOrWhiteSpace(solicitud.NombreCorto))
                equipo.nombre_corto = solicitud.NombreCorto;

            if (!string.IsNullOrWhiteSpace(solicitud.ColorPrimario))
                equipo.color_primario = solicitud.ColorPrimario;

            if (!string.IsNullOrWhiteSpace(solicitud.ColorSecundario))
                equipo.color_secundario = solicitud.ColorSecundario;

            if (solicitud.FechaFundacion.HasValue)
                equipo.fecha_fundacion = DateOnly.FromDateTime(solicitud.FechaFundacion.Value);

            if (!string.IsNullOrWhiteSpace(solicitud.Ciudad))
                equipo.ciudad = solicitud.Ciudad;

            if (!string.IsNullOrWhiteSpace(solicitud.Pais))
                equipo.pais = solicitud.Pais;

            if (!string.IsNullOrWhiteSpace(solicitud.EstadioHabitual))
                equipo.estadio_habitual = solicitud.EstadioHabitual;

            if (!string.IsNullOrWhiteSpace(solicitud.Email))
                equipo.email = solicitud.Email;

            if (!string.IsNullOrWhiteSpace(solicitud.Telefono))
                equipo.telefono = solicitud.Telefono;

            if (!string.IsNullOrWhiteSpace(solicitud.SitioWeb))
                equipo.sitio_web = solicitud.SitioWeb;

            equipo.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            var response = MapearEquipoResponse(equipo);
            response.TotalJugadores = await _contexto.jugadores_equipos
                .CountAsync(je => je.id_equipo == id && je.activo == true);

            return response;
        }

        #endregion

        #region ========== GESTIÓN DE ESCUDOS ==========

        public async Task<string> SubirEscudoAsync(int id, int usuarioId, IFormFile archivo, string? ipAddress = null, string? userAgent = null)
        {
            var equipo = await _contexto.equipos.FindAsync(id);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = equipo.id_capitan == usuarioId;

            if (!esAdmin && !esCapitan)
                throw new UnauthorizedAccessException("No tiene permisos para modificar este equipo");

            var rutaRelativa = await _archivosHelper.GuardarImagenAsync(archivo, "escudos", 5);

            equipo.escudo_url = rutaRelativa;
            equipo.fecha_modificacion = DateTime.UtcNow;
            await _contexto.SaveChangesAsync();

            var baseUrl = _configuracion["AppUrl"] ?? "https://localhost:7247";
            return _archivosHelper.ObtenerUrlArchivo(rutaRelativa, baseUrl);
        }

        public async Task<string> ActualizarEscudoAsync(int id, int usuarioId, IFormFile archivo, string? ipAddress = null, string? userAgent = null)
        {
            var equipo = await _contexto.equipos.FindAsync(id);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = equipo.id_capitan == usuarioId;

            if (!esAdmin && !esCapitan)
                throw new UnauthorizedAccessException("No tiene permisos para modificar este equipo");

            if (!string.IsNullOrEmpty(equipo.escudo_url))
            {
                var rutaAnterior = equipo.escudo_url.Replace("/uploads/", "");
                _archivosHelper.EliminarArchivo(rutaAnterior);
            }

            var rutaRelativa = await _archivosHelper.GuardarImagenAsync(archivo, "escudos", 5);

            equipo.escudo_url = rutaRelativa;
            equipo.fecha_modificacion = DateTime.UtcNow;
            await _contexto.SaveChangesAsync();

            var baseUrl = _configuracion["AppUrl"] ?? "https://localhost:7247";
            return _archivosHelper.ObtenerUrlArchivo(rutaRelativa, baseUrl);
        }

        public async Task EliminarEscudoAsync(int id, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var equipo = await _contexto.equipos.FindAsync(id);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = equipo.id_capitan == usuarioId;

            if (!esAdmin && !esCapitan)
                throw new UnauthorizedAccessException("No tiene permisos para modificar este equipo");

            if (!string.IsNullOrEmpty(equipo.escudo_url))
            {
                var rutaAnterior = equipo.escudo_url.Replace("/uploads/", "");
                _archivosHelper.EliminarArchivo(rutaAnterior);
                equipo.escudo_url = null;
                equipo.fecha_modificacion = DateTime.UtcNow;
                await _contexto.SaveChangesAsync();
            }
        }

        #endregion

        #region ========== TORNEOS ==========

        public async Task<EquipoTorneoResponse> InscribirEnTorneoAsync(int equipoId, int torneoId, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var torneo = await _contexto.torneos.FindAsync(torneoId);
            if (torneo == null)
                throw new KeyNotFoundException("Torneo no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = equipo.id_capitan == usuarioId;

            if (!esAdmin && !esCapitan)
                throw new UnauthorizedAccessException("No tiene permisos para inscribir este equipo");

            var inscripcionExistente = await _contexto.equipos_torneos
                .FirstOrDefaultAsync(et => et.id_equipo == equipoId && et.id_torneo == torneoId);

            if (inscripcionExistente != null)
                throw new InvalidOperationException("El equipo ya está inscrito en este torneo");

            var equiposInscritos = await _contexto.equipos_torneos
                .CountAsync(et => et.id_torneo == torneoId && et.estado != "RECHAZADO");

            if (torneo.equipos_maximos.HasValue && equiposInscritos >= torneo.equipos_maximos.Value)
                throw new InvalidOperationException("El torneo ha alcanzado el límite máximo de equipos");

            var inscripcion = new equipos_torneo
            {
                codigo = CodigoHelper.GenerarCodigo("ET", 8),
                id_equipo = equipoId,
                id_torneo = torneoId,
                fecha_inscripcion = DateTime.UtcNow,
                aprobado = !torneo.requiere_aprobacion_equipos,
                estado = torneo.requiere_aprobacion_equipos.GetValueOrDefault() ? "PENDIENTE" : "APROBADO",
                activo = true
            };

            _contexto.equipos_torneos.Add(inscripcion);
            await _contexto.SaveChangesAsync();

            if (equipo.id_capitan.HasValue)
            {
                await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
                {
                    IdUsuarioDestino = equipo.id_capitan.Value,
                    IdTipoNotificacion = 5,
                    Titulo = "Inscripción a torneo",
                    Mensaje = $"Tu equipo {equipo.nombre} ha sido inscrito en el torneo {torneo.nombre}",
                    IdTorneo = torneoId,
                    IdEquipo = equipoId,
                    Prioridad = "MEDIA"
                });
            }

            var response = MapearEquipoTorneoResponse(inscripcion);
            response.Torneo = torneo.nombre;
            response.TorneoLogo = torneo.logo_url;

            return response;
        }

        public async Task AprobarInscripcionAsync(int equipoId, int torneoId, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("Solo los administradores pueden aprobar inscripciones");

            var inscripcion = await _contexto.equipos_torneos
                .Include(et => et.id_equipoNavigation)
                .FirstOrDefaultAsync(et => et.id_equipo == equipoId && et.id_torneo == torneoId);

            if (inscripcion == null)
                throw new KeyNotFoundException("Inscripción no encontrada");

            var torneo = await _contexto.torneos.FindAsync(torneoId);

            inscripcion.aprobado = true;
            inscripcion.estado = "APROBADO";
            inscripcion.fecha_aprobacion = DateTime.UtcNow;
            inscripcion.id_usuario_aprobacion = usuarioId;

            await _contexto.SaveChangesAsync();

            if (inscripcion.id_equipoNavigation?.id_capitan.HasValue == true)
            {
                await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
                {
                    IdUsuarioDestino = inscripcion.id_equipoNavigation.id_capitan.Value,
                    IdTipoNotificacion = 7,
                    Titulo = "Inscripción aprobada",
                    Mensaje = $"La inscripción de tu equipo {inscripcion.id_equipoNavigation.nombre} en el torneo {torneo?.nombre} ha sido aprobada",
                    IdTorneo = torneoId,
                    IdEquipo = equipoId,
                    Prioridad = "ALTA"
                });
            }
        }

        public async Task<List<EquipoResponse>> ObtenerEquiposPorTorneoAsync(int torneoId)
        {
            var equipos = await _contexto.equipos_torneos
                .Include(et => et.id_equipoNavigation)
                    .ThenInclude(e => e.id_capitanNavigation)
                .Where(et => et.id_torneo == torneoId && et.estado == "APROBADO" && et.activo == true)
                .Select(et => et.id_equipoNavigation)
                .Where(e => e != null && e.activo == true)
                .ToListAsync();

            var response = new List<EquipoResponse>();
            foreach (var equipo in equipos!)
            {
                var equipoResponse = MapearEquipoResponse(equipo);
                equipoResponse.TotalJugadores = await _contexto.jugadores_equipos
                    .CountAsync(je => je.id_equipo == equipo.id && je.activo == true);
                response.Add(equipoResponse);
            }

            return response;
        }

        #endregion

        #region ========== JUGADORES DEL EQUIPO ==========

        public async Task<List<JugadorEquipoResponse>> ObtenerJugadoresAsync(int equipoId)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var jugadoresEquipo = await _contexto.jugadores_equipos
                .Include(je => je.id_jugadorNavigation)
                .Where(je => je.id_equipo == equipoId && je.activo == true)
                .ToListAsync();

            var response = new List<JugadorEquipoResponse>();
            foreach (var je in jugadoresEquipo)
            {
                response.Add(MapearJugadorEquipoResponse(je));
            }

            return response;
        }

        public async Task<JugadorEquipoResponse> AgregarJugadorAsync(int equipoId, int usuarioId, AgregarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = equipo.id_capitan == usuarioId;

            if (!esAdmin && !esCapitan)
                throw new UnauthorizedAccessException("No tiene permisos para agregar jugadores a este equipo");

            var jugador = await _contexto.usuarios.FindAsync(solicitud.IdJugador);
            if (jugador == null)
                throw new KeyNotFoundException("Jugador no encontrado");

            var pertenece = await _contexto.jugadores_equipos
                .AnyAsync(je => je.id_equipo == equipoId && je.id_jugador == solicitud.IdJugador && je.activo == true);

            if (pertenece)
                throw new InvalidOperationException("El jugador ya pertenece a este equipo");

            var jugadorEquipo = new jugadores_equipo
            {
                codigo = CodigoHelper.GenerarCodigo("JE", 8),
                id_jugador = solicitud.IdJugador,
                id_equipo = equipoId,
                numero_camiseta = solicitud.NumeroCamiseta,
                posicion = solicitud.Posicion,
                es_capitan = solicitud.EsCapitan,
                estado = "ACTIVO",
                fecha_inicio = DateTime.UtcNow,
                activo = true
            };

            _contexto.jugadores_equipos.Add(jugadorEquipo);

            if (solicitud.EsCapitan)
            {
                equipo.id_capitan = solicitud.IdJugador;
            }

            await _contexto.SaveChangesAsync();

            await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = solicitud.IdJugador,
                IdTipoNotificacion = 6,
                Titulo = "Agregado a equipo",
                Mensaje = $"Has sido agregado al equipo {equipo.nombre}",
                IdEquipo = equipoId,
                Prioridad = "MEDIA"
            });

            await _emailService.EnviarNotificacionEmailAsync(
                jugador.email,
                "Has sido agregado a un equipo - TorneoPro",
                $"<h1>¡Bienvenido a {equipo.nombre}!</h1><p>Has sido agregado como jugador del equipo.</p>",
                jugador.nombres);

            var response = MapearJugadorEquipoResponse(jugadorEquipo);
            return response;
        }

        public async Task<JugadorEquipoResponse> ActualizarJugadorAsync(int equipoId, int jugadorId, int usuarioId, ActualizarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = equipo.id_capitan == usuarioId;

            if (!esAdmin && !esCapitan)
                throw new UnauthorizedAccessException("No tiene permisos para modificar jugadores de este equipo");

            var jugadorEquipo = await _contexto.jugadores_equipos
                .Include(je => je.id_jugadorNavigation)
                .FirstOrDefaultAsync(je => je.id_equipo == equipoId && je.id_jugador == jugadorId && je.activo == true);

            if (jugadorEquipo == null)
                throw new KeyNotFoundException("Jugador no encontrado en este equipo");

            if (solicitud.NumeroCamiseta.HasValue)
                jugadorEquipo.numero_camiseta = solicitud.NumeroCamiseta.Value;

            if (!string.IsNullOrWhiteSpace(solicitud.Posicion))
                jugadorEquipo.posicion = solicitud.Posicion;

            if (solicitud.EsCapitan.HasValue && solicitud.EsCapitan.Value != jugadorEquipo.es_capitan)
            {
                if (solicitud.EsCapitan.Value)
                {
                    var capitanActual = await _contexto.jugadores_equipos
                        .FirstOrDefaultAsync(je => je.id_equipo == equipoId && je.es_capitan == true);
                    if (capitanActual != null)
                        capitanActual.es_capitan = false;

                    jugadorEquipo.es_capitan = true;
                    equipo.id_capitan = jugadorId;
                }
                else
                {
                    jugadorEquipo.es_capitan = false;
                    equipo.id_capitan = null;
                }
            }

            await _contexto.SaveChangesAsync();

            return MapearJugadorEquipoResponse(jugadorEquipo);
        }

        public async Task RemoverJugadorAsync(int equipoId, int jugadorId, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = equipo.id_capitan == usuarioId;

            if (!esAdmin && !esCapitan)
                throw new UnauthorizedAccessException("No tiene permisos para remover jugadores de este equipo");

            var jugadorEquipo = await _contexto.jugadores_equipos
                .Include(je => je.id_jugadorNavigation)
                .FirstOrDefaultAsync(je => je.id_equipo == equipoId && je.id_jugador == jugadorId && je.activo == true);

            if (jugadorEquipo == null)
                throw new KeyNotFoundException("Jugador no encontrado en este equipo");

            jugadorEquipo.activo = false;
            jugadorEquipo.fecha_fin = DateTime.UtcNow;
            jugadorEquipo.estado = "RETIRADO";

            if (jugadorEquipo.es_capitan.GetValueOrDefault())
            {
                var nuevoCapitan = await _contexto.jugadores_equipos
                    .FirstOrDefaultAsync(je => je.id_equipo == equipoId && je.activo == true && je.id_jugador != jugadorId);

                if (nuevoCapitan != null)
                {
                    nuevoCapitan.es_capitan = true;
                    equipo.id_capitan = nuevoCapitan.id_jugador;
                }
                else
                {
                    equipo.id_capitan = null;
                }
            }

            await _contexto.SaveChangesAsync();

            await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = jugadorId,
                IdTipoNotificacion = 17,
                Titulo = "Removido de equipo",
                Mensaje = $"Has sido removido del equipo {equipo.nombre}",
                IdEquipo = equipoId,
                Prioridad = "MEDIA"
            });
        }

        public async Task CambiarCapitanAsync(int equipoId, int nuevoCapitanId, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = equipo.id_capitan == usuarioId;

            if (!esAdmin && !esCapitan)
                throw new UnauthorizedAccessException("No tiene permisos para cambiar el capitán");

            var nuevoCapitan = await _contexto.jugadores_equipos
                .FirstOrDefaultAsync(je => je.id_equipo == equipoId && je.id_jugador == nuevoCapitanId && je.activo == true);

            if (nuevoCapitan == null)
                throw new KeyNotFoundException("El jugador no pertenece a este equipo");

            var capitanActual = await _contexto.jugadores_equipos
                .FirstOrDefaultAsync(je => je.id_equipo == equipoId && je.es_capitan == true);
            if (capitanActual != null)
                capitanActual.es_capitan = false;

            nuevoCapitan.es_capitan = true;
            equipo.id_capitan = nuevoCapitanId;

            await _contexto.SaveChangesAsync();

            await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = nuevoCapitanId,
                IdTipoNotificacion = 6,
                Titulo = "Eres el nuevo capitán",
                Mensaje = $"Has sido designado como capitán del equipo {equipo.nombre}.",
                IdEquipo = equipoId,
                Prioridad = "ALTA"
            });
        }

        public async Task AutorizarJugadorAsync(int equipoId, int jugadorId, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("Solo los administradores pueden autorizar jugadores");

            var jugadorEquipo = await _contexto.jugadores_equipos
                .Include(je => je.id_jugadorNavigation)
                .FirstOrDefaultAsync(je => je.id_equipo == equipoId && je.id_jugador == jugadorId && je.activo == true);

            if (jugadorEquipo == null)
                throw new KeyNotFoundException("Jugador no encontrado en este equipo");

            jugadorEquipo.estado = "AUTORIZADO";
            await _contexto.SaveChangesAsync();
        }

        #endregion

        #region ========== SUSPENSIONES ==========

        public async Task<SuspensionResponse> SuspenderJugadorAsync(int equipoId, int jugadorId, int usuarioId, DTOs.Equipos.Request.SuspenderJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("Solo los administradores pueden suspender jugadores");

            var jugadorEquipo = await _contexto.jugadores_equipos
                .Include(je => je.id_jugadorNavigation)
                .Include(je => je.id_equipoNavigation)
                .FirstOrDefaultAsync(je => je.id_equipo == equipoId && je.id_jugador == jugadorId && je.activo == true);

            if (jugadorEquipo == null)
                throw new KeyNotFoundException("Jugador no encontrado en este equipo");

            var suspension = new jugadores_suspensione
            {
                codigo = CodigoHelper.GenerarCodigoSuspension(solicitud.IdTorneo, jugadorId, DateTime.UtcNow),
                id_jugador = jugadorId,
                id_equipo = equipoId,
                id_torneo = solicitud.IdTorneo,
                id_partido_origen = solicitud.IdPartidoOrigen,
                id_evento_origen = solicitud.IdEventoOrigen,
                motivo = solicitud.Motivo,
                partidos_suspension = solicitud.PartidosSuspension,
                fecha_inicio = DateOnly.FromDateTime(solicitud.FechaInicio),
                fecha_fin_estimada = DateOnly.FromDateTime(solicitud.FechaInicio.AddDays(solicitud.PartidosSuspension * 7)),
                estado = "ACTIVA",
                generada_automaticamente = false,
                fecha_registro = DateTime.UtcNow
            };

            _contexto.jugadores_suspensiones.Add(suspension);
            jugadorEquipo.estado = "SUSPENDIDO";
            await _contexto.SaveChangesAsync();

            await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = jugadorId,
                IdTipoNotificacion = 14,
                Titulo = "Suspensión de jugador",
                Mensaje = $"Has sido suspendido por {solicitud.PartidosSuspension} partidos. Motivo: {solicitud.Motivo}",
                IdTorneo = solicitud.IdTorneo,
                IdEquipo = equipoId,
                Prioridad = "ALTA"
            });

            var response = MapearSuspensionResponse(suspension);
            return response;
        }

        #endregion

        #region ========== HISTORIAL Y ESTADÍSTICAS ==========

        public async Task<List<HistorialTorneoResponse>> ObtenerHistorialAsync(int equipoId)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var historial = await _contexto.equipos_torneos
                .Include(et => et.id_torneoNavigation)
                    .ThenInclude(t => t.id_formatoNavigation)
                .Where(et => et.id_equipo == equipoId && et.estado == "APROBADO" && et.activo == true)
                .Select(et => new HistorialTorneoResponse
                {
                    IdTorneo = et.id_torneo,
                    Torneo = et.id_torneoNavigation!.nombre,
                    FechaInicio = et.id_torneoNavigation.fecha_inicio.ToDateTime(TimeOnly.MinValue),
                    FechaFin = et.id_torneoNavigation.fecha_fin.HasValue
                        ? et.id_torneoNavigation.fecha_fin.Value.ToDateTime(TimeOnly.MinValue)
                        : (DateTime?)null,
                    Formato = et.id_torneoNavigation.id_formatoNavigation != null ? et.id_torneoNavigation.id_formatoNavigation.nombre : "",
                    PartidosJugados = 0,
                    PartidosGanados = 0,
                    PartidosEmpatados = 0,
                    PartidosPerdidos = 0,
                    GolesFavor = 0,
                    GolesContra = 0,
                    DiferenciaGoles = 0,
                    Puntos = 0,
                    PosicionFinal = null,
                    Resultado = et.id_torneoNavigation.estado == "FINALIZADO" ? "PARTICIPO" : "EN_CURSO"
                })
                .ToListAsync();

            return historial;
        }

        public async Task<EquipoEstadisticasResponse> ObtenerEstadisticasAsync(int equipoId, int? idTorneo = null)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var queryEstadisticas = _contexto.estadisticas_equipos_torneos
                .Where(e => e.id_equipo == equipoId);

            if (idTorneo.HasValue)
                queryEstadisticas = queryEstadisticas.Where(e => e.id_torneo == idTorneo.Value);

            var estadisticas = await queryEstadisticas.ToListAsync();

            return new EquipoEstadisticasResponse
            {
                IdEquipo = equipo.id,
                Equipo = equipo.nombre,
                TotalPartidos = estadisticas.Sum(e => e.partidos_jugados ?? 0),
                PartidosGanados = estadisticas.Sum(e => e.partidos_ganados ?? 0),
                PartidosEmpatados = estadisticas.Sum(e => e.partidos_empatados ?? 0),
                PartidosPerdidos = estadisticas.Sum(e => e.partidos_perdidos ?? 0),
                GolesFavor = estadisticas.Sum(e => e.goles_favor ?? 0),
                GolesContra = estadisticas.Sum(e => e.goles_contra ?? 0),
                DiferenciaGoles = estadisticas.Sum(e => (e.goles_favor ?? 0) - (e.goles_contra ?? 0)),
                TotalTarjetasAmarillas = estadisticas.Sum(e => e.tarjetas_amarillas ?? 0),
                TotalTarjetasRojas = estadisticas.Sum(e => e.tarjetas_rojas ?? 0),
                PromedioGolesFavor = estadisticas.Sum(e => e.partidos_jugados ?? 0) > 0
                    ? (double)(estadisticas.Sum(e => e.goles_favor ?? 0) / estadisticas.Sum(e => e.partidos_jugados ?? 0))
                    : 0,
                PromedioGolesContra = estadisticas.Sum(e => e.partidos_jugados ?? 0) > 0
                    ? (double)(estadisticas.Sum(e => e.goles_contra ?? 0) / estadisticas.Sum(e => e.partidos_jugados ?? 0))
                    : 0
            };
        }

        public async Task<List<PartidoEquipoResponse>> ObtenerCalendarioAsync(int equipoId, int? idTorneo = null, int limite = 10)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var query = _contexto.partidos
                .Include(p => p.id_torneoNavigation)
                .Include(p => p.id_canchaNavigation)
                .Where(p => (p.id_equipo_local == equipoId || p.id_equipo_visitante == equipoId) &&
                            p.fecha_hora >= DateTime.UtcNow &&
                            p.estado != "CANCELADO");

            if (idTorneo.HasValue)
                query = query.Where(p => p.id_torneo == idTorneo.Value);

            var partidos = await query
                .OrderBy(p => p.fecha_hora)
                .Take(limite)
                .ToListAsync();

            var response = new List<PartidoEquipoResponse>();
            foreach (var partido in partidos)
            {
                var esLocal = partido.id_equipo_local == equipoId;
                response.Add(new PartidoEquipoResponse
                {
                    IdPartido = partido.id,
                    IdTorneo = partido.id_torneo,
                    Torneo = partido.id_torneoNavigation?.nombre ?? "",
                    Rival = esLocal
                        ? partido.id_equipo_visitanteNavigation?.nombre ?? ""
                        : partido.id_equipo_localNavigation?.nombre ?? "",
                    EsLocal = esLocal,
                    Cancha = partido.id_canchaNavigation?.nombre ?? "",
                    FechaHora = partido.fecha_hora,
                    Estado = partido.estado ?? "PROGRAMADO",
                    GolesEquipo = esLocal ? partido.goles_local : partido.goles_visitante,
                    GolesRival = esLocal ? partido.goles_visitante : partido.goles_local
                });
            }

            return response;
        }

        #endregion

        #region ========== SOLICITUDES DE UNIÓN ==========

        public async Task<SolicitudUnionResponse> SolicitarUnionAsync(int equipoId, int usuarioId, string? mensaje = null, string? ipAddress = null, string? userAgent = null)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var yaPertenece = await _contexto.jugadores_equipos
                .AnyAsync(je => je.id_jugador == usuarioId && je.id_equipo == equipoId && je.activo == true);

            if (yaPertenece)
                throw new InvalidOperationException("Ya perteneces a este equipo");

            var solicitudPendiente = await _contexto.solicitudes_equipos
                .AnyAsync(s => s.id_jugador == usuarioId && s.id_equipo == equipoId && s.estado == "PENDIENTE");

            if (solicitudPendiente)
                throw new InvalidOperationException("Ya tienes una solicitud pendiente para este equipo");

            var solicitud = new solicitudes_equipo
            {
                codigo = CodigoHelper.GenerarCodigo("SE", 8),
                id_equipo = equipoId,
                id_jugador = usuarioId,
                mensaje = mensaje,
                estado = "PENDIENTE",
                fecha_solicitud = DateTime.UtcNow,
                activo = true,
                fecha_creacion = DateTime.UtcNow,
                fecha_modificacion = DateTime.UtcNow,
                metadata = "{}"
            };

            _contexto.solicitudes_equipos.Add(solicitud);
            await _contexto.SaveChangesAsync();

            if (equipo.id_capitan.HasValue)
            {
                await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
                {
                    IdUsuarioDestino = equipo.id_capitan.Value,
                    IdTipoNotificacion = 18,
                    Titulo = "Nueva solicitud de unión",
                    Mensaje = $"Un jugador ha solicitado unirse al equipo {equipo.nombre}",
                    IdEquipo = equipoId,
                    Prioridad = "ALTA"
                });
            }

            var usuario = await _contexto.usuarios.FindAsync(usuarioId);

            return new SolicitudUnionResponse
            {
                Id = solicitud.id,
                IdEquipo = equipoId,
                Equipo = equipo.nombre,
                IdJugador = usuarioId,
                Jugador = usuario != null ? $"{usuario.nombres} {usuario.apellidos}" : "",
                Estado = "PENDIENTE",
                FechaSolicitud = solicitud.fecha_solicitud,
                Mensaje = mensaje
            };
        }

        public async Task ProcesarSolicitudUnionAsync(int solicitudId, int usuarioId, bool aprobada, string? comentario = null, string? ipAddress = null, string? userAgent = null)
        {
            var solicitud = await _contexto.solicitudes_equipos
                .Include(s => s.id_equipoNavigation)
                .FirstOrDefaultAsync(s => s.id == solicitudId && s.activo == true);

            if (solicitud == null)
                throw new KeyNotFoundException("Solicitud no encontrada");

            var equipo = solicitud.id_equipoNavigation;
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = equipo.id_capitan == usuarioId;

            if (!esAdmin && !esCapitan)
                throw new UnauthorizedAccessException("No tiene permisos para procesar esta solicitud");

            if (solicitud.estado != "PENDIENTE")
                throw new InvalidOperationException($"La solicitud ya fue {solicitud.estado?.ToLowerInvariant()}");

            solicitud.estado = aprobada ? "APROBADA" : "RECHAZADA";
            solicitud.comentario = comentario;
            solicitud.fecha_procesamiento = DateTime.UtcNow;
            solicitud.id_usuario_procesador = usuarioId;
            solicitud.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            if (aprobada)
            {
                var jugadorEquipo = new jugadores_equipo
                {
                    codigo = CodigoHelper.GenerarCodigo("JE", 8),
                    id_jugador = solicitud.id_jugador,
                    id_equipo = equipo.id,
                    es_capitan = false,
                    estado = "ACTIVO",
                    fecha_inicio = DateTime.UtcNow,
                    activo = true
                };

                _contexto.jugadores_equipos.Add(jugadorEquipo);
                await _contexto.SaveChangesAsync();
            }

            await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = solicitud.id_jugador,
                IdTipoNotificacion = 19,
                Titulo = aprobada ? "Solicitud aprobada" : "Solicitud rechazada",
                Mensaje = aprobada
                    ? $"Tu solicitud para unirte al equipo {equipo.nombre} ha sido aprobada"
                    : $"Tu solicitud para unirte al equipo {equipo.nombre} ha sido rechazada. Motivo: {comentario ?? "No especificado"}",
                IdEquipo = equipo.id,
                Prioridad = "MEDIA"
            });
        }

        public async Task<List<InvitacionEquipoResponse>> ObtenerInvitacionesPendientesAsync(int equipoId, int usuarioId)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = equipo.id_capitan == usuarioId;

            if (!esAdmin && !esCapitan)
                throw new UnauthorizedAccessException("No tiene permisos para ver invitaciones de este equipo");

            var enlaces = await _contexto.enlaces_compartidos
                .Where(e => e.id_equipo == equipoId && e.id_tipo_enlace == 5 && e.estado == "ACTIVO")
                .OrderByDescending(e => e.fecha_creacion)
                .ToListAsync();

            var response = new List<InvitacionEquipoResponse>();
            foreach (var enlace in enlaces)
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(enlace.metadata ?? "{}");
                var emailDestino = metadata?.GetValueOrDefault("email_destino")?.ToString() ?? "";

                response.Add(new InvitacionEquipoResponse
                {
                    Id = enlace.id,
                    Token = enlace.codigo_enlace,
                    EmailDestino = emailDestino,
                    FechaExpiracion = enlace.fecha_expiracion ?? DateTime.UtcNow.AddDays(7),
                    Estado = enlace.estado ?? "ACTIVO",
                    FechaCreacion = enlace.fecha_creacion ?? DateTime.UtcNow,
                    FueUtilizada = (enlace.usos_actuales ?? 0) > 0
                });
            }

            return response;
        }

        #endregion

        #region ========== ESTADÍSTICAS DE JUGADORES ==========

        public async Task<List<JugadorEstadisticaEquipoResponse>> ObtenerEstadisticasJugadoresAsync(int equipoId, int idTorneo)
        {
            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            var jugadoresEquipo = await _contexto.jugadores_equipos
                .Include(je => je.id_jugadorNavigation)
                .Where(je => je.id_equipo == equipoId && je.activo == true)
                .ToListAsync();

            var response = new List<JugadorEstadisticaEquipoResponse>();

            foreach (var je in jugadoresEquipo)
            {
                var estadisticas = await _contexto.estadisticas_jugadores
                    .FirstOrDefaultAsync(ej => ej.id_jugador == je.id_jugador && ej.id_torneo == idTorneo);

                response.Add(new JugadorEstadisticaEquipoResponse
                {
                    IdJugador = je.id_jugador,
                    Jugador = $"{je.id_jugadorNavigation?.nombres} {je.id_jugadorNavigation?.apellidos}",
                    FotoPerfil = je.id_jugadorNavigation?.foto_perfil_url,
                    NumeroCamiseta = je.numero_camiseta ?? 0,
                    Posicion = je.posicion ?? "",
                    EsCapitan = je.es_capitan ?? false,
                    PartidosJugados = estadisticas?.partidos_jugados ?? 0,
                    Goles = estadisticas?.goles ?? 0,
                    Asistencias = estadisticas?.asistencias ?? 0,
                    TarjetasAmarillas = estadisticas?.tarjetas_amarillas ?? 0,
                    TarjetasRojas = estadisticas?.tarjetas_rojas ?? 0,
                    MinutosJugados = 0,
                    PromedioCalificacion = 0
                });
            }

            return response.OrderByDescending(j => j.Goles).ToList();
        }

        public async Task<bool> ValidarNumeroCamisetaAsync(int equipoId, int numero, int? jugadorExcluirId = null)
        {
            var query = _contexto.jugadores_equipos
                .Where(je => je.id_equipo == equipoId && je.numero_camiseta == numero && je.activo == true);

            if (jugadorExcluirId.HasValue)
                query = query.Where(je => je.id_jugador != jugadorExcluirId.Value);

            return !await query.AnyAsync();
        }

        #endregion

        #region ========== ACTIVAR/DESACTIVAR EQUIPO ==========

        public async Task DesactivarEquipoAsync(int equipoId, int usuarioId, string? motivo = null, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para desactivar equipos");

            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            equipo.activo = false;
            equipo.estado = "INACTIVO";
            equipo.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();
        }

        public async Task ActivarEquipoAsync(int equipoId, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para activar equipos");

            var equipo = await _contexto.equipos.FindAsync(equipoId);
            if (equipo == null)
                throw new KeyNotFoundException("Equipo no encontrado");

            equipo.activo = true;
            equipo.estado = "ACTIVO";
            equipo.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();
        }

        #endregion
    }
}