namespace TorneoPro.API.DTOs.Multas.Response
{
    public class PagoResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int IdMulta { get; set; }
        public int IdMetodoPago { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public decimal MontoPagado { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public DateTime FechaPago { get; set; }
        public string? CodigoTransaccion { get; set; }
        public string? ComprobanteUrl { get; set; }
        public bool ComprobanteVerificado { get; set; }
        public DateTime? FechaVerificacion { get; set; }
        public string? VerificadoPor { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? MotivoRechazo { get; set; }
    }
}