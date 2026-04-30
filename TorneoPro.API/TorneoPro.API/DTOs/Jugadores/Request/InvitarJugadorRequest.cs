using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Jugadores.Request
{
    public class InvitarJugadorRequest
    {
        [Required(ErrorMessage = "El email del jugador es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ID del equipo es requerido")]
        public int IdEquipo { get; set; }

        public int? IdTorneo { get; set; }
        public int? NumeroCamiseta { get; set; }
        public string? Posicion { get; set; }
        public bool EsCapitan { get; set; } = false;
    }

    public class InvitarJugadorResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string? EnlaceInvitacion { get; set; }
        public bool EsUsuarioExistente { get; set; }
        public int? IdJugador { get; set; }
        public string? TokenEquipo { get; set; }
    }
}