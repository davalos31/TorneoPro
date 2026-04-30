using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Jugadores.Request
{
    public class RegistrarJugadorRequest
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

        public int? IdTipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }

        [Range(30, 300, ErrorMessage = "El peso debe estar entre 30 y 300 kg")]
        public decimal? PesoKg { get; set; }

        [Range(100, 250, ErrorMessage = "La altura debe estar entre 100 y 250 cm")]
        public decimal? AlturaCm { get; set; }

        public DateTime? FechaNacimiento { get; set; }
        public string? Genero { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public string? PosicionPreferida { get; set; }
        public int? NumeroCamisetaPreferido { get; set; }
    }
}