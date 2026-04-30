using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Jugadores.Request
{
    public class ActualizarJugadorRequest
    {
        [MaxLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string? Nombres { get; set; }

        [MaxLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string? Apellidos { get; set; }

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? Telefono { get; set; }

        [Range(30, 300, ErrorMessage = "El peso debe estar entre 30 y 300 kg")]
        public decimal? PesoKg { get; set; }

        [Range(100, 250, ErrorMessage = "La altura debe estar entre 100 y 250 cm")]
        public decimal? AlturaCm { get; set; }

        [MaxLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string? Biografia { get; set; }

        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public string? Genero { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? TelefonoEmergencia { get; set; }
        public string? PosicionPreferida { get; set; }
        public int? NumeroCamisetaPreferido { get; set; }
    }
}