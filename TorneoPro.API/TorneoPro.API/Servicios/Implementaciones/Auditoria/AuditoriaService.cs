using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Auditoria;

namespace TorneoPro.API.Servicios.Implementaciones.Auditoria
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly TorneoProContext _contexto;
        private readonly ILogger<AuditoriaService> _logger;

        public AuditoriaService(TorneoProContext contexto, ILogger<AuditoriaService> logger)
        {
            _contexto = contexto;
            _logger = logger;
        }

        public async Task RegistrarAsync(
            int? usuarioId,
            string accion,
            string? tablaAfectada = null,
            int? idRegistro = null,
            string? datosAnteriores = null,
            string? datosNuevos = null,
            string? ipAddress = null,
            string? userAgent = null,
            bool exitoso = true,
            string? detalleError = null)
        {
            try
            {
                var auditoria = new auditoria_acceso
                {
                    id_usuario = usuarioId,
                    accion = accion,
                    tabla_afectada = tablaAfectada,
                    id_registro = idRegistro,
                    datos_anteriores = datosAnteriores?.Length > 4000 ? datosAnteriores[..4000] : datosAnteriores,
                    datos_nuevos = datosNuevos?.Length > 4000 ? datosNuevos[..4000] : datosNuevos,
                    ip_address = ipAddress,
                    user_agent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
                    fecha_accion = DateTime.UtcNow,
                    exitoso = exitoso,
                    detalle_error = detalleError?.Length > 500 ? detalleError[..500] : detalleError
                };

                await _contexto.auditoria_accesos.AddAsync(auditoria);
                await _contexto.SaveChangesAsync();

                _logger.LogDebug("Auditoría registrada - Acción: {Accion}, Usuario: {UsuarioId}, Exitoso: {Exitoso}",
                    accion, usuarioId, exitoso);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar auditoría - Acción: {Accion}", accion);
            }
        }

        public async Task RegistrarExitoAsync(
            int? usuarioId,
            string accion,
            string? tablaAfectada = null,
            int? idRegistro = null,
            string? datosNuevos = null)
        {
            await RegistrarAsync(
                usuarioId,
                accion,
                tablaAfectada,
                idRegistro,
                null,
                datosNuevos,
                null,
                null,
                true,
                null);
        }

        public async Task RegistrarErrorAsync(
            int? usuarioId,
            string accion,
            string error,
            string? tablaAfectada = null,
            int? idRegistro = null,
            string? datosIntentados = null)
        {
            await RegistrarAsync(
                usuarioId,
                accion,
                tablaAfectada,
                idRegistro,
                datosIntentados,
                null,
                null,
                null,
                false,
                error);
        }

        public async Task RegistrarInicioSesionAsync(
            string email,
            string? ipAddress,
            string? userAgent,
            bool exitoso,
            string? error = null)
        {
            await RegistrarAsync(
                null,
                "INICIO_SESION",
                "usuarios",
                null,
                null,
                $"Email: {email}",
                ipAddress,
                userAgent,
                exitoso,
                error);
        }

        public async Task RegistrarCierreSesionAsync(int usuarioId, string? ipAddress)
        {
            await RegistrarAsync(
                usuarioId,
                "CIERRE_SESION",
                null,
                null,
                null,
                null,
                ipAddress,
                null,
                true,
                null);
        }

        public async Task<List<auditoria_acceso>> ObtenerPorUsuarioAsync(
            int usuarioId,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            var query = _contexto.auditoria_accesos
                .Where(a => a.id_usuario == usuarioId)
                .Include(a => a.id_usuarioNavigation)
                .OrderByDescending(a => a.fecha_accion)
                .AsQueryable();

            if (desde.HasValue)
                query = query.Where(a => a.fecha_accion >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(a => a.fecha_accion <= hasta.Value);

            return await query.ToListAsync();
        }

        public async Task<List<auditoria_acceso>> ObtenerPorAccionAsync(
            string accion,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            var query = _contexto.auditoria_accesos
                .Where(a => a.accion == accion)
                .Include(a => a.id_usuarioNavigation)
                .OrderByDescending(a => a.fecha_accion)
                .AsQueryable();

            if (desde.HasValue)
                query = query.Where(a => a.fecha_accion >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(a => a.fecha_accion <= hasta.Value);

            return await query.ToListAsync();
        }

        public async Task<List<auditoria_acceso>> ObtenerPorTablaAsync(string tabla, int? idRegistro = null)
        {
            var query = _contexto.auditoria_accesos
                .Where(a => a.tabla_afectada == tabla)
                .Include(a => a.id_usuarioNavigation)
                .OrderByDescending(a => a.fecha_accion)
                .AsQueryable();

            if (idRegistro.HasValue)
                query = query.Where(a => a.id_registro == idRegistro.Value);

            return await query.ToListAsync();
        }

        public async Task<ResultadoPaginado<auditoria_acceso>> ObtenerPaginadoAsync(PaginacionAuditoriaRequest solicitud)
        {
            var query = _contexto.auditoria_accesos
                .Include(a => a.id_usuarioNavigation)
                .AsQueryable();

            if (solicitud.UsuarioId.HasValue)
                query = query.Where(a => a.id_usuario == solicitud.UsuarioId.Value);

            if (!string.IsNullOrWhiteSpace(solicitud.Accion))
                query = query.Where(a => a.accion.Contains(solicitud.Accion));

            if (!string.IsNullOrWhiteSpace(solicitud.TablaAfectada))
                query = query.Where(a => a.tabla_afectada == solicitud.TablaAfectada);

            if (solicitud.FechaDesde.HasValue)
                query = query.Where(a => a.fecha_accion >= solicitud.FechaDesde.Value);

            if (solicitud.FechaHasta.HasValue)
                query = query.Where(a => a.fecha_accion <= solicitud.FechaHasta.Value);

            if (solicitud.Exitoso.HasValue)
                query = query.Where(a => a.exitoso == solicitud.Exitoso.Value);

            var totalItems = await query.CountAsync();

            query = query.OrderByDescending(a => a.fecha_accion);

            var items = await query
                .Skip((solicitud.Pagina - 1) * solicitud.TamanoPagina)
                .Take(solicitud.TamanoPagina)
                .ToListAsync();

            return ResultadoPaginado<auditoria_acceso>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
        }

        /// <summary>
        /// Método auxiliar para serializar datos a JSON (para guardar en auditoría)
        /// </summary>
        public static string? SerializarDatos(object? datos)
        {
            if (datos == null) return null;

            try
            {
                return JsonSerializer.Serialize(datos, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
            }
            catch
            {
                return datos.ToString();
            }
        }

        public async Task RegistrarErrorAsync(
    int? usuarioId,
    string accion,
    string error,
    string? tablaAfectada = null,
    int? idRegistro = null,
    string? datosIntentados = null,
    string? ipAddress = null,
    string? userAgent = null)
        {
            await RegistrarAsync(
                usuarioId,
                accion,
                tablaAfectada,
                idRegistro,
                datosIntentados,
                null,
                ipAddress,
                userAgent,
                false,
                error);
        }
    }
}