namespace TorneoPro.API.DTOs.Enlaces.Response
{
    public class TipoEnlaceResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Alcance { get; set; }
        public bool PermiteExpiracion { get; set; }
    }
}
