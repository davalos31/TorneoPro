namespace TorneoPro.API.DTOs.Enlaces.Response
{
    public class UsoEnlaceResponse
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime FechaUso { get; set; }
        public string? IpAddress { get; set; }
        public int? IdRolAnterior { get; set; }
        public string? RolAnterior { get; set; }
        public int IdRolNuevo { get; set; }
        public string RolNuevo { get; set; } = string.Empty;
        public bool UsoExitoso { get; set; }
        public string? MotivoFallo { get; set; }
    }
}