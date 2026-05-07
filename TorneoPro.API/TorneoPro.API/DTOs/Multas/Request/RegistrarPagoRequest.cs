using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Multas.Request
{
    public class RegistrarPagoRequest
    {
        [Required(ErrorMessage = "El método de pago es requerido")]
        public int IdMetodoPago { get; set; }

        [Required(ErrorMessage = "El monto pagado es requerido")]
        [Range(0.01, 100000, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal MontoPagado { get; set; }

        public string? Moneda { get; set; } = "Bs";

        public string? CodigoTransaccion { get; set; }

        public IFormFile? Comprobante { get; set; }
    }
}