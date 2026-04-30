using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Auth.Request
{
    public class RefrescarTokenRequest
    {
        [Required(ErrorMessage = "El token de actualización es requerido")]
        public string TokenActualizacion { get; set; } = string.Empty;
    }
}