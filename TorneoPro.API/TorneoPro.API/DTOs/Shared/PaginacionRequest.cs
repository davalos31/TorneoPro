namespace TorneoPro.API.DTOs.Shared
{
    public class PaginacionRequest
    {
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 20;
        public string? Buscar { get; set; }
        public string? OrdenarPor { get; set; }
        public bool OrdenDescendente { get; set; } = false;
    }
}