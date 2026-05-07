using TorneoPro.API.Controllers;
using TorneoPro.API.DTOs.Jugadores;
using TorneoPro.API.DTOs.Jugadores.Request;
using TorneoPro.API.DTOs.Jugadores.Response;
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.Servicios.Interfaces.Jugador
{
    public interface IJugadorService
    {
        /// <summary>
        /// Listar jugadores con filtros y paginación
        /// </summary>
        Task<ResultadoPaginado<JugadorResponse>> ObtenerTodosAsync(FiltrarJugadorRequest solicitud);

        /// <summary>
        /// Obtener jugador por ID
        /// </summary>
        Task<JugadorResponse?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Obtener jugador por email
        /// </summary>
        Task<JugadorResponse?> ObtenerPorEmailAsync(string email);

        /// <summary>
        /// Registrar nuevo jugador (crea usuario con rol JUGADOR)
        /// </summary>
        Task<JugadorResponse> RegistrarJugadorAsync(int usuarioIdAdmin, RegistrarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Actualizar datos del jugador
        /// </summary>
        Task<JugadorResponse> ActualizarJugadorAsync(int id, int usuarioId, ActualizarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Invitar jugador por email (crea enlace temporal)
        /// </summary>
        Task<InvitarJugadorResponse> InvitarJugadorAsync(int usuarioIdAdmin, InvitarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Aceptar invitación (usar enlace)
        /// </summary>
        Task<JugadorResponse> AceptarInvitacionAsync(string token, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener estadísticas del jugador
        /// </summary>
        Task<EstadisticasJugadorResumen> ObtenerEstadisticasAsync(int id, int? idTorneo = null);

        /// <summary>
        /// Obtener equipos del jugador
        /// </summary>
        Task<List<EquipoJugadorResponse>> ObtenerEquiposAsync(int id);

        /// <summary>
        /// Suspender jugador
        /// </summary>
        Task SuspenderJugadorAsync(int id, int usuarioIdAdmin, string motivo, int partidosSuspension, int? idTorneo = null, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Rehabilitar jugador
        /// </summary>
        Task RehabilitarJugadorAsync(int id, int usuarioIdAdmin, string? motivo = null, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener historial de suspensiones del jugador
        /// </summary>
        Task<List<SuspensionJugadorResponse>> ObtenerSuspensionesAsync(int id);

        /// <summary>
        /// Enviar credencial digital del jugador (PDF con QR)
        /// </summary>
        Task<byte[]> GenerarCredencialAsync(int id, int idTorneo, string baseUrl);

        /// <summary>
        /// Obtener historial de partidos del jugador
        /// </summary>
        Task<List<PartidoJugadorResponse>> ObtenerHistorialPartidosAsync(int id, int? idTorneo = null, int? limite = 10);

        /// <summary>
        /// Obtener próximos partidos del jugador
        /// </summary>
        Task<List<PartidoJugadorResponse>> ObtenerProximosPartidosAsync(int id, int limite = 5);

        /// <summary>
        /// Obtener estadísticas detalladas por torneo
        /// </summary>
        Task<List<EstadisticasPorTorneoResponse>> ObtenerEstadisticasPorTorneoAsync(int id);

        /// <summary>
        /// Obtener compañeros de equipo en un torneo específico
        /// </summary>
        Task<List<CompañeroEquipoResponse>> ObtenerCompanerosEquipoAsync(int id, int idTorneo);

        /// <summary>
        /// Solicitar transferencia a otro equipo
        /// </summary>
        Task<SolicitudTransferenciaResponse> SolicitarTransferenciaAsync(int id, SolicitarTransferenciaRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Aprobar/rechazar transferencia (capitán/admin)
        /// </summary>
        Task ProcesarTransferenciaAsync(int solicitudId, int usuarioId, bool aprobada, string? comentario = null, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener estadísticas avanzadas del jugador
        /// </summary>
        Task<EstadisticasAvanzadasResponse> ObtenerEstadisticasAvanzadasAsync(int id, int? idTorneo = null);

        /// <summary>
        /// Obtener resumen de temporada del jugador
        /// </summary>
        Task<ResumenTemporadaResponse> ObtenerResumenTemporadaAsync(int id, int anio);


    }

    public class SuspensionJugadorResponse
    {
        public int Id { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public int PartidosSuspension { get; set; }
        public int PartidosCumplidos { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFinEstimada { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Torneo { get; set; }
        public string? Equipo { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}