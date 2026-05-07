using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Jugadores.Request
{
    public class SuspenderJugadorRequest
    {
 
        [Required(ErrorMessage = "El motivo es requerido")]
        [MaxLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string Motivo { get; set; } = string.Empty;


        [Required(ErrorMessage = "El número de partidos es requerido")]
        [Range(1, 20, ErrorMessage = "Los partidos de suspensión deben estar entre 1 y 20")]
        public int PartidosSuspension { get; set; } = 1;

        public int? IdTorneo { get; set; }
    }
}
