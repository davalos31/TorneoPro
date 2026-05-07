using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Equipos.Request
{
    public class CrearEquipoRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200, ErrorMessage = "Máximo 200 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string? NombreCorto { get; set; }

        public string? ColorPrimario { get; set; }
        public string? ColorSecundario { get; set; }

        public DateTime? FechaFundacion { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public string? EstadioHabitual { get; set; }

        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? Telefono { get; set; }

        public string? SitioWeb { get; set; }
    }
}