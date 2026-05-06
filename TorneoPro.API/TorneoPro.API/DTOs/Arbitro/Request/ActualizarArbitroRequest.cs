using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Arbitros.Request
{
    public class ActualizarArbitroRequest
    {
        [MaxLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string? Nombres { get; set; }

        [MaxLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string? Apellidos { get; set; }

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? Telefono { get; set; }

        public string? Especialidad { get; set; }
        public int? AniosExperiencia { get; set; }
        public string? Categoria { get; set; }
        public bool? Activo { get; set; }
    }
}
