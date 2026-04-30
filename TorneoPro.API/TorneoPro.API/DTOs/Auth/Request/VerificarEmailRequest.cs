using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Auth.Request
{
    public class VerificarEmailRequest
    {
        [Required(ErrorMessage = "El token es requerido")]
        public string Token { get; set; } = string.Empty;
    }
}