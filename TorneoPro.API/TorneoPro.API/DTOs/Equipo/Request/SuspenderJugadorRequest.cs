using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Equipos.Request
{
    public class SuspenderJugadorRequest
    {
        [Required(ErrorMessage = "El ID del jugador es requerido")]
        public int IdJugador { get; set; }

        [Required(ErrorMessage = "El ID del torneo es requerido")]
        public int IdTorneo { get; set; }

        [Required(ErrorMessage = "El motivo es requerido")]
        [MaxLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string Motivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cantidad de partidos es requerida")]
        [Range(1, 20, ErrorMessage = "Mínimo 1 partido, máximo 20")]
        public int PartidosSuspension { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es requerida")]
        public DateTime FechaInicio { get; set; }

        public int? IdPartidoOrigen { get; set; }
        public int? IdEventoOrigen { get; set; }
    }
}