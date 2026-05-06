namespace TorneoPro.API.DTOs.AccesoTemporal.Response
{
    public class UsoEnlaceTemporalResponse
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime FechaUso { get; set; }
        public string? IpAddress { get; set; }
        public bool UsoExitoso { get; set; }
        public string? MotivoFallo { get; set; }
    }
}
