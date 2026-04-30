
using TorneoPro.API.DTOs.Estadisticas.Response;

namespace TorneoPro.API.Interfaces.Estadistica_Vivo
{
    public interface IEstadisticaEnVivoService
    {
        /// <summary>
        /// Obtiene estadísticas en vivo de un partido
        /// </summary>
        Task<EstadisticaPartidoResponse> ObtenerEstadisticasEnVivoAsync(int partidoId);

        /// <summary>
        /// Inicia el servicio de actualización automática
        /// </summary>
        void IniciarServicio();

        /// <summary>
        /// Detiene el servicio de actualización automática
        /// </summary>
        void DetenerServicio();
    }
}