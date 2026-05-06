using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.DTOs.Arbitros.Request
{
    public class FiltrarArbitroRequest : PaginacionRequest
    {
        public string? Buscar { get; set; }
        public string? Especialidad { get; set; }
        public string? Categoria { get; set; }
        public bool? Activo { get; set; }
        public int? IdTorneo { get; set; }
        public bool? Disponibles { get; set; }
        public DateTime? FechaDisponibilidad { get; set; }
    }
}
