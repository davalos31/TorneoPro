using TorneoPro.API.Controllers;
using TorneoPro.API.DTOs.AccesoTemporal;
using TorneoPro.API.DTOs.AccesoTemporal.Request;
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.Servicios.Interfaces.AccesoTemporal
{
    public interface IAccesoTemporalService
    {

        /// <summary>
        /// Obtiene información de una invitación para la página web intermedia
        /// </summary>
        Task<InviteInfoResponse?> ObtenerInfoInvitacionAsync(string token);

        /// <summary>
        /// Crear enlace temporal para acceso a entidad específica
        /// </summary>
        Task<EnlaceTemporalResponse> CrearEnlaceTemporalAsync(int usuarioIdCreador, EnlaceTemporalRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Crear enlace para árbitro de partido (con expiración automática post-partido)
        /// </summary>
        Task<EnlaceTemporalResponse> CrearEnlaceArbitroPartidoAsync(int usuarioIdCreador, int idPartido, int? idUsuarioDestino = null, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Crear enlace para capitán para alineación (expira 1 hora antes del partido)
        /// </summary>
        Task<EnlaceTemporalResponse> CrearEnlaceCapitanAlineacionAsync(int usuarioIdCreador, int idPartido, int idEquipo, int? idUsuarioDestino = null, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Crear enlace para jugador confirmar asistencia (expira 24 horas antes del partido)
        /// </summary>
        Task<EnlaceTemporalResponse> CrearEnlaceJugadorAsistenciaAsync(int usuarioIdCreador, int idPartido, int idJugador, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Crear enlace para invitar a un jugador a un equipo (sin partido asociado)
        /// </summary>
        Task<EnlaceTemporalResponse> CrearEnlaceInvitacionEquipoAsync(int usuarioIdCreador, int idEquipo, int idUsuarioDestino, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Usar enlace temporal (acceder a la entidad)
        /// </summary>
        Task<UsarEnlaceTemporalResponse> UsarEnlaceTemporalAsync(string token, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener información del enlace (sin usarlo)
        /// </summary>
        Task<EnlaceTemporalResponse?> ObtenerInfoEnlaceAsync(string token);

        /// <summary>
        /// Obtener mis enlaces temporales creados
        /// </summary>
        Task<ResultadoPaginado<EnlaceTemporalResponse>> ObtenerMisEnlacesAsync(int usuarioId, FiltrarEnlaceTemporalRequest solicitud);

        /// <summary>
        /// Desactivar enlace temporal
        /// </summary>
        Task DesactivarEnlaceAsync(int id, int usuarioId, string? motivo = null);

        /// <summary>
        /// Renovar enlace temporal
        /// </summary>
        Task<EnlaceTemporalResponse> RenovarEnlaceAsync(int id, int usuarioId, int horasExtra, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener historial de usos del enlace
        /// </summary>
        Task<List<UsoEnlaceTemporalResponse>> ObtenerHistorialUsosAsync(int enlaceId, int usuarioId);

        /// <summary>
        /// Enviar enlaces automáticos para partidos del día (job programado)
        /// </summary>
        Task EnviarEnlacesAutomaticosAsync();

        /// <summary>
        /// Obtener información del deep link para la app móvil
        /// </summary>
        Task<DeepLinkInfoResponse?> ObtenerInfoDeepLinkAsync(string token);
    }

    public class UsoEnlaceTemporalResponse
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime FechaUso { get; set; }
        public string? IpAddress { get; set; }
        public bool UsoExitoso { get; set; }
        public string? MotivoFallo { get; set; }
    }
}