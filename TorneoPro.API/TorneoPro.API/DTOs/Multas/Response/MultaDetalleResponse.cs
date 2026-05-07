namespace TorneoPro.API.DTOs.Multas.Response
{
    public class MultaDetalleResponse : MultaResponse
    {
        public int? IdPartido { get; set; }
        public string? Partido { get; set; }
        public int? IdEvento { get; set; }
        public bool GeneradaAutomaticamente { get; set; }
        public string? NotasInternas { get; set; }
        public List<PagoResponse> HistorialPagos { get; set; } = new();
    }
}