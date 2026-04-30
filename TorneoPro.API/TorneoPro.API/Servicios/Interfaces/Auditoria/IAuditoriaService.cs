using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Models;

namespace TorneoPro.API.Servicios.Interfaces.Auditoria
{
    public interface IAuditoriaService
    {
        /// <summary>
        /// Registrar una acción de auditoría
        /// </summary>
        Task RegistrarAsync(
            int? usuarioId,
            string accion,
            string? tablaAfectada = null,
            int? idRegistro = null,
            string? datosAnteriores = null,
            string? datosNuevos = null,
            string? ipAddress = null,
            string? userAgent = null,
            bool exitoso = true,
            string? detalleError = null);

        /// <summary>
        /// Registrar una acción exitosa
        /// </summary>
        Task RegistrarExitoAsync(
            int? usuarioId,
            string accion,
            string? tablaAfectada = null,
            int? idRegistro = null,
            string? datosNuevos = null);

        /// <summary>
        /// Registrar una acción fallida
        /// </summary>
        Task RegistrarErrorAsync(
            int? usuarioId,
            string accion,
            string error,
            string? tablaAfectada = null,
            int? idRegistro = null,
            string? datosIntentados = null);

        /// <summary>
        /// Registrar inicio de sesión
        /// </summary>
        Task RegistrarInicioSesionAsync(string email, string? ipAddress, string? userAgent, bool exitoso, string? error = null);

        /// <summary>
        /// Registrar cierre de sesión
        /// </summary>
        Task RegistrarCierreSesionAsync(int usuarioId, string? ipAddress);

        /// <summary>
        /// Obtener historial de auditoría por usuario
        /// </summary>
        Task<List<auditoria_acceso>> ObtenerPorUsuarioAsync(int usuarioId, DateTime? desde = null, DateTime? hasta = null);

        /// <summary>
        /// Obtener historial de auditoría por acción
        /// </summary>
        Task<List<auditoria_acceso>> ObtenerPorAccionAsync(string accion, DateTime? desde = null, DateTime? hasta = null);

        /// <summary>
        /// Obtener historial de auditoría por tabla afectada
        /// </summary>
        Task<List<auditoria_acceso>> ObtenerPorTablaAsync(string tabla, int? idRegistro = null);

        /// <summary>
        /// Obtener historial de auditoría paginado
        /// </summary>
        Task<ResultadoPaginado<auditoria_acceso>> ObtenerPaginadoAsync(PaginacionAuditoriaRequest solicitud);

        /// <summary>
        /// Registrar error con IP y UserAgent
        /// </summary>
        Task RegistrarErrorAsync(
            int? usuarioId,
            string accion,
            string error,
            string? tablaAfectada = null,
            int? idRegistro = null,
            string? datosIntentados = null,
            string? ipAddress = null,
            string? userAgent = null);

    }

    public class PaginacionAuditoriaRequest : PaginacionRequest
    {
        public int? UsuarioId { get; set; }
        public string? Accion { get; set; }
        public string? TablaAfectada { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public bool? Exitoso { get; set; }
    }
}