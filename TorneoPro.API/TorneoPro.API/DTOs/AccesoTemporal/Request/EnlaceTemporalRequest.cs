using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.AccesoTemporal.Request
{
    public class EnlaceTemporalRequest
    {
        [Required(ErrorMessage = "El ID de la entidad es requerido")]
        public int IdEntidad { get; set; }

        [Required(ErrorMessage = "El tipo de entidad es requerido")]
        public TipoEntidad TipoEntidad { get; set; }

        [Required(ErrorMessage = "El tipo de usuario destinatario es requerido")]
        public TipoUsuario Destinatario { get; set; }

        public int? IdUsuarioDestino { get; set; }

        [Range(1, 168, ErrorMessage = "Las horas de validez deben estar entre 1 y 168 (7 días)")]
        public int HorasValidez { get; set; } = 24;

        public string? Metadatos { get; set; }
    }

    //public class EnlaceTemporalResponse
    //{
    //    public int Id { get; set; }
    //    public string Token { get; set; } = string.Empty;
    //    public string EnlaceUnico { get; set; } = string.Empty;
    //    public string UrlCompleta { get; set; } = string.Empty;
    //    public DateTime FechaExpiracion { get; set; }
    //    public TipoEntidad TipoEntidad { get; set; }
    //    public int IdEntidad { get; set; }
    //    public string EntidadNombre { get; set; } = string.Empty;
    //    public TipoUsuario Destinatario { get; set; }
    //    public int? IdUsuarioDestino { get; set; }
    //    public string? UsuarioDestino { get; set; }
    //    public bool EsActivo { get; set; }
    //}


    public class EnlaceTemporalResponse
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string EnlaceUnico { get; set; } = string.Empty;

        // URL ÚNICA que se envía al usuario (SIEMPRE web)
        public string InviteUrl { get; set; } = string.Empty;

        // Información para la app (profundidad)
        public string? DeepLink { get; set; }

        // Metadatos
        public DateTime FechaExpiracion { get; set; }
        public TipoEntidad TipoEntidad { get; set; }
        public int IdEntidad { get; set; }
        public string EntidadNombre { get; set; } = string.Empty;
        public TipoUsuario? Destinatario { get; set; }
        public int? IdUsuarioDestino { get; set; }
        public bool EsActivo { get; set; }

        // Información para UI web intermedia
        public InviteInfoResponse? InviteInfo { get; set; }
    }

    // DTO para la página web intermedia
    public class InviteInfoResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string NombreEntidad { get; set; } = string.Empty;
        public string DeepLink { get; set; } = string.Empty;
        public bool RequiereAutenticacion { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public bool EsValido { get; set; }
        public string? ErrorMensaje { get; set; }
    }


    public enum TipoEntidad
    {
        PARTIDO = 1,
        TORNEO = 2,
        PLANILLA = 3,
        INFORME = 4,
        SANCION = 5,
        CONVOCATORIA = 6,
        ACTA_DIGITAL = 7,
        EQUIPO = 8
    }

    public enum TipoUsuario
    {
        SUPER_ADMIN = 1,
        ADMIN = 2,
        SUB_ADMIN = 3,
        ARBITRO = 4,
        CAPITAN = 5,
        JUGADOR = 6
    }
}