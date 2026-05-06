using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Arbitros.Request
{
    public class CalificacionArbitroRequest
    {
        [Required(ErrorMessage = "El ID del partido es requerido")]
        public int IdPartido { get; set; }

        [Required(ErrorMessage = "La puntuación es requerida")]
        [Range(1, 10, ErrorMessage = "La puntuación debe estar entre 1 y 10")]
        public int Puntuacion { get; set; }

        [MaxLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string? Comentario { get; set; }
    }

    public class CalificacionArbitroResponse
    {
        public int Id { get; set; }
        public int IdPartido { get; set; }
        public string Partido { get; set; } = string.Empty;
        public int Puntuacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime FechaCalificacion { get; set; }
        public string CalificadoPor { get; set; } = string.Empty;
    }
}
