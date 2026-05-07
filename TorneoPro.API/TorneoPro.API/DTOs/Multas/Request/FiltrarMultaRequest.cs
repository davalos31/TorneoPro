using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.DTOs.Multas.Request
{
    public class FiltrarMultaRequest : PaginacionRequest
    {
        public int? IdTorneo { get; set; }
        public int? IdEquipo { get; set; }
        public int? IdJugador { get; set; }
        public int? IdTipoMulta { get; set; }
        public string? Estado { get; set; }
        public bool? Pagadas { get; set; }
        public bool? Vencidas { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }
}
