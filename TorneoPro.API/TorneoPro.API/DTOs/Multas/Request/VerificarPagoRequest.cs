using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Multas.Request
{
    public class VerificarPagoRequest
    {
        [Required(ErrorMessage = "El ID del pago es requerido")]
        public int IdPago { get; set; }

        [Required(ErrorMessage = "La acción es requerida (aprobar/rechazar)")]
        public string Accion { get; set; } = string.Empty;

        public string? MotivoRechazo { get; set; }
    }
}