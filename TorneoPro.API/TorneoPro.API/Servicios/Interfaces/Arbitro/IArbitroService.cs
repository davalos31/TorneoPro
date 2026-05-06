using TorneoPro.API.DTOs.Arbitros;
using TorneoPro.API.DTOs.Arbitros.Request;
using TorneoPro.API.DTOs.Arbitros.Response;
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.Servicios.Interfaces.Arbitro
{
    public interface IArbitroService
    {
        /// <summary>
        /// Listar árbitros con filtros y paginación
        /// </summary>
        Task<ResultadoPaginado<ArbitroResponse>> ObtenerTodosAsync(FiltrarArbitroRequest solicitud);

        /// <summary>
        /// Obtener árbitro por ID
        /// </summary>
        Task<ArbitroResponse?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Registrar nuevo árbitro (crea usuario con rol ARBITRO)
        /// </summary>
        Task<ArbitroResponse> RegistrarArbitroAsync(int usuarioIdAdmin, RegistrarArbitroRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Actualizar datos del árbitro
        /// </summary>
        Task<ArbitroResponse> ActualizarArbitroAsync(int id, int usuarioId, ActualizarArbitroRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Asignar árbitro a un partido
        /// </summary>
        Task AsignarAPartidoAsync(int arbitroId, AsignarArbitroRequest solicitud, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Desasignar árbitro de un partido
        /// </summary>
        Task DesasignarDePartidoAsync(int arbitroId, int partidoId, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener partidos asignados a un árbitro
        /// </summary>
        Task<List<PartidoAsignadoResponse>> ObtenerPartidosAsignadosAsync(int arbitroId, string? estado = null);

        /// <summary>
        /// Registrar disponibilidad del árbitro
        /// </summary>
        Task RegistrarDisponibilidadAsync(int arbitroId, DisponibilidadArbitroRequest solicitud, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener disponibilidad del árbitro
        /// </summary>
        Task<List<DisponibilidadArbitroResponse>> ObtenerDisponibilidadAsync(int arbitroId, DateTime? fechaDesde = null, DateTime? fechaHasta = null);

        /// <summary>
        /// Calificar actuación del árbitro
        /// </summary>
        Task<CalificacionArbitroResponse> CalificarArbitroAsync(int arbitroId, CalificacionArbitroRequest solicitud, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener calificaciones del árbitro
        /// </summary>
        Task<List<CalificacionArbitroResponse>> ObtenerCalificacionesAsync(int arbitroId);

        /// <summary>
        /// Obtener estadísticas del árbitro
        /// </summary>
        Task<EstadisticasArbitroResponse> ObtenerEstadisticasAsync(int arbitroId);

        /// <summary>
        /// Buscar árbitros disponibles para una fecha
        /// </summary>
        Task<List<ArbitroResponse>> BuscarArbitrosDisponiblesAsync(DateTime fechaHora, string? especialidad = null);

        /// <summary>
        /// Elimina disponibilidad del arbitro
        /// </summary>
        Task EliminarDisponibilidadAsync(int arbitroId, DateTime fechaHora, int usuarioId, string? ipAddress = null, string? userAgent = null);
    }

    public class DisponibilidadArbitroResponse
    {
        public DateTime FechaHora { get; set; }
        public bool Disponible { get; set; }
        public string? MotivoNoDisponibilidad { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}