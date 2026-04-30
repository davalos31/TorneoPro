using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Usuarios.Request
{
    public class AsignarRolRequest
    {
        [Required(ErrorMessage = "El ID del rol es requerido")]
        public int IdRol { get; set; }

        public int? IdTorneo { get; set; }
        public int? IdEquipo { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es requerida")]
        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }
    }
}