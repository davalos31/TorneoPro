
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.DTOs.Enlaces.Request
{
    public class FiltrarEnlaceRequest : PaginacionRequest
    {
        public int? IdTipoEnlace { get; set; }
        public int? IdTorneo { get; set; }
        public int? IdEquipo { get; set; }
        public string? Estado { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public bool? SoloActivos { get; set; }
    }
}
