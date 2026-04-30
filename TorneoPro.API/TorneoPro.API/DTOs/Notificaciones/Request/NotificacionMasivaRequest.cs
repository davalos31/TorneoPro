using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Notificaciones
{
    public class NotificacionMasivaRequest
    {
        [Required(ErrorMessage = "El tipo de notificación es requerido")]
        public int IdTipoNotificacion { get; set; }

        [Required(ErrorMessage = "El título es requerido")]
        [MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es requerido")]
        public string Mensaje { get; set; } = string.Empty;

        public string? Prioridad { get; set; } = "MEDIA";

        // Filtros de destinatarios
        public List<int>? IdsUsuarios { get; set; }
        public List<int>? IdsRoles { get; set; }
        public int? IdTorneo { get; set; }
        public int? IdEquipo { get; set; }

        public string? AccionUrl { get; set; }
        public List<string> Canales { get; set; } = new() { "IN_APP", "EMAIL" };
    }
}