namespace TorneoPro.API.DTOs.Equipos.Response
{
    public class EquipoResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? NombreCorto { get; set; }
        public string? EscudoUrl { get; set; }
        public string? ColorPrimario { get; set; }
        public string? ColorSecundario { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool Verificado { get; set; }
        public int? IdCapitan { get; set; }
        public string? Capitan { get; set; }
        public int TotalJugadores { get; set; }
    }
}