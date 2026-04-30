using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Canchas
{
    public class SubirFotoCanchaRequest
    {
        [Required(ErrorMessage = "La foto es requerida")]
        public IFormFile Foto { get; set; } = null!;
    }
}