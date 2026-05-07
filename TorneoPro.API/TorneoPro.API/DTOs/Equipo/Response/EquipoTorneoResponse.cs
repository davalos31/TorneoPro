namespace TorneoPro.API.DTOs.Equipos.Response
{
    public class EquipoTorneoResponse
    {
        public int Id { get; set; }
        public int IdTorneo { get; set; }
        public string Torneo { get; set; } = string.Empty;
        public string? TorneoLogo { get; set; }
        public DateTime FechaInscripcion { get; set; }
        public bool Aprobado { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int? IdFase { get; set; }
        public string? Fase { get; set; }
        public string? Grupo { get; set; }
        public bool TieneBye { get; set; }
    }
}