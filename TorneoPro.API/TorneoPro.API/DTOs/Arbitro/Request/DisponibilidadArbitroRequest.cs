using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Arbitros.Request
{
    public class DisponibilidadArbitroRequest
    {
        [Required(ErrorMessage = "La fecha y hora es requerida")]
        public DateTime FechaHora { get; set; }

        public bool Disponible { get; set; } = true;
        public string? MotivoNoDisponibilidad { get; set; }
    }
}
