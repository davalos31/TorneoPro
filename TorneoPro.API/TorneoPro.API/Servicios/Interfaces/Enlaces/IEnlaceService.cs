using TorneoPro.API.DTOs.Enlaces;
using TorneoPro.API.DTOs.Enlaces.Request;
using TorneoPro.API.DTOs.Enlaces.Response;
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.Servicios.Interfaces.Enlaces
{
    public interface IEnlaceService
    {
        /// <summary>
        /// Obtener enlaces del usuario (paginado)
        /// </summary>
        Task<ResultadoPaginado<EnlaceResponse>> ObtenerMisEnlacesAsync(int usuarioId, FiltrarEnlaceRequest solicitud);

        /// <summary>
        /// Obtener enlace por ID
        /// </summary>
        Task<EnlaceResponse?> ObtenerEnlacePorIdAsync(int id, int usuarioId);

        /// <summary>
        /// Crear enlace de capitán
        /// </summary>
        Task<EnlaceResponse> CrearEnlaceCapitanAsync(int usuarioId, CrearEnlaceRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Crear enlace de árbitro
        /// </summary>
        Task<EnlaceResponse> CrearEnlaceArbitroAsync(int usuarioId, CrearEnlaceRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Crear enlace de subadministrador
        /// </summary>
        Task<EnlaceResponse> CrearEnlaceSubAdminAsync(int usuarioId, CrearEnlaceRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Crear enlace de jugador
        /// </summary>
        Task<EnlaceResponse> CrearEnlaceJugadorAsync(int usuarioId, CrearEnlaceRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener información pública del enlace (preview)
        /// </summary>
        Task<EnlaceInfoResponse> ObtenerInfoEnlaceAsync(string codigoEnlace);

        /// <summary>
        /// Usar enlace - asigna rol al usuario
        /// </summary>
        Task<RolAsignadoResponse> UsarEnlaceAsync(string codigoEnlace, int usuarioId, string? ipAddress, string? userAgent);

        /// <summary>
        /// Desactivar enlace
        /// </summary>
        Task DesactivarEnlaceAsync(int id, int usuarioId, string? motivo = null);

        /// <summary>
        /// Obtener historial de usos del enlace
        /// </summary>
        Task<List<UsoEnlaceResponse>> ObtenerHistorialUsosAsync(int enlaceId, int usuarioId);

        /// <summary>
        /// Renovar enlace expirado
        /// </summary>
        Task<EnlaceResponse> RenovarEnlaceAsync(int id, int usuarioId, DateTime? nuevaFechaExpiracion = null, int? nuevoMaxUsos = null);


        /// <summary>
        /// Obtener lista de tipos de enlace disponibles
        /// </summary>
        Task<List<TipoEnlaceResponse>> ObtenerTiposEnlaceAsync();

        /// <summary>
        /// Regenerar el código de un enlace existente
        /// </summary>
        Task<EnlaceResponse> RegenerarCodigoEnlaceAsync(int id, int usuarioId, string? ipAddress = null);

        /// <summary>
        /// Obtener estadísticas generales de enlaces del usuario
        /// </summary>
        Task<EnlaceEstadisticasResponse> ObtenerEstadisticasAsync(int usuarioId);
    }

    public class RolAsignadoResponse
    {
        public int IdRol { get; set; }
        public string Rol { get; set; } = string.Empty;
        public int? IdTorneo { get; set; }
        public string? Torneo { get; set; }
        public int? IdEquipo { get; set; }
        public string? Equipo { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }
}