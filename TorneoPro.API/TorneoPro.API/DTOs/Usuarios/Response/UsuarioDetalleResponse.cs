namespace TorneoPro.API.DTOs.Usuarios.Response
{
    public class UsuarioDetalleResponse : UsuarioResponse
    {
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? Genero { get; set; }
        public string? Telefono { get; set; }
        public string? TelefonoEmergencia { get; set; }
        public decimal? PesoKg { get; set; }
        public decimal? AlturaCm { get; set; }
        public string? Biografia { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public string? Direccion { get; set; }
        public bool EmailVerificado { get; set; }
        public bool TelefonoVerificado { get; set; }
        public DateTime? FechaUltimaConexion { get; set; }
    }
}