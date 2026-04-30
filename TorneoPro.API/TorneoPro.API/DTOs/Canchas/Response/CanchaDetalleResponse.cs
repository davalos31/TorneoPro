namespace TorneoPro.API.DTOs.Canchas.Response
{
    public class CanchaDetalleResponse : CanchaResponse
    {
        public string? Direccion { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string? UrlMapa { get; set; }
        public int? CapacidadEspectadores { get; set; }
        public bool TieneIluminacion { get; set; }
        public bool TieneVestuarios { get; set; }
        public bool TieneEstacionamiento { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        public List<DisponibilidadResponse> DisponibilidadSemana { get; set; } = new();
    }
}