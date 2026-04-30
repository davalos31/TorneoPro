using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.DTOs.Canchas
{
    public class FiltrarCanchaRequest : PaginacionRequest
    {
        public string? Buscar { get; set; }
        public int? IdTipoSuperficie { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public string? Estado { get; set; }
        public bool? TieneIluminacion { get; set; }
        public bool? TieneVestuarios { get; set; }
        public bool? TieneEstacionamiento { get; set; }
        public int? CapacidadMinima { get; set; }
        public bool? SoloActivos { get; set; }
    }
}