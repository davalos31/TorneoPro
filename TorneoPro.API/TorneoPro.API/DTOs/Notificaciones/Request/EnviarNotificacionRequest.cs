using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Notificaciones
{
    public class EnviarNotificacionRequest
    {
        [Required(ErrorMessage = "El ID del usuario destino es requerido")]
        public int IdUsuarioDestino { get; set; }

        [Required(ErrorMessage = "El tipo de notificación es requerido")]
        public int IdTipoNotificacion { get; set; }

        [Required(ErrorMessage = "El título es requerido")]
        [MaxLength(200, ErrorMessage = "Máximo 200 caracteres")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es requerido")]
        public string Mensaje { get; set; } = string.Empty;

        public string? Prioridad { get; set; } = "MEDIA";

        public int? IdTorneo { get; set; }
        public int? IdEquipo { get; set; }
        public int? IdPartido { get; set; }
        public int? IdMulta { get; set; }
        public int? IdSuspension { get; set; }

        public string? AccionUrl { get; set; }
        public string? AccionTipo { get; set; }

        public DateTime? FechaProgramadaEnvio { get; set; }
        public List<string> Canales { get; set; } = new() { "IN_APP" }; // IN_APP, EMAIL, PUSH, SMS
    }
}