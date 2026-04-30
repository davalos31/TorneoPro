namespace TorneoPro.API.DTOs.Estadisticas.Response
{
    public class EstadisticaPartidoResponse
    {
        public int IdPartido { get; set; }
        public string Local { get; set; } = string.Empty;
        public string Visitante { get; set; } = string.Empty;
        public int GolesLocal { get; set; }
        public int GolesVisitante { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int TotalEventos { get; set; }
        public int TotalGoles { get; set; }
        public int TotalTarjetasAmarillas { get; set; }
        public int TotalTarjetasRojas { get; set; }
        public int TotalSustituciones { get; set; }
        public List<EventoPartidoResumen> EventosRecientes { get; set; } = new();

        // Datos en vivo (opcional)
        public object? DatosEnVivo { get; set; }
    }

    public class EventoPartidoResumen
    {
        public string TipoEvento { get; set; } = string.Empty;
        public int Minuto { get; set; }
        public string Jugador { get; set; } = string.Empty;
        public string Equipo { get; set; } = string.Empty;
    }
}