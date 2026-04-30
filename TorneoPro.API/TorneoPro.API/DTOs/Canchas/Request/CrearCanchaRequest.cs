using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Canchas
{
    public class CrearCanchaRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200, ErrorMessage = "Máximo 200 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string? NombreCorto { get; set; }

        [Required(ErrorMessage = "El tipo de superficie es requerido")]
        public int IdTipoSuperficie { get; set; }

        public string? Direccion { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string? UrlMapa { get; set; }

        public int? CapacidadEspectadores { get; set; }
        public bool TieneIluminacion { get; set; } = false;
        public bool TieneVestuarios { get; set; } = false;
        public bool TieneEstacionamiento { get; set; } = false;
    }
}