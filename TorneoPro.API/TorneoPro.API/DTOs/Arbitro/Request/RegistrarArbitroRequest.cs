using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Arbitros.Request
{
    public class RegistrarArbitroRequest
    {
        [Required(ErrorMessage = "Los nombres son requeridos")]
        [MaxLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son requeridos")]
        [MaxLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? Telefono { get; set; }

        public string? Especialidad { get; set; }
        public int? AniosExperiencia { get; set; }
        public string? Categoria { get; set; } // NACIONAL, INTERNACIONAL, FIFA
    }
}
