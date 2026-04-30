using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Notificaciones
{
    public class ActualizarPreferenciaRequest
    {
        public bool? Activado { get; set; }
        public bool? PushActivado { get; set; }
        public bool? EmailActivado { get; set; }
        public bool? SmsActivado { get; set; }
    }
}