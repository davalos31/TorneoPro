using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Canchas
{
    public class BuscarCanchaRequest
    {
        [Required(ErrorMessage = "La fecha y hora es requerida")]
        public DateTime FechaHoraInicio { get; set; }

        [Required(ErrorMessage = "La fecha y hora fin es requerida")]
        public DateTime FechaHoraFin { get; set; }

        public int? IdTipoSuperficie { get; set; }
        public string? Ciudad { get; set; }
        public int? CapacidadMinima { get; set; }
        public bool? TieneIluminacion { get; set; }
    }
}