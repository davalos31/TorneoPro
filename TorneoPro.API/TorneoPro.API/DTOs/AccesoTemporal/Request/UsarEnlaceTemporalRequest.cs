using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.AccesoTemporal.Request
{
    public class UsarEnlaceTemporalRequest
    {
        [Required(ErrorMessage = "El token del enlace es requerido")]
        public string Token { get; set; } = string.Empty;
    }

    public class UsarEnlaceTemporalResponse
    {
        public bool Exitoso { get; set; }
        public string? Mensaje { get; set; }
        public TipoEntidad TipoEntidad { get; set; }
        public int IdEntidad { get; set; }
        public Dictionary<string, object>? DatosAcceso { get; set; }

        // Datos específicos según el tipo de entidad
        public AccesoPartidoData? PartidoData { get; set; }
        public AccesoTorneoData? TorneoData { get; set; }
        public AccesoActaData? ActaData { get; set; }

        public AccesoEquipoData? EquipoData { get; set; }
    }

    public class AccesoPartidoData
    {
        public int IdPartido { get; set; }
        public string Local { get; set; } = string.Empty;
        public string Visitante { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string Cancha { get; set; } = string.Empty;
        public bool PuedeEditarEventos { get; set; }
        public bool PuedeFirmarActa { get; set; }
        public bool PuedeVerAlineaciones { get; set; }
        public bool PuedeRegistrarResultado { get; set; }
        public bool PuedeConfirmarAsistencia { get; set; }
    }

    public class AccesoTorneoData
    {
        public int IdTorneo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public bool PuedeVerTabla { get; set; }
        public bool PuedeVerCalendario { get; set; }
        public bool PuedeVerEstadisticas { get; set; }
        public bool PuedeInscribirEquipo { get; set; }
    }

    public class AccesoActaData
    {
        public int IdActa { get; set; }
        public int IdPartido { get; set; }
        public string ActaUrl { get; set; } = string.Empty;
        public bool PuedeFirmar { get; set; }
        public bool PuedeDescargarPdf { get; set; }
        public DateTime FechaLimiteFirma { get; set; }
    }

    public class AccesoEquipoData
    {
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; } = string.Empty;
        public bool PuedeUnirse { get; set; }
        public int IdJugadorInvitado { get; set; }
        public string? MensajeBienvenida { get; set; }
        public string? EscudoUrl { get; set; }
    }
}