using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.Servicios.Interfaces.Notificacion
{
    public interface INotificacionService
    {
        /// <summary>
        /// Obtener notificaciones del usuario (paginado, filtros)
        /// </summary>
        Task<ResultadoPaginado<NotificacionResponse>> ObtenerMisNotificacionesAsync(int usuarioId, FiltrarNotificacionRequest solicitud);

        /// <summary>
        /// Obtener notificación por ID
        /// </summary>
        Task<NotificacionResponse?> ObtenerNotificacionPorIdAsync(int notificacionId, int usuarioId);

        /// <summary>
        /// Marcar notificación como leída
        /// </summary>
        Task MarcarComoLeidaAsync(int notificacionId, int usuarioId);

        /// <summary>
        /// Marcar todas las notificaciones como leídas
        /// </summary>
        Task MarcarTodasComoLeidasAsync(int usuarioId);

        /// <summary>
        /// Archivar notificación
        /// </summary>
        Task ArchivarNotificacionAsync(int notificacionId, int usuarioId);

        /// <summary>
        /// Enviar notificación individual
        /// </summary>
        Task<NotificacionResponse> EnviarNotificacionAsync(int usuarioIdOrigen, EnviarNotificacionRequest solicitud);

        /// <summary>
        /// Enviar notificación masiva (solo administradores)
        /// </summary>
        Task<int> EnviarNotificacionMasivaAsync(int usuarioIdOrigen, NotificacionMasivaRequest solicitud);

        /// <summary>
        /// Obtener preferencias de notificación del usuario
        /// </summary>
        Task<List<PreferenciaNotificacionResponse>> ObtenerPreferenciasAsync(int usuarioId);

        /// <summary>
        /// Actualizar preferencias de notificación del usuario
        /// </summary>
        Task ActualizarPreferenciasAsync(int usuarioId, int idTipoNotificacion, ActualizarPreferenciaRequest solicitud);

        /// <summary>
        /// Procesar notificaciones pendientes (job programado)
        /// </summary>
        Task ProcesarNotificacionesPendientesAsync();

        /// <summary>
        /// Obtener estadísticas de notificaciones del usuario
        /// </summary>
        Task<NotificacionEstadisticasResponse> ObtenerEstadisticasAsync(int usuarioId);

        /// <summary>
        /// Obtener preferencia por tipo de notificación
        /// </summary>
        Task<PreferenciaNotificacionResponse?> ObtenerPreferenciasPorTipoAsync(int usuarioId, int idTipoNotificacion);
    }

    public class NotificacionEstadisticasResponse
    {
        public int TotalNotificaciones { get; set; }
        public int NoLeidas { get; set; }
        public int Archivadas { get; set; }
        public int Ultimas24Horas { get; set; }
        public Dictionary<string, int> PorPrioridad { get; set; } = new();
        public Dictionary<string, int> PorTipo { get; set; } = new();
    }
}