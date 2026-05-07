using TorneoPro.API.DTOs.Equipo.Response;
using TorneoPro.API.DTOs.Equipos.Request;
using TorneoPro.API.DTOs.Equipos.Response;
using TorneoPro.API.DTOs.Jugadores.Request;
using TorneoPro.API.DTOs.Shared;
using ActualizarJugadorRequest = TorneoPro.API.DTOs.Equipos.Request.ActualizarJugadorRequest;

namespace TorneoPro.API.Servicios.Interfaces.Equipo
{
    public interface IEquipoService
    {
        /// <summary>
        /// Listar equipos con filtros y paginación
        /// </summary>
        Task<ResultadoPaginado<EquipoResponse>> ObtenerTodosAsync(EquipoFilterRequest solicitud);

        /// <summary>
        /// Obtener equipo por ID con detalles completos
        /// </summary>
        Task<EquipoDetalleResponse?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Crear un nuevo equipo
        /// </summary>
        Task<EquipoResponse> CrearAsync(int usuarioId, CrearEquipoRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Actualizar datos del equipo
        /// </summary>
        Task<EquipoResponse> ActualizarAsync(int id, int usuarioId, ActualizarEquipoRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Subir escudo del equipo
        /// </summary>
        Task<string> SubirEscudoAsync(int id, int usuarioId, IFormFile archivo, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Actualizar escudo del equipo (elimina el anterior y sube el nuevo)
        /// </summary>
        Task<string> ActualizarEscudoAsync(int id, int usuarioId, IFormFile archivo, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Eliminar escudo del equipo
        /// </summary>
        Task EliminarEscudoAsync(int id, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Inscribir equipo en un torneo
        /// </summary>
        Task<EquipoTorneoResponse> InscribirEnTorneoAsync(int equipoId, int torneoId, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Aprobar inscripción de equipo en torneo (admin)
        /// </summary>
        Task AprobarInscripcionAsync(int equipoId, int torneoId, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener jugadores del equipo
        /// </summary>
        Task<List<JugadorEquipoResponse>> ObtenerJugadoresAsync(int equipoId);

        /// <summary>
        /// Agregar jugador al equipo
        /// </summary>
        Task<JugadorEquipoResponse> AgregarJugadorAsync(int equipoId, int usuarioId, AgregarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Actualizar datos del jugador en el equipo
        /// </summary>
        Task<JugadorEquipoResponse> ActualizarJugadorAsync(int equipoId, int jugadorId, int usuarioId, ActualizarJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Remover jugador del equipo
        /// </summary>
        Task RemoverJugadorAsync(int equipoId, int jugadorId, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Cambiar capitán del equipo
        /// </summary>
        Task CambiarCapitanAsync(int equipoId, int nuevoCapitanId, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Autorizar jugador (validar documentación)
        /// </summary>
        Task AutorizarJugadorAsync(int equipoId, int jugadorId, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Suspender jugador por N partidos
        /// </summary>
        Task<SuspensionResponse> SuspenderJugadorAsync(int equipoId, int jugadorId, int usuarioId, DTOs.Equipos.Request.SuspenderJugadorRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener historial de torneos del equipo
        /// </summary>
        Task<List<HistorialTorneoResponse>> ObtenerHistorialAsync(int equipoId);
        /// <summary>
        /// Obtener equipos por torneo
        /// </summary>
        Task<List<EquipoResponse>> ObtenerEquiposPorTorneoAsync(int torneoId);

        /// <summary>
        /// Obtener estadísticas del equipo
        /// </summary>
        Task<EquipoEstadisticasResponse> ObtenerEstadisticasAsync(int equipoId, int? idTorneo = null);

        /// <summary>
        /// Obtener calendario de partidos del equipo
        /// </summary>
        Task<List<PartidoEquipoResponse>> ObtenerCalendarioAsync(int equipoId, int? idTorneo = null, int limite = 10);

        /// <summary>
        /// Solicitar unión al equipo
        /// </summary>
        Task<SolicitudUnionResponse> SolicitarUnionAsync(int equipoId, int usuarioId, string? mensaje = null, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener estadísticas de jugadores del equipo
        /// </summary>
        Task<List<JugadorEstadisticaEquipoResponse>> ObtenerEstadisticasJugadoresAsync(int equipoId, int idTorneo);

        /// <summary>
        /// Validar número de camiseta único en el equipo
        /// </summary>
        Task<bool> ValidarNumeroCamisetaAsync(int equipoId, int numero, int? jugadorExcluirId = null);

        /// <summary>
        /// Desactivar equipo
        /// </summary>
        Task DesactivarEquipoAsync(int equipoId, int usuarioId, string? motivo = null, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Activar equipo
        /// </summary>
        Task ActivarEquipoAsync(int equipoId, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener invitaciones pendientes del equipo
        /// </summary>
        Task<List<InvitacionEquipoResponse>> ObtenerInvitacionesPendientesAsync(int equipoId, int usuarioId);

        /// <summary>
        /// Procesar solicitud de unión (aprobar/rechazar)
        /// </summary>
        Task ProcesarSolicitudUnionAsync(int solicitudId, int usuarioId, bool aprobada, string? comentario = null, string? ipAddress = null, string? userAgent = null);
    }
}
