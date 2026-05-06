using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.Estadisticas.Response;
using TorneoPro.API.Hubs;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces.Estadistica_Vivo;

namespace TorneoPro.API.Servicios.Implementaciones.SignalR
{
    public class EstadisticaEnVivoService : IEstadisticaEnVivoService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<EstadisticasHub> _hubContext;
        private readonly ILogger<EstadisticaEnVivoService> _logger;
        private Timer? _timer;
        private readonly Dictionary<int, DateTime> _ultimaActualizacion = new();
        private bool _servicioActivo = false;

        public EstadisticaEnVivoService(
            IServiceScopeFactory scopeFactory,
            IHubContext<EstadisticasHub> hubContext,
            ILogger<EstadisticaEnVivoService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        public void IniciarServicio()
        {
            if (_servicioActivo) return;

            _timer = new Timer(ActualizarEstadisticasEnVivo, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            _servicioActivo = true;
            _logger.LogInformation("Servicio de estadísticas en vivo iniciado");
        }

        public void DetenerServicio()
        {
            _timer?.Dispose();
            _servicioActivo = false;
            _logger.LogInformation("Servicio de estadísticas en vivo detenido");
        }

        private async void ActualizarEstadisticasEnVivo(object? state)
        {
            try
            {
                // Crear un scope para acceder al DbContext (Scoped) desde un Singleton
                using var scope = _scopeFactory.CreateScope();
                var contexto = scope.ServiceProvider.GetRequiredService<TorneoProContext>();

                var partidosEnVivo = await contexto.partidos
                    .Where(p => p.estado == "EN_VIVO" && p.activo == true)
                    .ToListAsync();

                foreach (var partido in partidosEnVivo)
                {
                    // Evitar actualizaciones demasiado frecuentes
                    if (_ultimaActualizacion.ContainsKey(partido.id) &&
                        (DateTime.UtcNow - _ultimaActualizacion[partido.id]).TotalSeconds < 5)
                        continue;

                    await ActualizarYTransmitirEstadisticas(partido.id);
                    _ultimaActualizacion[partido.id] = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en actualización de estadísticas en vivo");
            }
        }

        private async Task ActualizarYTransmitirEstadisticas(int partidoId)
        {
            var estadisticas = await ObtenerEstadisticasEnVivoAsync(partidoId);

            await EstadisticasHub.ActualizarEstadisticasPartido(
                _hubContext,
                partidoId,
                estadisticas);

            _logger.LogDebug("Estadísticas transmitidas para partido {PartidoId}", partidoId);
        }

        public async Task<EstadisticaPartidoResponse> ObtenerEstadisticasEnVivoAsync(int partidoId)
        {
            // Crear scope propio para este método también
            using var scope = _scopeFactory.CreateScope();
            var contexto = scope.ServiceProvider.GetRequiredService<TorneoProContext>();

            var partido = await contexto.partidos
                .Include(p => p.id_equipo_localNavigation)
                .Include(p => p.id_equipo_visitanteNavigation)
                .Include(p => p.partidos_eventos)
                    .ThenInclude(e => e.id_jugadorNavigation)
                .Include(p => p.partidos_eventos)
                    .ThenInclude(e => e.id_equipoNavigation)
                .FirstOrDefaultAsync(p => p.id == partidoId);

            if (partido == null)
                throw new KeyNotFoundException("Partido no encontrado");

            var eventos = partido.partidos_eventos?.ToList() ?? new List<partidos_evento>();

            int minutoActual = partido.minuto_actual ?? 0;
            string tiempoActual = partido.tiempo_actual ?? "PRIMER_TIEMPO";

            var estadisticasEnVivo = new
            {
                MinutoActual = minutoActual,
                TiempoActual = tiempoActual,
                PosesionLocal = CalcularPosesion(partido.id_equipo_local, eventos),
                PosesionVisitante = CalcularPosesion(partido.id_equipo_visitante, eventos),
                TirosLocal = ContarEventos(partido.id_equipo_local, eventos, "TIRO"),
                TirosVisitante = ContarEventos(partido.id_equipo_visitante, eventos, "TIRO"),
                TirosPuertaLocal = ContarEventos(partido.id_equipo_local, eventos, "TIRO_PUERTA"),
                TirosPuertaVisitante = ContarEventos(partido.id_equipo_visitante, eventos, "TIRO_PUERTA"),
                FaltasLocal = ContarEventos(partido.id_equipo_local, eventos, "FALTA"),
                FaltasVisitante = ContarEventos(partido.id_equipo_visitante, eventos, "FALTA"),
                CornersLocal = ContarEventos(partido.id_equipo_local, eventos, "CORNER"),
                CornersVisitante = ContarEventos(partido.id_equipo_visitante, eventos, "CORNER"),
                FuerasJuegoLocal = ContarEventos(partido.id_equipo_local, eventos, "FUERA_JUEGO"),
                FuerasJuegoVisitante = ContarEventos(partido.id_equipo_visitante, eventos, "FUERA_JUEGO"),
                EventosRecientes = eventos
                    .OrderByDescending(e => e.minuto)
                    .Take(10)
                    .Select(e => new EventoPartidoResumen
                    {
                        TipoEvento = e.tipo_evento ?? "",
                        Minuto = e.minuto,
                        Jugador = e.id_jugadorNavigation != null
                            ? $"{e.id_jugadorNavigation.nombres} {e.id_jugadorNavigation.apellidos}"
                            : "",
                        Equipo = e.id_equipoNavigation?.nombre ?? ""
                    }).ToList()
            };

            return new EstadisticaPartidoResponse
            {
                IdPartido = partido.id,
                Local = partido.id_equipo_localNavigation?.nombre ?? "",
                Visitante = partido.id_equipo_visitanteNavigation?.nombre ?? "",
                GolesLocal = partido.goles_local ?? 0,
                GolesVisitante = partido.goles_visitante ?? 0,
                FechaHora = partido.fecha_hora,
                Estado = partido.estado ?? "PROGRAMADO",
                TotalEventos = eventos.Count,
                TotalGoles = eventos.Count(e => e.tipo_evento == "GOL"),
                TotalTarjetasAmarillas = eventos.Count(e => e.tipo_evento == "TARJETA_AMARILLA"),
                TotalTarjetasRojas = eventos.Count(e => e.tipo_evento == "TARJETA_ROJA"),
                TotalSustituciones = eventos.Count(e => e.tipo_evento == "SUSTITUCION"),
                EventosRecientes = estadisticasEnVivo.EventosRecientes,
                DatosEnVivo = estadisticasEnVivo
            };
        }

        private decimal CalcularPosesion(int equipoId, List<partidos_evento> eventos)
        {
            var eventosEquipo = eventos.Count(e => e.id_equipo == equipoId && e.tipo_evento == "PASE");
            var eventosRival = eventos.Count(e => e.id_equipo != equipoId && e.tipo_evento == "PASE");

            if (eventosEquipo + eventosRival == 0) return 50;

            return Math.Round((decimal)eventosEquipo / (eventosEquipo + eventosRival) * 100, 1);
        }

        private int ContarEventos(int equipoId, List<partidos_evento> eventos, string tipoEvento)
        {
            return eventos.Count(e => e.id_equipo == equipoId && e.tipo_evento == tipoEvento);
        }
    }
}
