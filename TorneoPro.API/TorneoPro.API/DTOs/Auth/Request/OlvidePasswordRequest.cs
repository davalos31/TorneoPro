using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Auth.Request
{
    public class OlvidePasswordRequest
    {
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = string.Empty;
    }
}