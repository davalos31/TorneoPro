namespace TorneoPro.API.DTOs.Equipos.Response
{
    public class SuspensionResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int IdJugador { get; set; }
        public string Jugador { get; set; } = string.Empty;
        public int IdEquipo { get; set; }
        public string Equipo { get; set; } = string.Empty;
        public int IdTorneo { get; set; }
        public string Torneo { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public int PartidosSuspension { get; set; }
        public int PartidosCumplidos { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFinEstimada { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool GeneradaAutomaticamente { get; set; }
        public string? ReglaAplicada { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}