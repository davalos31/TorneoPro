using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.DTOs.Notificaciones
{
    public class FiltrarNotificacionRequest : PaginacionRequest
    {
        public bool? Leida { get; set; }
        public bool? Archivada { get; set; }
        public bool? Enviada { get; set; }
        public int? IdTipoNotificacion { get; set; }
        public string? Prioridad { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }
}
