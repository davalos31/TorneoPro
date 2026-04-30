namespace TorneoPro.API.DTOs.Canchas.Response
{
    public class CanchaResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? NombreCorto { get; set; }
        public int IdTipoSuperficie { get; set; }
        public string TipoSuperficie { get; set; } = string.Empty;
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string? FotoUrl { get; set; }
    }
}