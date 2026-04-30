using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Canchas
{
    public class BloquearFechaRequest
    {
        [Required(ErrorMessage = "La fecha de inicio es requerida")]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de fin es requerida")]
        public DateTime FechaFin { get; set; }

        [Required(ErrorMessage = "El motivo es requerido")]
        public string Motivo { get; set; } = string.Empty;

        public int? IdMotivoCatalogo { get; set; }

        public bool AplicaATodasCanchas { get; set; } = true;
    }
}