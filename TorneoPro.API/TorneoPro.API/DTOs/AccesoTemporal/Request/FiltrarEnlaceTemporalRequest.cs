using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.DTOs.AccesoTemporal.Request
{
    public class FiltrarEnlaceTemporalRequest : PaginacionRequest
    {
        public TipoEntidad? TipoEntidad { get; set; }
        public int? IdEntidad { get; set; }
        public TipoUsuario? Destinatario { get; set; }
        public int? IdUsuarioDestino { get; set; }
        public string? Estado { get; set; }
        public bool? SoloActivos { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }
}