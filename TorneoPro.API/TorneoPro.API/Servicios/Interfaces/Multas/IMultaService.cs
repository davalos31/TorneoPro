using TorneoPro.API.DTOs.Multas.Request;
using TorneoPro.API.DTOs.Multas.Response;
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.Servicios.Interfaces.Multas
{
    public interface IMultaService
    {
        /// <summary>
        /// Listar multas con filtros y paginación
        /// </summary>
        Task<ResultadoPaginado<MultaResponse>> ObtenerTodosAsync(FiltrarMultaRequest solicitud);

        /// <summary>
        /// Obtener multa por ID con detalles
        /// </summary>
        Task<MultaDetalleResponse?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Crear nueva multa manualmente
        /// </summary>
        Task<MultaResponse> CrearMultaAsync(int usuarioId, CrearMultaRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Aplicar multas automáticas por evento (WO, tarjeta roja, etc.)
        /// </summary>
        Task<List<MultaResponse>> AplicarMultasAutomaticasAsync(int partidoId, int? eventoId = null, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Actualizar multa
        /// </summary>
        Task<MultaResponse> ActualizarMultaAsync(int id, int usuarioId, ActualizarMultaRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Cancelar multa
        /// </summary>
        Task CancelarMultaAsync(int id, int usuarioId, string? motivo = null, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Registrar pago de multa
        /// </summary>
        Task<PagoResponse> RegistrarPagoAsync(int multaId, int usuarioId, RegistrarPagoRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Verificar comprobante de pago (admin)
        /// </summary>
        Task<PagoResponse> VerificarPagoAsync(int pagoId, int usuarioId, VerificarPagoRequest solicitud, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Obtener multas de un equipo en un torneo
        /// </summary>
        Task<List<MultaResponse>> ObtenerMultasPorEquipoAsync(int equipoId, int torneoId);

        /// <summary>
        /// Obtener resumen financiero del torneo
        /// </summary>
        Task<ResumenFinancieroResponse> ObtenerResumenFinancieroAsync(int torneoId);

        /// <summary>
        /// Generar comprobante de pago en PDF
        /// </summary>
        Task<byte[]> GenerarComprobantePagoAsync(int pagoId, string baseUrl);

        /// <summary>
        /// Enviar recordatorio de pago (job programado)
        /// </summary>
        Task EnviarRecordatoriosPagoAsync();

        /// <summary>
        /// Marcar multas vencidas automáticamente (job programado)
        /// </summary>
        Task MarcarMultasVencidasAsync();

        Task<object> ObtenerEstadisticasMultasAsync();
        Task<List<MultaResponse>> ObtenerMultasPorJugadorAsync(int jugadorId, int torneoId);
    }

    public class ActualizarMultaRequest
    {
        public decimal? Monto { get; set; }
        public string? Moneda { get; set; }
        public DateTime? FechaLimitePago { get; set; }
        public string? Descripcion { get; set; }
        public string? NotasInternas { get; set; }
        public string? Estado { get; set; }
    }
}