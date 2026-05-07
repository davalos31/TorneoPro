using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Equipos.Request
{
    public class ActualizarEquipoRequest
    {
        [MaxLength(200, ErrorMessage = "Máximo 200 caracteres")]
        public string? Nombre { get; set; }

        [MaxLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string? NombreCorto { get; set; }

        public string? ColorPrimario { get; set; }
        public string? ColorSecundario { get; set; }

        public DateTime? FechaFundacion { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public string? EstadioHabitual { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Telefono { get; set; }

        public string? SitioWeb { get; set; }
    }
}