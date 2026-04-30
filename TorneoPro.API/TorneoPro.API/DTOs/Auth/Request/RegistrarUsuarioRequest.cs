using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Auth.Request
{
    public class RegistrarUsuarioRequest
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

        [Required(ErrorMessage = "El tipo de usuario es requerido")]
        public int IdTipoUsuario { get; set; }

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "El tipo de documento es requerido")]
        public int IdTipoDocumento { get; set; }

        [Required(ErrorMessage = "El número de documento es requerido")]
        [MinLength(5, ErrorMessage = "El número de documento debe tener al menos 5 caracteres")]
        [MaxLength(20, ErrorMessage = "El número de documento no puede exceder los 20 caracteres")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-\.]+$", ErrorMessage = "El número de documento contiene caracteres inválidos")]
        public string NumeroDocumento { get; set; } = string.Empty;
    }
}