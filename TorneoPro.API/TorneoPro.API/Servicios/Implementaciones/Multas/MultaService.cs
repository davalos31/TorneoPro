using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.Multas;
using TorneoPro.API.DTOs.Multas.Request;
using TorneoPro.API.DTOs.Multas.Response;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Email;
using TorneoPro.API.Servicios.Interfaces.Multas;
using TorneoPro.API.Servicios.Interfaces.Notificacion;

namespace TorneoPro.API.Servicios.Implementaciones.Multas
{
    public class MultaService : IMultaService
    {
        #region ========== CAMPOS Y CONSTRUCTOR ==========

        private readonly TorneoProContext _contexto;
        private readonly ILogger<MultaService> _logger;
        private readonly IConfiguration _configuracion;
        private readonly INotificacionService _notificacionService;
        private readonly IEmailService _emailService;
        private readonly ArchivosHelper _archivosHelper;
        private readonly QRHelper _qrHelper;

        public MultaService(
            TorneoProContext contexto,
            ILogger<MultaService> logger,
            IConfiguration configuracion,
            INotificacionService notificacionService,
            IEmailService emailService,
            ArchivosHelper archivosHelper,
            QRHelper qrHelper)
        {
            _contexto = contexto;
            _logger = logger;
            _configuracion = configuracion;
            _notificacionService = notificacionService;
            _emailService = emailService;
            _archivosHelper = archivosHelper;
            _qrHelper = qrHelper;
        }

        #endregion

        #region ========== MÉTODOS PRIVADOS ==========

        private async Task<bool> EsAdminAsync(int usuarioId)
        {
            return await _contexto.usuarios_roles
                .AnyAsync(ur => ur.id_usuario == usuarioId &&
                               (ur.id_rol == 1 || ur.id_rol == 2) &&
                               ur.estado == "ACTIVO");
        }

        private async Task<int> ObtenerSiguienteSecuenciaMulta(int torneoId)
        {
            var ultimaMulta = await _contexto.multas
                .Where(m => m.id_torneo == torneoId)
                .OrderByDescending(m => m.id)
                .FirstOrDefaultAsync();

            if (ultimaMulta == null)
                return 1;

            var partes = ultimaMulta.codigo.Split('-');
            if (partes.Length >= 4 && int.TryParse(partes[3], out var secuencia))
                return secuencia + 1;

            return 1;
        }

        private MultaResponse MapearMultaResponse(multa multa)
        {
            var estaVencida = multa.fecha_limite_pago.HasValue &&
                              multa.fecha_limite_pago.Value < DateOnly.FromDateTime(DateTime.UtcNow) &&
                              multa.estado != "PAGADA" &&
                              multa.estado != "CANCELADA";

            return new MultaResponse
            {
                Id = multa.id,
                Codigo = multa.codigo,
                IdTorneo = multa.id_torneo,
                Torneo = multa.id_torneoNavigation?.nombre ?? "",
                IdTipoMulta = multa.id_tipo_multa,
                TipoMulta = multa.id_tipo_multaNavigation?.nombre ?? "",
                Categoria = multa.id_tipo_multaNavigation?.categoria ?? "",
                IdEquipo = multa.id_equipo,
                Equipo = multa.id_equipoNavigation?.nombre,
                IdJugador = multa.id_jugador,
                Jugador = multa.id_jugadorNavigation != null
                    ? $"{multa.id_jugadorNavigation.nombres} {multa.id_jugadorNavigation.apellidos}"
                    : null,
                Monto = multa.monto,
                MontoPagado = multa.monto_pagado ?? 0,
                SaldoPendiente = multa.monto - (multa.monto_pagado ?? 0),
                Moneda = multa.moneda ?? "Bs",
                Estado = multa.estado ?? "PENDIENTE",
                FechaLimitePago = multa.fecha_limite_pago.HasValue
                    ? multa.fecha_limite_pago.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,
                FechaAplicacion = multa.fecha_aplicacion ?? DateTime.UtcNow,
                Descripcion = multa.descripcion,
                EstaVencida = estaVencida
            };
        }

        private MultaDetalleResponse MapearMultaDetalleResponse(multa multa)
        {
            var baseResponse = MapearMultaResponse(multa);

            return new MultaDetalleResponse
            {
                Id = baseResponse.Id,
                Codigo = baseResponse.Codigo,
                IdTorneo = baseResponse.IdTorneo,
                Torneo = baseResponse.Torneo,
                IdTipoMulta = baseResponse.IdTipoMulta,
                TipoMulta = baseResponse.TipoMulta,
                Categoria = baseResponse.Categoria,
                IdEquipo = baseResponse.IdEquipo,
                Equipo = baseResponse.Equipo,
                IdJugador = baseResponse.IdJugador,
                Jugador = baseResponse.Jugador,
                Monto = baseResponse.Monto,
                MontoPagado = baseResponse.MontoPagado,
                SaldoPendiente = baseResponse.SaldoPendiente,
                Moneda = baseResponse.Moneda,
                Estado = baseResponse.Estado,
                FechaLimitePago = baseResponse.FechaLimitePago,
                FechaAplicacion = baseResponse.FechaAplicacion,
                Descripcion = baseResponse.Descripcion,
                EstaVencida = baseResponse.EstaVencida,
                IdPartido = multa.id_partido,
                Partido = multa.id_partidoNavigation != null
                    ? $"{multa.id_partidoNavigation.id_equipo_localNavigation?.nombre} vs {multa.id_partidoNavigation.id_equipo_visitanteNavigation?.nombre}"
                    : null,
                IdEvento = multa.id_evento,
                GeneradaAutomaticamente = multa.generada_automaticamente ?? false,
                NotasInternas = multa.notas_internas,
                HistorialPagos = new List<PagoResponse>()
            };
        }

        private async Task EnviarNotificacionMultaAsync(multa multa)
        {
            var destinatarioId = multa.id_equipoNavigation?.id_capitan ?? multa.id_jugador;
            if (!destinatarioId.HasValue) return;

            await _notificacionService.EnviarNotificacionAsync(1, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = destinatarioId.Value,
                IdTipoNotificacion = 12,
                Titulo = "Nueva multa registrada",
                Mensaje = $"Se ha registrado una multa con código {multa.codigo} por un monto de {multa.monto} {multa.moneda}. Fecha límite: {multa.fecha_limite_pago:dd/MM/yyyy}",
                IdTorneo = multa.id_torneo,
                IdEquipo = multa.id_equipo,
                Prioridad = "ALTA"
            });
        }

        private async Task NotificarNuevoPagoPendienteAsync(multas_historial_pago pago, multa multa)
        {
            var admins = await _contexto.usuarios_roles
                .Where(ur => ur.id_rol == 1 || ur.id_rol == 2)
                .Select(ur => ur.id_usuario)
                .ToListAsync();

            foreach (var adminId in admins)
            {
                await _notificacionService.EnviarNotificacionAsync(1, new EnviarNotificacionRequest
                {
                    IdUsuarioDestino = adminId,
                    IdTipoNotificacion = 15,
                    Titulo = "Pago pendiente de verificación",
                    Mensaje = $"Se ha registrado un pago para la multa {multa.codigo} por {pago.monto_pagado} {pago.moneda}. Requiere verificación.",
                    Prioridad = "MEDIA"
                });
            }
        }

        private async Task EnviarComprobantePagoAsync(multas_historial_pago pago, multa multa)
        {
            var destinatarioId = multa.id_equipoNavigation?.id_capitan ?? multa.id_jugador;
            if (!destinatarioId.HasValue) return;

            await _notificacionService.EnviarNotificacionAsync(1, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = destinatarioId.Value,
                IdTipoNotificacion = 16,
                Titulo = "Pago de multa confirmado",
                Mensaje = $"El pago de {pago.monto_pagado} {pago.moneda} para la multa {multa.codigo} ha sido confirmado.",
                Prioridad = "MEDIA"
            });
        }

        private async Task NotificarPagoRechazadoAsync(multas_historial_pago pago, multa multa)
        {
            var destinatarioId = multa.id_equipoNavigation?.id_capitan ?? multa.id_jugador;
            if (!destinatarioId.HasValue) return;

            await _notificacionService.EnviarNotificacionAsync(1, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = destinatarioId.Value,
                IdTipoNotificacion = 17,
                Titulo = "Pago de multa rechazado",
                Mensaje = $"El pago de {pago.monto_pagado} {pago.moneda} para la multa {multa.codigo} ha sido rechazado. Motivo: {pago.motivo_rechazo ?? "No especificado"}",
                Prioridad = "URGENTE"
            });
        }

        #endregion

        #region ========== CRUD DE MULTAS ==========

        public async Task<ResultadoPaginado<MultaResponse>> ObtenerTodosAsync(FiltrarMultaRequest solicitud)
        {
            var query = _contexto.multas
                .Include(m => m.id_torneoNavigation)
                .Include(m => m.id_tipo_multaNavigation)
                .Include(m => m.id_equipoNavigation)
                .Include(m => m.id_jugadorNavigation)
                .AsQueryable();

            if (solicitud.IdTorneo.HasValue)
                query = query.Where(m => m.id_torneo == solicitud.IdTorneo.Value);

            if (solicitud.IdEquipo.HasValue)
                query = query.Where(m => m.id_equipo == solicitud.IdEquipo.Value);

            if (solicitud.IdJugador.HasValue)
                query = query.Where(m => m.id_jugador == solicitud.IdJugador.Value);

            if (solicitud.IdTipoMulta.HasValue)
                query = query.Where(m => m.id_tipo_multa == solicitud.IdTipoMulta.Value);

            if (!string.IsNullOrWhiteSpace(solicitud.Estado))
                query = query.Where(m => m.estado == solicitud.Estado);

            if (solicitud.Pagadas == true)
                query = query.Where(m => m.estado == "PAGADA" || m.monto_pagado >= m.monto);

            if (solicitud.Vencidas == true)
                query = query.Where(m => m.fecha_limite_pago < DateOnly.FromDateTime(DateTime.UtcNow) && m.estado != "PAGADA" && m.estado != "CANCELADA");

            if (solicitud.FechaDesde.HasValue)
                query = query.Where(m => m.fecha_aplicacion >= solicitud.FechaDesde.Value);

            if (solicitud.FechaHasta.HasValue)
                query = query.Where(m => m.fecha_aplicacion <= solicitud.FechaHasta.Value);

            var totalItems = await query.CountAsync();

            query = solicitud.OrdenDescendente
                ? query.OrderByDescending(m => m.fecha_aplicacion)
                : query.OrderBy(m => m.fecha_aplicacion);

            var multas = await query
                .Skip((solicitud.Pagina - 1) * solicitud.TamanoPagina)
                .Take(solicitud.TamanoPagina)
                .ToListAsync();

            var items = multas.Select(MapearMultaResponse).ToList();

            return ResultadoPaginado<MultaResponse>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
        }

        public async Task<MultaDetalleResponse?> ObtenerPorIdAsync(int id)
        {
            var multa = await _contexto.multas
                .Include(m => m.id_torneoNavigation)
                .Include(m => m.id_tipo_multaNavigation)
                .Include(m => m.id_equipoNavigation)
                .Include(m => m.id_jugadorNavigation)
                .Include(m => m.id_partidoNavigation)
                .Include(m => m.multas_historial_pagos)
                    .ThenInclude(p => p.id_metodo_pagoNavigation)
                .FirstOrDefaultAsync(m => m.id == id);

            if (multa == null)
                return null;

            var response = MapearMultaDetalleResponse(multa);

            response.HistorialPagos = multa.multas_historial_pagos?.Select(p => new PagoResponse
            {
                Id = p.id,
                Codigo = p.codigo,
                IdMulta = p.id_multa,
                IdMetodoPago = p.id_metodo_pago,
                MetodoPago = p.id_metodo_pagoNavigation?.nombre ?? "",
                MontoPagado = p.monto_pagado,
                Moneda = p.moneda ?? "Bs",
                FechaPago = p.fecha_pago ?? DateTime.UtcNow,
                CodigoTransaccion = p.codigo_transaccion,
                ComprobanteUrl = p.comprobante_url,
                ComprobanteVerificado = p.comprobante_verificado ?? false,
                FechaVerificacion = p.fecha_verificacion,
                Estado = p.estado ?? "PROCESANDO",
                MotivoRechazo = p.motivo_rechazo
            }).ToList() ?? new();

            return response;
        }

        public async Task<MultaResponse> CrearMultaAsync(int usuarioId, CrearMultaRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para crear multas");

            var torneo = await _contexto.torneos.FindAsync(solicitud.IdTorneo);
            if (torneo == null)
                throw new KeyNotFoundException("Torneo no encontrado");

            var tipoMulta = await _contexto.tipos_multa.FindAsync(solicitud.IdTipoMulta);
            if (tipoMulta == null)
                throw new KeyNotFoundException("Tipo de multa no encontrado");

            if (solicitud.IdEquipo.HasValue && !await _contexto.equipos.AnyAsync(e => e.id == solicitud.IdEquipo.Value))
                throw new KeyNotFoundException("Equipo no encontrado");

            if (solicitud.IdJugador.HasValue && !await _contexto.usuarios.AnyAsync(u => u.id == solicitud.IdJugador.Value))
                throw new KeyNotFoundException("Jugador no encontrado");

            if (solicitud.IdPartido.HasValue && !await _contexto.partidos.AnyAsync(p => p.id == solicitud.IdPartido.Value))
                throw new KeyNotFoundException("Partido no encontrado");

            // Validar que no se aplique multa a un equipo y jugador simultáneamente (opcional)
            if (solicitud.IdEquipo.HasValue && solicitud.IdJugador.HasValue)
                throw new InvalidOperationException("Una multa no puede estar asociada a un equipo y a un jugador simultáneamente");

            var multa = new multa
            {
                codigo = CodigoHelper.GenerarCodigoMulta(solicitud.IdTorneo, solicitud.IdTipoMulta, await ObtenerSiguienteSecuenciaMulta(solicitud.IdTorneo)),
                id_torneo = solicitud.IdTorneo,
                id_tipo_multa = solicitud.IdTipoMulta,
                id_equipo = solicitud.IdEquipo,
                id_jugador = solicitud.IdJugador,
                id_partido = solicitud.IdPartido,
                id_evento = solicitud.IdEvento,
                generada_automaticamente = solicitud.GeneradaAutomaticamente,
                monto = solicitud.Monto,
                monto_pagado = 0,
                moneda = solicitud.Moneda ?? "Bs",
                estado = "PENDIENTE",
                fecha_limite_pago = solicitud.FechaLimitePago.HasValue ? DateOnly.FromDateTime(solicitud.FechaLimitePago.Value) : null,
                fecha_aplicacion = DateTime.UtcNow,
                descripcion = solicitud.Descripcion,
                notas_internas = solicitud.NotasInternas,
                fecha_creacion = DateTime.UtcNow
            };

            _contexto.multas.Add(multa);
            await _contexto.SaveChangesAsync();

            await EnviarNotificacionMultaAsync(multa);

            return MapearMultaResponse(multa);
        }

        public async Task<MultaResponse> ActualizarMultaAsync(int id, int usuarioId, ActualizarMultaRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para actualizar multas");

            var multa = await _contexto.multas.FindAsync(id);
            if (multa == null)
                throw new KeyNotFoundException("Multa no encontrada");

            if (solicitud.Monto.HasValue)
            {
                if (solicitud.Monto.Value <= 0)
                    throw new InvalidOperationException("El monto debe ser mayor a 0");
                multa.monto = solicitud.Monto.Value;
            }

            if (!string.IsNullOrWhiteSpace(solicitud.Moneda))
                multa.moneda = solicitud.Moneda;

            if (solicitud.FechaLimitePago.HasValue)
                multa.fecha_limite_pago = DateOnly.FromDateTime(solicitud.FechaLimitePago.Value);

            if (!string.IsNullOrWhiteSpace(solicitud.Descripcion))
                multa.descripcion = solicitud.Descripcion;

            if (!string.IsNullOrWhiteSpace(solicitud.NotasInternas))
                multa.notas_internas = solicitud.NotasInternas;

            if (!string.IsNullOrWhiteSpace(solicitud.Estado))
            {
                var estadosValidos = new[] { "PENDIENTE", "PAGADA", "PARCIAL", "VENCIDA", "CANCELADA" };
                if (!estadosValidos.Contains(solicitud.Estado))
                    throw new InvalidOperationException($"Estado no válido. Estados permitidos: {string.Join(", ", estadosValidos)}");
                multa.estado = solicitud.Estado;
            }

            await _contexto.SaveChangesAsync();

            return MapearMultaResponse(multa);
        }

        public async Task CancelarMultaAsync(int id, int usuarioId, string? motivo = null, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para cancelar multas");

            var multa = await _contexto.multas.FindAsync(id);
            if (multa == null)
                throw new KeyNotFoundException("Multa no encontrada");

            if (multa.estado == "PAGADA")
                throw new InvalidOperationException("No se puede cancelar una multa que ya ha sido pagada");

            multa.estado = "CANCELADA";
            multa.notas_internas = (multa.notas_internas ?? "") + $"\n[Cancelada] Por usuario {usuarioId}. Motivo: {motivo ?? "No especificado"} - Fecha: {DateTime.UtcNow:yyyy-MM-dd HH:mm}";

            await _contexto.SaveChangesAsync();

            var destinatarioId = multa.id_equipoNavigation?.id_capitan ?? multa.id_jugador;
            if (destinatarioId.HasValue)
            {
                await _notificacionService.EnviarNotificacionAsync(usuarioId, new EnviarNotificacionRequest
                {
                    IdUsuarioDestino = destinatarioId.Value,
                    IdTipoNotificacion = 13,
                    Titulo = "Multa cancelada",
                    Mensaje = $"La multa con código {multa.codigo} ha sido cancelada. Motivo: {motivo ?? "No especificado"}",
                    IdTorneo = multa.id_torneo,
                    IdEquipo = multa.id_equipo,
                    Prioridad = "MEDIA"
                });
            }
        }

        #endregion

        #region ========== GESTIÓN DE PAGOS ==========

        public async Task<PagoResponse> RegistrarPagoAsync(int multaId, int usuarioId, RegistrarPagoRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var multa = await _contexto.multas
                .Include(m => m.id_equipoNavigation)
                .Include(m => m.id_jugadorNavigation)
                .FirstOrDefaultAsync(m => m.id == multaId);

            if (multa == null)
                throw new KeyNotFoundException("Multa no encontrada");

            if (multa.estado == "CANCELADA")
                throw new InvalidOperationException("No se puede pagar una multa cancelada");

            if (multa.estado == "PAGADA")
                throw new InvalidOperationException("La multa ya está completamente pagada");

            var esAdmin = await EsAdminAsync(usuarioId);
            var esCapitan = multa.id_equipoNavigation?.id_capitan == usuarioId;
            var esMismoJugador = multa.id_jugador == usuarioId;

            if (!esAdmin && !esCapitan && !esMismoJugador)
                throw new UnauthorizedAccessException("No tiene permisos para registrar pagos de esta multa");

            if (solicitud.MontoPagado <= 0)
                throw new InvalidOperationException("El monto pagado debe ser mayor a 0");

            if (solicitud.MontoPagado > (multa.monto - (multa.monto_pagado ?? 0)))
                throw new InvalidOperationException("El monto pagado no puede superar el saldo pendiente");

            var metodoPago = await _contexto.metodos_pagos.FindAsync(solicitud.IdMetodoPago);
            if (metodoPago == null)
                throw new KeyNotFoundException("Método de pago no encontrado");

            string? comprobanteUrl = null;

            if (solicitud.Comprobante != null && solicitud.Comprobante.Length > 0)
            {
                var extension = Path.GetExtension(solicitud.Comprobante.FileName).ToLower();
                if (!new[] { ".jpg", ".jpeg", ".png", ".pdf" }.Contains(extension))
                    throw new ArgumentException("Formato de comprobante no soportado. Use JPG, PNG o PDF");

                if (solicitud.Comprobante.Length > 5 * 1024 * 1024)
                    throw new ArgumentException("El comprobante no puede superar los 5MB");

                comprobanteUrl = await _archivosHelper.GuardarArchivoAsync(solicitud.Comprobante, "comprobantes", $"pago_{multaId}_{DateTime.Now:yyyyMMddHHmmss}");
            }

            var pago = new multas_historial_pago
            {
                codigo = CodigoHelper.GenerarReferenciaPago(multaId, (multa.multas_historial_pagos?.Count ?? 0) + 1),
                id_multa = multaId,
                id_metodo_pago = solicitud.IdMetodoPago,
                monto_pagado = solicitud.MontoPagado,
                moneda = solicitud.Moneda ?? "Bs",
                fecha_pago = DateTime.UtcNow,
                codigo_transaccion = solicitud.CodigoTransaccion,
                comprobante_url = comprobanteUrl,
                comprobante_verificado = metodoPago.requiere_comprobante == false,
                estado = metodoPago.requiere_comprobante == true ? "PROCESANDO" : "APROBADO",
                id_usuario_pago = usuarioId
            };

            _contexto.multas_historial_pagos.Add(pago);

            multa.monto_pagado = (multa.monto_pagado ?? 0) + solicitud.MontoPagado;

            if (multa.monto_pagado >= multa.monto)
                multa.estado = "PAGADA";
            else if (multa.monto_pagado > 0)
                multa.estado = "PARCIAL";

            await _contexto.SaveChangesAsync();

            if (metodoPago.requiere_comprobante == true)
                await NotificarNuevoPagoPendienteAsync(pago, multa);
            else
                await EnviarComprobantePagoAsync(pago, multa);

            return new PagoResponse
            {
                Id = pago.id,
                Codigo = pago.codigo,
                IdMulta = pago.id_multa,
                IdMetodoPago = pago.id_metodo_pago,
                MetodoPago = metodoPago.nombre,
                MontoPagado = pago.monto_pagado,
                Moneda = pago.moneda ?? "Bs",
                FechaPago = pago.fecha_pago ?? DateTime.UtcNow,
                CodigoTransaccion = pago.codigo_transaccion,
                ComprobanteUrl = pago.comprobante_url,
                ComprobanteVerificado = pago.comprobante_verificado ?? false,
                Estado = pago.estado ?? "PROCESANDO"
            };
        }

        public async Task<PagoResponse> VerificarPagoAsync(int pagoId, int usuarioId, VerificarPagoRequest solicitud, string? ipAddress = null, string? userAgent = null)
        {
            var esAdmin = await EsAdminAsync(usuarioId);
            if (!esAdmin)
                throw new UnauthorizedAccessException("No tiene permisos para verificar pagos");

            var pago = await _contexto.multas_historial_pagos
                .Include(p => p.id_multaNavigation)
                .ThenInclude(m => m.id_equipoNavigation)
                .FirstOrDefaultAsync(p => p.id == pagoId);

            if (pago == null)
                throw new KeyNotFoundException("Pago no encontrado");

            var accion = solicitud.Accion.ToUpperInvariant();

            if (accion == "APROBAR")
            {
                pago.comprobante_verificado = true;
                pago.estado = "APROBADO";
                pago.fecha_verificacion = DateTime.UtcNow;
                pago.id_usuario_verificacion = usuarioId;
                pago.motivo_rechazo = null;

                var multa = pago.id_multaNavigation;
                if (multa.monto_pagado >= multa.monto)
                    multa.estado = "PAGADA";
                else if (multa.monto_pagado > 0)
                    multa.estado = "PARCIAL";

                await _contexto.SaveChangesAsync();
                await EnviarComprobantePagoAsync(pago, multa);
            }
            else if (accion == "RECHAZAR")
            {
                if (string.IsNullOrWhiteSpace(solicitud.MotivoRechazo))
                    throw new ArgumentException("Debe especificar un motivo de rechazo");

                pago.estado = "RECHAZADO";
                pago.motivo_rechazo = solicitud.MotivoRechazo;
                pago.fecha_verificacion = DateTime.UtcNow;
                pago.id_usuario_verificacion = usuarioId;

                var multa = pago.id_multaNavigation;
                multa.monto_pagado = (multa.monto_pagado ?? 0) - pago.monto_pagado;
                multa.estado = "PENDIENTE";

                await _contexto.SaveChangesAsync();
                await NotificarPagoRechazadoAsync(pago, multa);
            }
            else
            {
                throw new ArgumentException("Acción no válida. Use 'aprobar' o 'rechazar'");
            }

            return new PagoResponse
            {
                Id = pago.id,
                Codigo = pago.codigo,
                IdMulta = pago.id_multa,
                IdMetodoPago = pago.id_metodo_pago,
                MontoPagado = pago.monto_pagado,
                Moneda = pago.moneda ?? "Bs",
                FechaPago = pago.fecha_pago ?? DateTime.UtcNow,
                ComprobanteVerificado = pago.comprobante_verificado ?? false,
                FechaVerificacion = pago.fecha_verificacion,
                Estado = pago.estado ?? "PROCESANDO",
                MotivoRechazo = pago.motivo_rechazo
            };
        }

        public async Task<byte[]> GenerarComprobantePagoAsync(int pagoId, string baseUrl)
        {
            var pago = await _contexto.multas_historial_pagos
                .Include(p => p.id_multaNavigation)
                    .ThenInclude(m => m.id_torneoNavigation)
                .Include(p => p.id_multaNavigation)
                    .ThenInclude(m => m.id_equipoNavigation)
                .Include(p => p.id_multaNavigation)
                    .ThenInclude(m => m.id_jugadorNavigation)
                .Include(p => p.id_metodo_pagoNavigation)
                .FirstOrDefaultAsync(p => p.id == pagoId);

            if (pago == null)
                throw new KeyNotFoundException("Pago no encontrado");

            var multa = pago.id_multaNavigation;
            var qrContent = $"{baseUrl}/api/multas/verificar-pago/{pago.codigo}";
            var qrBytes = _qrHelper.GenerarQRCode(qrContent, 15);

            // Retornar QR por ahora - en producción se generaría PDF completo
            return qrBytes;
        }

        #endregion

        #region ========== REPORTES Y CONSULTAS ==========

        public async Task<List<MultaResponse>> ObtenerMultasPorEquipoAsync(int equipoId, int torneoId)
        {
            var multas = await _contexto.multas
                .Include(m => m.id_tipo_multaNavigation)
                .Where(m => m.id_equipo == equipoId && m.id_torneo == torneoId && m.estado != "CANCELADA")
                .OrderByDescending(m => m.fecha_aplicacion)
                .ToListAsync();

            return multas.Select(MapearMultaResponse).ToList();
        }

        public async Task<List<MultaResponse>> ObtenerMultasPorJugadorAsync(int jugadorId, int torneoId)
        {
            var multas = await _contexto.multas
                .Include(m => m.id_tipo_multaNavigation)
                .Where(m => m.id_jugador == jugadorId && m.id_torneo == torneoId && m.estado != "CANCELADA")
                .OrderByDescending(m => m.fecha_aplicacion)
                .ToListAsync();

            return multas.Select(MapearMultaResponse).ToList();
        }

        public async Task<ResumenFinancieroResponse> ObtenerResumenFinancieroAsync(int torneoId)
        {
            var multas = await _contexto.multas
                .Include(m => m.id_equipoNavigation)
                .Include(m => m.id_tipo_multaNavigation)
                .Where(m => m.id_torneo == torneoId && m.estado != "CANCELADA")
                .ToListAsync();

            var torneo = await _contexto.torneos.FindAsync(torneoId);
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

            return new ResumenFinancieroResponse
            {
                IdTorneo = torneoId,
                Torneo = torneo?.nombre ?? "",
                TotalMultas = multas.Sum(m => m.monto),
                TotalPagado = multas.Sum(m => m.monto_pagado ?? 0),
                TotalPendiente = multas.Sum(m => m.monto - (m.monto_pagado ?? 0)),
                TotalVencido = multas.Where(m => m.fecha_limite_pago < hoy && m.estado != "PAGADA").Sum(m => m.monto - (m.monto_pagado ?? 0)),
                CantidadMultasPendientes = multas.Count(m => m.estado == "PENDIENTE" || m.estado == "PARCIAL"),
                CantidadMultasPagadas = multas.Count(m => m.estado == "PAGADA"),
                CantidadMultasVencidas = multas.Count(m => m.fecha_limite_pago < hoy && m.estado != "PAGADA"),
                PorEquipo = multas.Where(m => m.id_equipo.HasValue)
                    .GroupBy(m => new { m.id_equipo, Equipo = m.id_equipoNavigation?.nombre ?? "Sin equipo" })
                    .Select(g => new ResumenPorEquipo
                    {
                        IdEquipo = g.Key.id_equipo ?? 0,
                        Equipo = g.Key.Equipo,
                        TotalMultas = g.Sum(m => m.monto),
                        Pagado = g.Sum(m => m.monto_pagado ?? 0),
                        Pendiente = g.Sum(m => m.monto - (m.monto_pagado ?? 0)),
                        CantidadMultas = g.Count()
                    }).ToList(),
                PorTipoMulta = multas
                    .GroupBy(m => new { m.id_tipo_multa, Tipo = m.id_tipo_multaNavigation?.nombre ?? "Sin tipo" })
                    .Select(g => new ResumenPorTipoMulta
                    {
                        IdTipoMulta = g.Key.id_tipo_multa,
                        TipoMulta = g.Key.Tipo,
                        TotalMonto = g.Sum(m => m.monto),
                        Pagado = g.Sum(m => m.monto_pagado ?? 0),
                        Cantidad = g.Count()
                    }).ToList()
            };
        }

        public async Task<object> ObtenerEstadisticasMultasAsync()
        {
            var totalMultas = await _contexto.multas.CountAsync();
            var totalPagado = await _contexto.multas.SumAsync(m => m.monto_pagado ?? 0);
            var totalPendiente = await _contexto.multas.SumAsync(m => m.monto - (m.monto_pagado ?? 0));

            var porEstado = await _contexto.multas
                .GroupBy(m => m.estado ?? "PENDIENTE")
                .Select(g => new { Estado = g.Key, Cantidad = g.Count(), Total = g.Sum(m => m.monto) })
                .ToListAsync();

            var porTipo = await _contexto.multas
                .GroupBy(m => m.id_tipo_multaNavigation != null ? m.id_tipo_multaNavigation.nombre : "Sin tipo")
                .Select(g => new { Tipo = g.Key, Cantidad = g.Count(), Total = g.Sum(m => m.monto) })
                .ToListAsync();

            return new
            {
                TotalMultas = totalMultas,
                TotalPagado = totalPagado,
                TotalPendiente = totalPendiente,
                PorEstado = porEstado,
                PorTipo = porTipo
            };
        }

        #endregion

        #region ========== PROCESOS AUTOMÁTICOS ==========

        public async Task<List<MultaResponse>> AplicarMultasAutomaticasAsync(int partidoId, int? eventoId = null, string? ipAddress = null, string? userAgent = null)
        {
            var multasCreadas = new List<MultaResponse>();

            var partido = await _contexto.partidos
                .Include(p => p.id_torneoNavigation)
                .FirstOrDefaultAsync(p => p.id == partidoId && p.activo == true);

            if (partido == null)
                throw new KeyNotFoundException("Partido no encontrado");

            var configMultas = await _contexto.torneos_config_multas
                .Where(cm => cm.id_torneo == partido.id_torneo && cm.aplica_automaticamente == true && cm.activo == true)
                .ToListAsync();

            if (!configMultas.Any())
                return multasCreadas;

            // Caso WALK OVER
            if (!eventoId.HasValue && partido.es_wo == true)
            {
                var configWO = configMultas.FirstOrDefault(cm => cm.id_tipo_multa == 7);
                if (configWO != null)
                {
                    var equipoPerdedorWO = partido.id_equipo_ganador_wo == partido.id_equipo_local
                        ? partido.id_equipo_visitante
                        : partido.id_equipo_local;

                    // Verificar si ya existe multa por WO para este partido
                    var multaExistente = await _contexto.multas
                        .AnyAsync(m => m.id_partido == partidoId && m.id_tipo_multa == 7 && m.generada_automaticamente == true);

                    if (!multaExistente)
                    {
                        var solicitudMulta = new CrearMultaRequest
                        {
                            IdTorneo = partido.id_torneo,
                            IdTipoMulta = configWO.id_tipo_multa,
                            IdEquipo = equipoPerdedorWO,
                            IdPartido = partidoId,
                            GeneradaAutomaticamente = true,
                            Monto = configWO.monto,
                            Moneda = configWO.moneda,
                            Descripcion = "Walk Over - El equipo no se presentó al partido programado",
                            NotasInternas = $"Partido ID: {partidoId} - Fecha: {partido.fecha_hora}",
                            FechaLimitePago = DateTime.UtcNow.AddDays(7)
                        };

                        var multa = await CrearMultaAsync(1, solicitudMulta, ipAddress, userAgent);
                        multasCreadas.Add(multa);
                    }
                }
            }

            // Procesar eventos
            if (eventoId.HasValue)
            {
                var evento = await _contexto.partidos_eventos
                    .FirstOrDefaultAsync(e => e.id == eventoId.Value);

                if (evento != null && evento.id_jugador.HasValue)
                {
                    foreach (var config in configMultas)
                    {
                        bool debeAplicar = false;
                        string descripcion = string.Empty;

                        switch (evento.tipo_evento)
                        {
                            case "TARJETA_ROJA" when config.id_tipo_multa == 2:
                                debeAplicar = true;
                                descripcion = $"Tarjeta roja recibida en el minuto {evento.minuto}";
                                break;
                            case "TARJETA_AMARILLA" when config.id_tipo_multa == 1:
                                var amarillasPrevias = await _contexto.partidos_eventos
                                    .CountAsync(e => e.id_jugador == evento.id_jugador &&
                                                    e.tipo_evento == "TARJETA_AMARILLA" &&
                                                    e.id_partido == partidoId &&
                                                    e.id != eventoId.Value);
                                if (amarillasPrevias >= 1)
                                {
                                    debeAplicar = true;
                                    descripcion = $"Segunda tarjeta amarilla (acumulación) en el minuto {evento.minuto}";
                                }
                                break;
                            case "AGRESION" when config.id_tipo_multa == 3:
                                debeAplicar = true;
                                descripcion = $"Agresión cometida en el minuto {evento.minuto}";
                                break;
                            case "CONDUCTA_ANTIDEPORTIVA" when config.id_tipo_multa == 9:
                                debeAplicar = true;
                                descripcion = $"Conducta antideportiva en el minuto {evento.minuto}";
                                break;
                        }

                        if (debeAplicar)
                        {
                            var multaExistente = await _contexto.multas
                                .AnyAsync(m => m.id_evento == eventoId.Value && m.id_tipo_multa == config.id_tipo_multa);

                            if (!multaExistente)
                            {
                                var solicitudMulta = new CrearMultaRequest
                                {
                                    IdTorneo = partido.id_torneo,
                                    IdTipoMulta = config.id_tipo_multa,
                                    IdEquipo = evento.id_equipo,
                                    IdJugador = evento.id_jugador,
                                    IdPartido = partidoId,
                                    IdEvento = eventoId,
                                    GeneradaAutomaticamente = true,
                                    Monto = config.monto,
                                    Moneda = config.moneda,
                                    Descripcion = descripcion,
                                    NotasInternas = $"Partido ID: {partidoId}, Jugador ID: {evento.id_jugador}, Minuto: {evento.minuto}",
                                    FechaLimitePago = DateTime.UtcNow.AddDays(7)
                                };

                                var multa = await CrearMultaAsync(1, solicitudMulta, ipAddress, userAgent);
                                multasCreadas.Add(multa);
                            }
                        }
                    }
                }
            }

            return multasCreadas;
        }

        public async Task EnviarRecordatoriosPagoAsync()
        {
            var fechaLimite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
            var multasProximasVencer = await _contexto.multas
                .Include(m => m.id_equipoNavigation)
                .Include(m => m.id_jugadorNavigation)
                .Where(m => m.fecha_limite_pago == fechaLimite && m.estado != "PAGADA" && m.estado != "CANCELADA")
                .ToListAsync();

            foreach (var multa in multasProximasVencer)
            {
                var destinatarioId = multa.id_equipoNavigation?.id_capitan ?? multa.id_jugador;
                if (destinatarioId.HasValue)
                {
                    await _notificacionService.EnviarNotificacionAsync(1, new EnviarNotificacionRequest
                    {
                        IdUsuarioDestino = destinatarioId.Value,
                        IdTipoNotificacion = 14,
                        Titulo = "Recordatorio de pago de multa",
                        Mensaje = $"La multa con código {multa.codigo} vencerá en 3 días. Saldo pendiente: {multa.monto - (multa.monto_pagado ?? 0)} {multa.moneda}",
                        IdTorneo = multa.id_torneo,
                        IdEquipo = multa.id_equipo,
                        Prioridad = "ALTA"
                    });
                }
            }

            _logger.LogInformation("Recordatorios de pago enviados - Cantidad: {Cantidad}", multasProximasVencer.Count);
        }

        public async Task MarcarMultasVencidasAsync()
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var multasVencidas = await _contexto.multas
                .Where(m => m.fecha_limite_pago < hoy && m.estado != "PAGADA" && m.estado != "CANCELADA" && m.estado != "VENCIDA")
                .ToListAsync();

            foreach (var multa in multasVencidas)
            {
                multa.estado = "VENCIDA";
            }

            await _contexto.SaveChangesAsync();
            _logger.LogInformation("Multas marcadas como vencidas - Cantidad: {Cantidad}", multasVencidas.Count);
        }

        #endregion
    }
}