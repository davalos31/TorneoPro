using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Canchas
{
    public class ActualizarCanchaRequest
    {
        [MaxLength(200)]
        public string? Nombre { get; set; }

        [MaxLength(50)]
        public string? NombreCorto { get; set; }

        public int? IdTipoSuperficie { get; set; }

        public string? Direccion { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string? UrlMapa { get; set; }

        public int? CapacidadEspectadores { get; set; }
        public bool? TieneIluminacion { get; set; }
        public bool? TieneVestuarios { get; set; }
        public bool? TieneEstacionamiento { get; set; }

        public string? Estado { get; set; }
    }
}