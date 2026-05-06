using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Enlaces.Request
{
    public class CrearEnlaceRequest
    {
        [Required(ErrorMessage = "El tipo de enlace es requerido")]
        public int IdTipoEnlace { get; set; }

        [Required(ErrorMessage = "El rol a asignar es requerido")]
        public int IdRolAsignado { get; set; }

        public int? IdTorneo { get; set; }
        public int? IdEquipo { get; set; }

        public DateTime? FechaExpiracion { get; set; }
        public int? MaxUsos { get; set; }
    }
}