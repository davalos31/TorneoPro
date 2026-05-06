using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Arbitros.Request
{
    public class AsignarArbitroRequest
    {
        [Required(ErrorMessage = "El ID del partido es requerido")]
        public int IdPartido { get; set; }

        [Required(ErrorMessage = "El ID del árbitro es requerido")]
        public int IdArbitro { get; set; }

        public string? Tipo { get; set; } = "PRINCIPAL"; // PRINCIPAL, ASISTENTE1, ASISTENTE2, CUARTO
    }
}
