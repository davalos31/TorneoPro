using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Equipos.Request
{
    public class AgregarJugadorRequest
    {
        [Required(ErrorMessage = "El ID del jugador es requerido")]
        public int IdJugador { get; set; }

        public int? NumeroCamiseta { get; set; }
        public string? Posicion { get; set; }
        public bool EsCapitan { get; set; } = false;
    }
}