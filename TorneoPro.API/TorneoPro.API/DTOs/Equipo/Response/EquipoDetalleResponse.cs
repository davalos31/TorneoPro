namespace TorneoPro.API.DTOs.Equipos.Response
{
    public class EquipoDetalleResponse : EquipoResponse
    {
        public DateTime? FechaFundacion { get; set; }
        public string? EstadioHabitual { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? SitioWeb { get; set; }
        public int IdCreador { get; set; }
        public string Creador { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public List<JugadorEquipoResponse> Jugadores { get; set; } = new();
        public List<EquipoTorneoResponse> Torneos { get; set; } = new();
    }
}