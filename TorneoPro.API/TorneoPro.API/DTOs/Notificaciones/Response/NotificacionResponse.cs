namespace TorneoPro.API.DTOs.Notificaciones
{
    public class NotificacionResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int IdTipoNotificacion { get; set; }
        public string TipoNotificacion { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public bool Leida { get; set; }
        public DateTime? FechaLectura { get; set; }
        public bool Archivada { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaProgramadaEnvio { get; set; }
        public bool Enviada { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public string Canal { get; set; } = string.Empty;
        public string? AccionUrl { get; set; }
        public string? AccionTipo { get; set; }
        public int? IdTorneo { get; set; }
        public string? Torneo { get; set; }
        public int? IdEquipo { get; set; }
        public string? Equipo { get; set; }
        public int? IdPartido { get; set; }
        public int? IdMulta { get; set; }
        public int? IdSuspension { get; set; }
    }
}