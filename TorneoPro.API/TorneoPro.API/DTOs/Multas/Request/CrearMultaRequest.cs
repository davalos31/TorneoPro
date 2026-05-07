using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Multas.Request
{
    public class CrearMultaRequest
    {
        [Required(ErrorMessage = "El ID del torneo es requerido")]
        public int IdTorneo { get; set; }

        [Required(ErrorMessage = "El tipo de multa es requerido")]
        public int IdTipoMulta { get; set; }

        public int? IdEquipo { get; set; }
        public int? IdJugador { get; set; }

        public int? IdPartido { get; set; }
        public int? IdEvento { get; set; }
        public bool GeneradaAutomaticamente { get; set; } = false;

        [Required(ErrorMessage = "El monto es requerido")]
        [Range(0.01, 100000, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal Monto { get; set; }

        public string? Moneda { get; set; } = "Bs";

        public DateTime? FechaLimitePago { get; set; }

        [Required(ErrorMessage = "La descripción es requerida")]
        public string Descripcion { get; set; } = string.Empty;

        public string? NotasInternas { get; set; }
    }
}