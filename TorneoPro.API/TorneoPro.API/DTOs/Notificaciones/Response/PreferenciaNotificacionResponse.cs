namespace TorneoPro.API.DTOs.Notificaciones
{
    public class PreferenciaNotificacionResponse
    {
        public int Id { get; set; }
        public int IdTipoNotificacion { get; set; }
        public string TipoNotificacion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public bool Activado { get; set; }
        public bool PushActivado { get; set; }
        public bool EmailActivado { get; set; }
        public bool SmsActivado { get; set; }
        public DateTime FechaModificacion { get; set; }
    }
}