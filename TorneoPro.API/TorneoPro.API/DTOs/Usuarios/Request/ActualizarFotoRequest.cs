using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Usuarios.Request
{
    public class ActualizarFotoRequest
    {
        [Required(ErrorMessage = "La foto es requerida")]
        public IFormFile Foto { get; set; } = null!;
    }
}