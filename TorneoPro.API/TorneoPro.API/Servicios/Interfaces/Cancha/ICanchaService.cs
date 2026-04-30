using TorneoPro.API.DTOs.Canchas;
using TorneoPro.API.DTOs.Canchas.Response;
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.Servicios.Interfaces.Cancha
{
    public interface ICanchaService
    {
        /// <summary>
        /// Listar canchas con filtros y paginación
        /// </summary>
        Task<ResultadoPaginado<CanchaResponse>> ObtenerTodosAsync(FiltrarCanchaRequest solicitud);

        /// <summary>
        /// Obtener cancha por ID con detalles
        /// </summary>
        Task<CanchaDetalleResponse?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Crear nueva cancha
        /// </summary>
        Task<CanchaResponse> CrearAsync(int usuarioId, CrearCanchaRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Actualizar cancha
        /// </summary>
        Task<CanchaResponse> ActualizarAsync(int id, int usuarioId, ActualizarCanchaRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Subir foto de cancha
        /// </summary>
        Task<string> SubirFotoAsync(int id, int usuarioId, IFormFile foto, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Actualizar foto de cancha (reemplaza la anterior)
        /// </summary>
        Task<string> ActualizarFotoAsync(int id, int usuarioId, IFormFile foto, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Eliminar foto de cancha
        /// </summary>
        Task EliminarFotoAsync(int id, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Desactivar cancha
        /// </summary>
        Task DesactivarAsync(int id, int usuarioId, string? motivo = null, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Activar cancha
        /// </summary>
        Task ActivarAsync(int id, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener disponibilidad de cancha
        /// </summary>
        Task<List<DisponibilidadResponse>> ObtenerDisponibilidadAsync(int id, DateTime fechaInicio, DateTime fechaFin);

        /// <summary>
        /// Bloquear fecha/horario de cancha
        /// </summary>
        Task<BloqueoResponse> BloquearFechaAsync(int id, int usuarioId, BloquearFechaRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Eliminar bloqueo de cancha
        /// </summary>
        Task EliminarBloqueoAsync(int canchaId, int bloqueoId, int usuarioId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Buscar canchas disponibles en un rango de fechas
        /// </summary>
        Task<List<CanchaResponse>> BuscarCanchasDisponiblesAsync(BuscarCanchaRequest solicitud);
    }
}