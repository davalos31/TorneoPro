namespace TorneoPro.API.DTOs.Equipos.Response
{
    public class HistorialTorneoResponse
    {
        public int IdTorneo { get; set; }
        public string Torneo { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Formato { get; set; } = string.Empty;
        public int PartidosJugados { get; set; }
        public int PartidosGanados { get; set; }
        public int PartidosEmpatados { get; set; }
        public int PartidosPerdidos { get; set; }
        public int GolesFavor { get; set; }
        public int GolesContra { get; set; }
        public int DiferenciaGoles { get; set; }
        public int Puntos { get; set; }
        public int? PosicionFinal { get; set; }
        public string? Resultado { get; set; } // CAMPEON | SUBCAMPEON | SEMIFINALISTA | etc
    }
}