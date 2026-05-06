namespace TorneoPro.API.DTOs.Enlaces.Response
{
    public class EnlaceInfoResponse
    {
        public string CodigoEnlace { get; set; } = string.Empty;
        public string TipoEnlace { get; set; } = string.Empty;
        public string RolAsignado { get; set; } = string.Empty;
        public string? Torneo { get; set; }
        public string? Equipo { get; set; }
        public DateTime? FechaExpiracion { get; set; }
        public int? UsosRestantes { get; set; }
        public bool Valido { get; set; }
        public string? Mensaje { get; set; }
    }
}