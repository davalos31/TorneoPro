using TorneoPro.API.Controllers;
using TorneoPro.API.DTOs.AccesoTemporal.Request;
using TorneoPro.API.DTOs.AccesoTemporal.Response;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using DeepLinkInfoResponse = TorneoPro.API.DTOs.AccesoTemporal.Response.DeepLinkInfoResponse;

namespace TorneoPro.API.Servicios.Interfaces.AccesoTemporal
{
    public interface IAccesoTemporalService
    {
        #region Creación de Enlaces

        /// <summary>
        /// Crea enlace temporal genérico
        /// </summary>
        Task<EnlaceTemporalResponse> CrearEnlaceTemporalAsync(
            int usuarioIdCreador,
            EnlaceTemporalRequest solicitud,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Crea enlace para árbitro de partido
        /// </summary>
        Task<EnlaceTemporalResponse> CrearEnlaceArbitroPartidoAsync(
            int usuarioIdCreador,
            int idPartido,
            int? idUsuarioDestino = null,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Crea enlace para capitán (alineación)
        /// </summary>
        Task<EnlaceTemporalResponse> CrearEnlaceCapitanAlineacionAsync(
            int usuarioIdCreador,
            int idPartido,
            int idEquipo,
            int? idUsuarioDestino = null,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Crea enlace para jugador (confirmar asistencia)
        /// </summary>
        Task<EnlaceTemporalResponse> CrearEnlaceJugadorAsistenciaAsync(
            int usuarioIdCreador,
            int idPartido,
            int idJugador,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Crea invitación para unirse a equipo
        /// </summary>
        Task<EnlaceTemporalResponse> CrearEnlaceInvitacionEquipoAsync(
            int usuarioIdCreador,
            int idEquipo,
            int idUsuarioDestino,
            string? ipAddress = null,
            string? userAgent = null);

        #endregion

        #region Uso y Consulta

        /// <summary>
        /// Usa un enlace temporal
        /// </summary>
        Task<UsarEnlaceTemporalResponse> UsarEnlaceTemporalAsync(
            string token,
            int usuarioId,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Obtiene información de un enlace (sin usarlo)
        /// </summary>
        Task<EnlaceTemporalResponse?> ObtenerInfoEnlaceAsync(string token);

        /// <summary>
        /// Obtiene información para página web de invitación
        /// </summary>
        Task<InviteInfoResponse?> ObtenerInfoInvitacionAsync(string token);

        /// <summary>
        /// Obtiene información para deep link (app móvil)
        /// </summary>
        Task<DeepLinkInfoResponse?> ObtenerInfoDeepLinkAsync(string token);

        #endregion

        #region Registro Público

        /// <summary>
        /// Registra un nuevo jugador desde invitación
        /// </summary>
        Task<RegistroPublicoResponse> RegistrarJugadorDesdeInvitacionAsync(
            RegistroPublicoRequest request,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Usuario existente se une a equipo
        /// </summary>
        Task<UnirseEquipoResponse> UnirseAEquipoAsync(
            string tokenEquipo,
            int usuarioId,
            string? ipAddress = null,
            string? userAgent = null);

        #endregion

        #region Gestión

        /// <summary>
        /// Obtiene enlaces del usuario (paginado)
        /// </summary>
        Task<ResultadoPaginado<EnlaceTemporalResponse>> ObtenerMisEnlacesAsync(
            int usuarioId,
            FiltrarEnlaceTemporalRequest solicitud);

        /// <summary>
        /// Desactiva un enlace
        /// </summary>
        Task DesactivarEnlaceAsync(int id, int usuarioId, string? motivo = null);

        /// <summary>
        /// Renueva un enlace
        /// </summary>
        Task<EnlaceTemporalResponse> RenovarEnlaceAsync(
            int id,
            int usuarioId,
            int horasExtra,
            string? ipAddress = null);

        /// <summary>
        /// Obtiene historial de usos
        /// </summary>
        Task<List<UsoEnlaceTemporalResponse>> ObtenerHistorialUsosAsync(int enlaceId, int usuarioId);

        /// <summary>
        /// Envía enlaces automáticos para partidos del día siguiente
        /// </summary>
        Task EnviarEnlacesAutomaticosAsync();

        #endregion
    }

    
}