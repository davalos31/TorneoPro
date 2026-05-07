using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.Canchas;
using TorneoPro.API.DTOs.Canchas.Response;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Cancha;
using TorneoPro.API.Servicios.Interfaces.Notificacion;

namespace TorneoPro.API.Servicios.Implementaciones.Cancha
{
    public class CanchaService : ICanchaService
    {
        private readonly TorneoProContext _contexto;
        private readonly ArchivosHelper _archivosHelper;
        private readonly ILogger<CanchaService> _logger;
        private readonly IConfiguration _configuracion;
        private readonly INotificacionService _notificacionService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CanchaService(
            TorneoProContext contexto,
            ArchivosHelper archivosHelper,
            ILogger<CanchaService> logger,
            IConfiguration configuracion,
            INotificacionService notificacionService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contexto = contexto;
            _archivosHelper = archivosHelper;
            _logger = logger;
            _configuracion = configuracion;
            _notificacionService = notificacionService;
            _httpContextAccessor = httpContextAccessor;
        }

        private string ObtenerBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            return $"{request?.Scheme}://{request?.Host}";
        }

        #region CRUD Principal

        public async Task<ResultadoPaginado<CanchaResponse>> ObtenerTodosAsync(FiltrarCanchaRequest solicitud)
        {
            var query = _contexto.canchas
                .Include(c => c.id_tipo_superficieNavigation)
                .Where(c => c.activo == true)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(solicitud.Buscar))
            {
                query = query.Where(c =>
                    c.nombre.Contains(solicitud.Buscar) ||
                    (c.nombre_corto != null && c.nombre_corto.Contains(solicitud.Buscar)) ||
                    (c.ciudad != null && c.ciudad.Contains(solicitud.Buscar)));
            }

            if (solicitud.IdTipoSuperficie.HasValue)
                query = query.Where(c => c.id_tipo_superficie == solicitud.IdTipoSuperficie.Value);

            if (!string.IsNullOrWhiteSpace(solicitud.Ciudad))
                query = query.Where(c => c.ciudad == solicitud.Ciudad);

            if (!string.IsNullOrWhiteSpace(solicitud.Pais))
                query = query.Where(c => c.pais == solicitud.Pais);

            if (!string.IsNullOrWhiteSpace(solicitud.Estado))
                query = query.Where(c => c.estado == solicitud.Estado);

            if (solicitud.TieneIluminacion.HasValue)
                query = query.Where(c => c.tiene_iluminacion == solicitud.TieneIluminacion.Value);

            if (solicitud.TieneVestuarios.HasValue)
                query = query.Where(c => c.tiene_vestuarios == solicitud.TieneVestuarios.Value);

            if (solicitud.TieneEstacionamiento.HasValue)
                query = query.Where(c => c.tiene_estacionamiento == solicitud.TieneEstacionamiento.Value);

            if (solicitud.CapacidadMinima.HasValue)
                query = query.Where(c => c.capacidad_espectadores >= solicitud.CapacidadMinima.Value);

            if (solicitud.SoloActivos == true)
                query = query.Where(c => c.activo == true);

            var totalItems = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(solicitud.OrdenarPor))
            {
                query = solicitud.OrdenDescendente
                    ? query.OrderByDescending(c => EF.Property<object>(c, solicitud.OrdenarPor))
                    : query.OrderBy(c => EF.Property<object>(c, solicitud.OrdenarPor));
            }
            else
            {
                query = query.OrderBy(c => c.nombre);
            }

            var canchas = await query
                .Skip((solicitud.Pagina - 1) * solicitud.TamanoPagina)
                .Take(solicitud.TamanoPagina)
                .ToListAsync();

            var items = canchas.Select(c => MapearACanchaResponse(c)).ToList();

            return ResultadoPaginado<CanchaResponse>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
        }

        public async Task<CanchaDetalleResponse?> ObtenerPorIdAsync(int id)
        {
            var cancha = await _contexto.canchas
                .Include(c => c.id_tipo_superficieNavigation)
                .FirstOrDefaultAsync(c => c.id == id && c.activo == true);

            if (cancha == null)
                return null;

            var response = MapearACanchaDetalleResponse(cancha);

            var hoy = DateTime.UtcNow.Date;
            var finSemana = hoy.AddDays(7);
            response.DisponibilidadSemana = await ObtenerDisponibilidadAsync(id, hoy, finSemana);

            return response;
        }

        public async Task<CanchaResponse> CrearAsync(int usuarioId, CrearCanchaRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para crear canchas");

            // Validar que el tipo de superficie existe
            var tipoSuperficie = await _contexto.tipos_superficies.FindAsync(solicitud.IdTipoSuperficie);
            if (tipoSuperficie == null)
                throw new InvalidOperationException("El tipo de superficie no es válido");

            var existe = await _contexto.canchas
                .AnyAsync(c => c.nombre == solicitud.Nombre && c.activo == true);

            if (existe)
                throw new InvalidOperationException("Ya existe una cancha con ese nombre");

            var cancha = MapearDeCrearCanchaRequest(solicitud);
            cancha.codigo = CodigoHelper.GenerarCodigo("CAN", 8);
            cancha.estado = "DISPONIBLE";
            cancha.activo = true;
            cancha.fecha_creacion = DateTime.UtcNow;

            _contexto.canchas.Add(cancha);
            await _contexto.SaveChangesAsync();

            await _contexto.Entry(cancha)
                .Reference(c => c.id_tipo_superficieNavigation)
                .LoadAsync();

            return MapearACanchaResponse(cancha);
        }

        public async Task<CanchaResponse> ActualizarAsync(int id, int usuarioId, ActualizarCanchaRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para actualizar canchas");

            var cancha = await _contexto.canchas.FindAsync(id);
            if (cancha == null)
                throw new KeyNotFoundException("Cancha no encontrada");

            // Validar tipo de superficie si se está cambiando
            if (solicitud.IdTipoSuperficie.HasValue)
            {
                var tipoSuperficie = await _contexto.tipos_superficies.FindAsync(solicitud.IdTipoSuperficie.Value);
                if (tipoSuperficie == null)
                    throw new InvalidOperationException("El tipo de superficie no es válido");
            }

            if (!string.IsNullOrWhiteSpace(solicitud.Nombre) && solicitud.Nombre != cancha.nombre)
            {
                var existe = await _contexto.canchas
                    .AnyAsync(c => c.nombre == solicitud.Nombre && c.id != id && c.activo == true);
                if (existe)
                    throw new InvalidOperationException("Ya existe una cancha con ese nombre");
            }

            if (!string.IsNullOrWhiteSpace(solicitud.Nombre))
                cancha.nombre = solicitud.Nombre;

            if (!string.IsNullOrWhiteSpace(solicitud.NombreCorto))
                cancha.nombre_corto = solicitud.NombreCorto;

            if (solicitud.IdTipoSuperficie.HasValue)
                cancha.id_tipo_superficie = solicitud.IdTipoSuperficie.Value;

            if (!string.IsNullOrWhiteSpace(solicitud.Direccion))
                cancha.direccion = solicitud.Direccion;

            if (!string.IsNullOrWhiteSpace(solicitud.Ciudad))
                cancha.ciudad = solicitud.Ciudad;

            if (!string.IsNullOrWhiteSpace(solicitud.Pais))
                cancha.pais = solicitud.Pais;

            if (solicitud.Latitud.HasValue)
                cancha.latitud = solicitud.Latitud.Value;

            if (solicitud.Longitud.HasValue)
                cancha.longitud = solicitud.Longitud.Value;

            if (!string.IsNullOrWhiteSpace(solicitud.UrlMapa))
                cancha.url_mapa = solicitud.UrlMapa;

            if (solicitud.CapacidadEspectadores.HasValue)
                cancha.capacidad_espectadores = solicitud.CapacidadEspectadores.Value;

            if (solicitud.TieneIluminacion.HasValue)
                cancha.tiene_iluminacion = solicitud.TieneIluminacion.Value;

            if (solicitud.TieneVestuarios.HasValue)
                cancha.tiene_vestuarios = solicitud.TieneVestuarios.Value;

            if (solicitud.TieneEstacionamiento.HasValue)
                cancha.tiene_estacionamiento = solicitud.TieneEstacionamiento.Value;

            if (!string.IsNullOrWhiteSpace(solicitud.Estado))
                cancha.estado = solicitud.Estado;

            cancha.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            await _contexto.Entry(cancha)
                .Reference(c => c.id_tipo_superficieNavigation)
                .LoadAsync();

            return MapearACanchaResponse(cancha);
        }

        public async Task DesactivarAsync(int id, int usuarioId, string? motivo = null, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para desactivar canchas");

            var cancha = await _contexto.canchas.FindAsync(id);
            if (cancha == null)
                throw new KeyNotFoundException("Cancha no encontrada");

            cancha.activo = false;
            cancha.estado = "INACTIVA";
            cancha.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();
        }

        public async Task ActivarAsync(int id, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para activar canchas");

            var cancha = await _contexto.canchas.FindAsync(id);
            if (cancha == null)
                throw new KeyNotFoundException("Cancha no encontrada");

            cancha.activo = true;
            cancha.estado = "DISPONIBLE";
            cancha.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();
        }

        #endregion

        #region Gestión de Fotos

        public async Task<string> SubirFotoAsync(int id, int usuarioId, IFormFile foto, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para modificar canchas");

            var cancha = await _contexto.canchas.FindAsync(id);
            if (cancha == null)
                throw new KeyNotFoundException("Cancha no encontrada");

            var rutaRelativa = await _archivosHelper.GuardarImagenAsync(foto, "canchas", 5);
            cancha.foto_url = rutaRelativa;
            cancha.fecha_modificacion = DateTime.UtcNow;
            await _contexto.SaveChangesAsync();

            var baseUrl = ObtenerBaseUrl();
            return _archivosHelper.ObtenerUrlArchivo(rutaRelativa, baseUrl);
        }

        public async Task<string> ActualizarFotoAsync(int id, int usuarioId, IFormFile foto, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para modificar canchas");

            var cancha = await _contexto.canchas.FindAsync(id);
            if (cancha == null)
                throw new KeyNotFoundException("Cancha no encontrada");

            if (!string.IsNullOrEmpty(cancha.foto_url))
            {
                var rutaAnterior = cancha.foto_url.Replace("/uploads/", "");
                _archivosHelper.EliminarArchivo(rutaAnterior);
            }

            var rutaRelativa = await _archivosHelper.GuardarImagenAsync(foto, "canchas", 5);
            cancha.foto_url = rutaRelativa;
            cancha.fecha_modificacion = DateTime.UtcNow;
            await _contexto.SaveChangesAsync();

            var baseUrl = ObtenerBaseUrl();
            return _archivosHelper.ObtenerUrlArchivo(rutaRelativa, baseUrl);
        }

        public async Task EliminarFotoAsync(int id, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para modificar canchas");

            var cancha = await _contexto.canchas.FindAsync(id);
            if (cancha == null)
                throw new KeyNotFoundException("Cancha no encontrada");

            if (!string.IsNullOrEmpty(cancha.foto_url))
            {
                var rutaAnterior = cancha.foto_url.Replace("/uploads/", "");
                _archivosHelper.EliminarArchivo(rutaAnterior);
                cancha.foto_url = null;
                cancha.fecha_modificacion = DateTime.UtcNow;
                await _contexto.SaveChangesAsync();
            }
        }

        #endregion

        #region Disponibilidad y Bloqueos

        public async Task<List<DisponibilidadResponse>> ObtenerDisponibilidadAsync(int id, DateTime fechaInicio, DateTime fechaFin)
        {
            var cancha = await _contexto.canchas.FindAsync(id);
            if (cancha == null)
                throw new KeyNotFoundException("Cancha no encontrada");

            var partidos = await _contexto.partidos
                .Where(p => p.id_cancha == id && p.fecha_hora >= fechaInicio && p.fecha_hora <= fechaFin && p.activo == true)
                .Select(p => new DisponibilidadResponse
                {
                    FechaHoraInicio = p.fecha_hora,
                    FechaHoraFin = p.fecha_hora.AddMinutes(120),
                    Disponible = false,
                    IdPartido = p.id,
                    MotivoBloqueo = "Partido programado"
                })
                .ToListAsync();

            var bloqueos = await _contexto.fechas_bloqueadas
                .Where(fb => fb.id_cancha == id && fb.fecha_inicio >= fechaInicio && fb.fecha_inicio <= fechaFin && fb.activo == true)
                .Select(fb => new DisponibilidadResponse
                {
                    FechaHoraInicio = fb.fecha_inicio,
                    FechaHoraFin = fb.fecha_fin,
                    Disponible = false,
                    IdPartido = null,
                    MotivoBloqueo = fb.motivo
                })
                .ToListAsync();

            return partidos.Concat(bloqueos).ToList();
        }

        /// <summary>
        /// Obtiene disponibilidad horaria para un día específico
        /// </summary>
        public async Task<List<DisponibilidadHorariaResponse>> ObtenerDisponibilidadHorariaAsync(int id, DateTime fecha, int duracionMinutos = 60)
        {
            var cancha = await _contexto.canchas.FindAsync(id);
            if (cancha == null)
                throw new KeyNotFoundException("Cancha no encontrada");

            var fechaInicio = fecha.Date;
            var fechaFin = fecha.Date.AddDays(1).AddSeconds(-1);

            // Obtener ocupaciones del día
            var ocupaciones = new List<(DateTime Inicio, DateTime Fin)>();

            var partidos = await _contexto.partidos
                .Where(p => p.id_cancha == id && p.fecha_hora >= fechaInicio && p.fecha_hora <= fechaFin && p.activo == true)
                .Select(p => new { Inicio = p.fecha_hora, Fin = p.fecha_hora.AddMinutes(120) })
                .ToListAsync();

            foreach (var p in partidos)
                ocupaciones.Add((p.Inicio, p.Fin));

            var bloqueos = await _contexto.fechas_bloqueadas
                .Where(fb => fb.id_cancha == id && fb.fecha_inicio <= fechaFin && fb.fecha_fin >= fechaInicio && fb.activo == true)
                .Select(fb => new { Inicio = fb.fecha_inicio, Fin = fb.fecha_fin })
                .ToListAsync();

            foreach (var b in bloqueos)
                ocupaciones.Add((b.Inicio, b.Fin));

            // Generar franjas horarias
            var resultado = new List<DisponibilidadHorariaResponse>();
            var horaActual = fechaInicio.AddHours(8); // Ejemplo: desde 8:00 AM
            var horaFin = fechaInicio.AddHours(22); // Ejemplo: hasta 10:00 PM

            while (horaActual < horaFin)
            {
                var finSlot = horaActual.AddMinutes(duracionMinutos);
                var ocupado = ocupaciones.Any(o => o.Inicio < finSlot && o.Fin > horaActual);

                resultado.Add(new DisponibilidadHorariaResponse
                {
                    HoraInicio = horaActual,
                    HoraFin = finSlot,
                    Disponible = !ocupado,
                    IdPartido = !ocupado ? (int?)null :
                        partidos.FirstOrDefault(p => p.Inicio < finSlot && p.Fin > horaActual)?.Inicio.GetHashCode() ?? 0
                });

                horaActual = finSlot;
            }

            return resultado;
        }

        public async Task<BloqueoResponse> BloquearFechaAsync(int id, int usuarioId, BloquearFechaRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para bloquear fechas");

            var cancha = await _contexto.canchas.FindAsync(id);
            if (cancha == null)
                throw new KeyNotFoundException("Cancha no encontrada");

            if (solicitud.FechaInicio >= solicitud.FechaFin)
                throw new InvalidOperationException("La fecha de inicio debe ser anterior a la fecha de fin");

            if (solicitud.FechaInicio < DateTime.UtcNow)
                throw new InvalidOperationException("No se pueden bloquear fechas pasadas");

            // Validar conflicto con bloqueos existentes
            var bloqueoExistente = await _contexto.fechas_bloqueadas
                .AnyAsync(fb => fb.id_cancha == id &&
                                fb.fecha_inicio < solicitud.FechaFin &&
                                fb.fecha_fin > solicitud.FechaInicio &&
                                fb.activo == true);

            if (bloqueoExistente)
                throw new InvalidOperationException("Ya existe un bloqueo en ese horario");

            // Verificar partidos en ese horario
            if (!solicitud.AplicaATodasCanchas)
            {
                var hayPartido = await _contexto.partidos
                    .AnyAsync(p => p.id_cancha == id &&
                                   p.fecha_hora >= solicitud.FechaInicio &&
                                   p.fecha_hora <= solicitud.FechaFin &&
                                   p.estado != "CANCELADO");

                if (hayPartido)
                    throw new InvalidOperationException("No se puede bloquear la cancha porque hay partidos programados en ese horario");
            }

            var bloqueo = new fechas_bloqueada
            {
                codigo = CodigoHelper.GenerarCodigo("BLQ", 10),
                id_torneo = 0,
                id_cancha = solicitud.AplicaATodasCanchas ? null : id,
                fecha_inicio = solicitud.FechaInicio,
                fecha_fin = solicitud.FechaFin,
                motivo = solicitud.Motivo,
                id_motivo_catalogo = solicitud.IdMotivoCatalogo,
                aplica_a_todas_canchas = solicitud.AplicaATodasCanchas,
                id_usuario_registro = usuarioId,
                fecha_registro = DateTime.UtcNow,
                activo = true
            };

            _contexto.fechas_bloqueadas.Add(bloqueo);
            await _contexto.SaveChangesAsync();

            // Notificar partidos afectados
            if (!solicitud.AplicaATodasCanchas)
            {
                await NotificarPartidosAfectados(id, cancha.nombre, solicitud, usuarioId);
            }

            return new BloqueoResponse
            {
                Id = bloqueo.id,
                Codigo = bloqueo.codigo,
                IdCancha = id,
                Cancha = cancha.nombre,
                FechaInicio = bloqueo.fecha_inicio,
                FechaFin = bloqueo.fecha_fin,
                Motivo = bloqueo.motivo ?? string.Empty,
                AplicaATodasCanchas = bloqueo.aplica_a_todas_canchas ?? false,
                FechaRegistro = bloqueo.fecha_registro ?? DateTime.UtcNow
            };
        }

        private async Task NotificarPartidosAfectados(int canchaId, string nombreCancha, BloquearFechaRequest solicitud, int usuarioId)
        {
            var partidosAfectados = await _contexto.partidos
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .Where(p => p.id_cancha == canchaId &&
                            p.fecha_hora >= solicitud.FechaInicio &&
                            p.fecha_hora <= solicitud.FechaFin &&
                            p.estado == "PROGRAMADO" &&
                            p.activo == true)
                .ToListAsync();

            foreach (var partido in partidosAfectados)
            {
                var mensaje = $"La cancha {nombreCancha} ha sido bloqueada del {solicitud.FechaInicio:dd/MM/yyyy HH:mm} al {solicitud.FechaFin:dd/MM/yyyy HH:mm}. Motivo: {solicitud.Motivo}.";

                if (partido.id_equipo_localNavigation?.id_capitan.HasValue == true)
                {
                    await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
                    {
                        IdUsuarioDestino = partido.id_equipo_localNavigation.id_capitan.Value,
                        IdTipoNotificacion = 9,
                        Titulo = "Cancha bloqueada",
                        Mensaje = mensaje,
                        Prioridad = "ALTA"
                    });
                }

                if (partido.id_equipo_visitanteNavigation?.id_capitan.HasValue == true)
                {
                    await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
                    {
                        IdUsuarioDestino = partido.id_equipo_visitanteNavigation.id_capitan.Value,
                        IdTipoNotificacion = 9,
                        Titulo = "Cancha bloqueada",
                        Mensaje = mensaje,
                        Prioridad = "ALTA"
                    });
                }

                if (partido.id_arbitro_principal.HasValue)
                {
                    await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
                    {
                        IdUsuarioDestino = partido.id_arbitro_principal.Value,
                        IdTipoNotificacion = 9,
                        Titulo = "Cancha bloqueada",
                        Mensaje = mensaje,
                        Prioridad = "ALTA"
                    });
                }
            }
        }

        public async Task EliminarBloqueoAsync(int canchaId, int bloqueoId, int usuarioId, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para eliminar bloqueos");

            var bloqueo = await _contexto.fechas_bloqueadas
                .FirstOrDefaultAsync(fb => fb.id == bloqueoId && (fb.id_cancha == canchaId || fb.id_cancha == null));

            if (bloqueo == null)
                throw new KeyNotFoundException("Bloqueo no encontrado");

            bloqueo.activo = false;
            await _contexto.SaveChangesAsync();
        }

        #endregion

        #region Búsqueda y Catálogos

        /// <summary>
        /// Busca canchas disponibles optimizado (sin N+1 queries)
        /// </summary>
        public async Task<List<CanchaResponse>> BuscarCanchasDisponiblesAsync(BuscarCanchaRequest solicitud)
        {
            // Obtener IDs de canchas ocupadas por partidos en el horario
            var idsOcupadasPorPartidos = await _contexto.partidos
                .Where(p => p.fecha_hora >= solicitud.FechaHoraInicio &&
                            p.fecha_hora <= solicitud.FechaHoraFin &&
                            p.estado != "CANCELADO")
                .Select(p => p.id_cancha)
                .Distinct()
                .ToListAsync();

            // Obtener IDs de canchas ocupadas por bloqueos en el horario
            var idsOcupadasPorBloqueos = await _contexto.fechas_bloqueadas
                .Where(fb => fb.fecha_inicio <= solicitud.FechaHoraFin &&
                             fb.fecha_fin >= solicitud.FechaHoraInicio &&
                             fb.activo == true)
                .Select(fb => fb.id_cancha)
                .Distinct()
                .ToListAsync();

            var idsOcupadas = idsOcupadasPorPartidos
                .Concat(idsOcupadasPorBloqueos)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            // Obtener canchas disponibles (excluyendo ocupadas)
            var query = _contexto.canchas
                .Include(c => c.id_tipo_superficieNavigation)
                .Where(c => c.activo == true && c.estado == "DISPONIBLE")
                .Where(c => !idsOcupadas.Contains(c.id))
                .AsQueryable();

            if (solicitud.IdTipoSuperficie.HasValue)
                query = query.Where(c => c.id_tipo_superficie == solicitud.IdTipoSuperficie.Value);

            if (!string.IsNullOrWhiteSpace(solicitud.Ciudad))
                query = query.Where(c => c.ciudad == solicitud.Ciudad);

            if (solicitud.CapacidadMinima.HasValue)
                query = query.Where(c => c.capacidad_espectadores >= solicitud.CapacidadMinima.Value);

            if (solicitud.TieneIluminacion.HasValue)
                query = query.Where(c => c.tiene_iluminacion == solicitud.TieneIluminacion.Value);

            var canchas = await query.ToListAsync();
            return canchas.Select(c => MapearACanchaResponse(c)).ToList();
        }

        /// <summary>
        /// Obtiene tipos de superficie disponibles
        /// </summary>
        public async Task<List<TipoSuperficieResponse>> ObtenerTiposSuperficieAsync()
        {
            var tipos = await _contexto.tipos_superficies
                .Where(t => t.activo == true)
                .Select(t => new TipoSuperficieResponse
                {
                    Id = t.id,
                    Codigo = t.codigo,
                    Nombre = t.nombre
                })
                .ToListAsync();

            return tipos;
        }

        #endregion

        #region Métodos de Mapeo Manual

        private cancha MapearDeCrearCanchaRequest(CrearCanchaRequest request)
        {
            return new cancha
            {
                nombre = request.Nombre,
                nombre_corto = request.NombreCorto,
                id_tipo_superficie = request.IdTipoSuperficie,
                direccion = request.Direccion,
                ciudad = request.Ciudad,
                pais = request.Pais,
                latitud = request.Latitud ?? 0,
                longitud = request.Longitud ?? 0,
                url_mapa = request.UrlMapa,
                capacidad_espectadores = request.CapacidadEspectadores,
                tiene_iluminacion = request.TieneIluminacion,
                tiene_vestuarios = request.TieneVestuarios,
                tiene_estacionamiento = request.TieneEstacionamiento
            };
        }

        private CanchaResponse MapearACanchaResponse(cancha cancha)
        {
            return new CanchaResponse
            {
                Id = cancha.id,
                Codigo = cancha.codigo,
                Nombre = cancha.nombre,
                NombreCorto = cancha.nombre_corto,
                IdTipoSuperficie = cancha.id_tipo_superficie,
                TipoSuperficie = cancha.id_tipo_superficieNavigation?.nombre ?? "",
                Ciudad = cancha.ciudad,
                Pais = cancha.pais,
                Estado = cancha.estado,
                Activo = cancha.activo ?? false,
                FotoUrl = cancha.foto_url
            };
        }

        private CanchaDetalleResponse MapearACanchaDetalleResponse(cancha cancha)
        {
            return new CanchaDetalleResponse
            {
                Id = cancha.id,
                Codigo = cancha.codigo,
                Nombre = cancha.nombre,
                NombreCorto = cancha.nombre_corto,
                IdTipoSuperficie = cancha.id_tipo_superficie,
                TipoSuperficie = cancha.id_tipo_superficieNavigation?.nombre ?? "",
                Ciudad = cancha.ciudad,
                Pais = cancha.pais,
                Estado = cancha.estado,
                Activo = cancha.activo ?? false,
                FotoUrl = cancha.foto_url,
                Direccion = cancha.direccion,
                Latitud = cancha.latitud,
                Longitud = cancha.longitud,
                UrlMapa = cancha.url_mapa,
                CapacidadEspectadores = cancha.capacidad_espectadores,
                TieneIluminacion = cancha.tiene_iluminacion ?? false,
                TieneVestuarios = cancha.tiene_vestuarios ?? false,
                TieneEstacionamiento = cancha.tiene_estacionamiento ?? false,
                FechaCreacion = cancha.fecha_creacion ?? DateTime.MinValue,
                FechaModificacion = cancha.fecha_modificacion ?? DateTime.MinValue,
                DisponibilidadSemana = new List<DisponibilidadResponse>()
            };
        }

        #endregion

        #region Métodos Privados

        private async Task<bool> EsAdminAsync(int usuarioId)
        {
            return await _contexto.usuarios_roles
                .AnyAsync(ur => ur.id_usuario == usuarioId &&
                               (ur.id_rol == 1 || ur.id_rol == 2) &&
                               ur.estado == "ACTIVO");
        }

        #endregion
    }
}