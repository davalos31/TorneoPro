using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using TorneoPro.API.DTOs.Multas;
using TorneoPro.API.DTOs.Multas.Request;
using TorneoPro.API.DTOs.Multas.Response;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Multas;

namespace TorneoPro.API.Controllers
{
    [Route("api/multas")]
    [ApiController]
    [Authorize]
    public class MultasController : ControllerBase
    {
        #region ========== CAMPOS Y CONSTRUCTOR ==========

        private readonly IMultaService _multaService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<MultasController> _logger;

        public MultasController(
            IMultaService multaService,
            IAuditoriaService auditoriaService,
            ILogger<MultasController> logger)
        {
            _multaService = multaService;
            _auditoriaService = auditoriaService;
            _logger = logger;
        }

        #endregion

        #region ========== CRUD DE MULTAS ==========

        /// <summary>
        /// Obtiene una lista paginada de multas con filtros avanzados
        /// </summary>
        /// <param name="solicitud">Filtros: torneo, equipo, jugador, tipo, estado, rango de fechas</param>
        /// <returns>Lista paginada de multas</returns>
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,SUB_ADMIN,CAPITAN")]
        public async Task<IActionResult> Listar([FromQuery] FiltrarMultaRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Listado de multas - UsuarioId: {UsuarioId}, Pagina: {Pagina}, IP: {Ip}",
                usuarioId, solicitud.Pagina, ipCliente);

            try
            {
                var resultado = await _multaService.ObtenerTodosAsync(solicitud);
                return Ok(ApiRespuesta<ResultadoPaginado<MultaResponse>>.Success(resultado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar multas");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la lista de multas"));
            }
        }

        /// <summary>
        /// Obtiene el detalle completo de una multa por su ID
        /// </summary>
        /// <param name="id">ID de la multa</param>
        /// <returns>Detalle de la multa incluyendo historial de pagos</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de multa - MultaId: {MultaId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var multa = await _multaService.ObtenerPorIdAsync(id);
                if (multa == null)
                    return NotFound(ApiRespuesta<object>.Error("Multa no encontrada"));

                return Ok(ApiRespuesta<MultaDetalleResponse>.Success(multa));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener multa - MultaId: {MultaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener la multa"));
            }
        }

        /// <summary>
        /// Crea una nueva multa manualmente (solo administradores)
        /// </summary>
        /// <param name="solicitud">Datos de la multa</param>
        /// <returns>Multa creada</returns>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> CrearMulta([FromBody] CrearMultaRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Creación de multa - UsuarioId: {UsuarioId}, Monto: {Monto}, IP: {Ip}",
                usuarioId, solicitud.Monto, ipCliente);

            try
            {
                var resultado = await _multaService.CrearMultaAsync(usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "CREAR_MULTA",
                    "multas",
                    resultado.Id,
                    JsonSerializer.Serialize(new { solicitud.Monto, solicitud.IdTipoMulta, solicitud.IdEquipo, solicitud.IdJugador }));

                _logger.LogInformation("Multa creada exitosamente - MultaId: {MultaId}, Monto: {Monto}", resultado.Id, resultado.Monto);
                return Ok(ApiRespuesta<MultaResponse>.Success(resultado, "Multa creada exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear multa");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al crear la multa"));
            }
        }

        /// <summary>
        /// Actualiza una multa existente (solo administradores)
        /// </summary>
        /// <param name="id">ID de la multa</param>
        /// <param name="solicitud">Datos a actualizar</param>
        /// <returns>Multa actualizada</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ActualizarMulta(int id, [FromBody] ActualizarMultaRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Actualización de multa - MultaId: {MultaId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var resultado = await _multaService.ActualizarMultaAsync(id, usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "ACTUALIZAR_MULTA", "multas", id);

                return Ok(ApiRespuesta<MultaResponse>.Success(resultado, "Multa actualizada exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar multa - MultaId: {MultaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al actualizar la multa"));
            }
        }

        /// <summary>
        /// Cancela una multa (solo administradores)
        /// </summary>
        /// <param name="id">ID de la multa</param>
        /// <param name="motivo">Motivo de cancelación</param>
        /// <returns>Confirmación de cancelación</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> CancelarMulta(int id, [FromQuery] string? motivo = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Cancelación de multa - MultaId: {MultaId}, UsuarioId: {UsuarioId}, Motivo: {Motivo}, IP: {Ip}",
                id, usuarioId, motivo ?? "Sin motivo", ipCliente);

            try
            {
                await _multaService.CancelarMultaAsync(id, usuarioId, motivo, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "CANCELAR_MULTA", "multas", id, motivo);

                _logger.LogInformation("Multa cancelada - MultaId: {MultaId}", id);
                return Ok(ApiRespuesta<object>.Success(null, "Multa cancelada exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar multa - MultaId: {MultaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al cancelar la multa"));
            }
        }

        #endregion

        #region ========== GESTIÓN DE PAGOS ==========

        /// <summary>
        /// Registra un pago para una multa
        /// </summary>
        /// <param name="id">ID de la multa</param>
        /// <param name="solicitud">Datos del pago (método, monto, comprobante)</param>
        /// <returns>Pago registrado</returns>
        [HttpPost("{id}/pagar")]
        public async Task<IActionResult> RegistrarPago(int id, [FromForm] RegistrarPagoRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Registro de pago - MultaId: {MultaId}, UsuarioId: {UsuarioId}, Monto: {Monto}, IP: {Ip}",
                id, usuarioId, solicitud.MontoPagado, ipCliente);

            try
            {
                var resultado = await _multaService.RegistrarPagoAsync(id, usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "REGISTRAR_PAGO_MULTA",
                    "multas_historial_pagos",
                    resultado.Id,
                    JsonSerializer.Serialize(new { solicitud.MontoPagado, solicitud.IdMetodoPago }));

                _logger.LogInformation("Pago registrado - PagoId: {PagoId}, MultaId: {MultaId}, Monto: {Monto}",
                    resultado.Id, id, resultado.MontoPagado);

                return Ok(ApiRespuesta<PagoResponse>.Success(resultado, "Pago registrado exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar pago - MultaId: {MultaId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al registrar el pago"));
            }
        }

        /// <summary>
        /// Verifica un comprobante de pago (solo administradores)
        /// </summary>
        /// <param name="solicitud">Datos de verificación (aprobar/rechazar)</param>
        /// <returns>Pago verificado</returns>
        [HttpPut("verificar-pago")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> VerificarPago([FromBody] VerificarPagoRequest solicitud)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Verificación de pago - PagoId: {PagoId}, UsuarioId: {UsuarioId}, Accion: {Accion}, IP: {Ip}",
                solicitud.IdPago, usuarioId, solicitud.Accion, ipCliente);

            try
            {
                var resultado = await _multaService.VerificarPagoAsync(solicitud.IdPago, usuarioId, solicitud, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "VERIFICAR_PAGO_MULTA",
                    "multas_historial_pagos",
                    solicitud.IdPago,
                    JsonSerializer.Serialize(new { solicitud.Accion, solicitud.MotivoRechazo }));

                _logger.LogInformation("Pago verificado - PagoId: {PagoId}, Accion: {Accion}", solicitud.IdPago, solicitud.Accion);
                return Ok(ApiRespuesta<PagoResponse>.Success(resultado, $"Pago {solicitud.Accion.ToLowerInvariant()} exitosamente"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar pago - PagoId: {PagoId}", solicitud.IdPago);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al verificar el pago"));
            }
        }

        /// <summary>
        /// Genera un comprobante de pago en PDF
        /// </summary>
        /// <param name="pagoId">ID del pago</param>
        /// <returns>Archivo PDF del comprobante</returns>
        [HttpGet("comprobante/{pagoId}")]
        public async Task<IActionResult> GenerarComprobantePago(int pagoId)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Generación de comprobante de pago - PagoId: {PagoId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                pagoId, usuarioId, ipCliente);

            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var pdfBytes = await _multaService.GenerarComprobantePagoAsync(pagoId, baseUrl);

                return File(pdfBytes, "application/pdf", $"comprobante_pago_{pagoId}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar comprobante de pago - PagoId: {PagoId}", pagoId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al generar el comprobante"));
            }
        }

        #endregion

        #region ========== REPORTES Y CONSULTAS ==========

        /// <summary>
        /// Obtiene las multas de un equipo en un torneo específico
        /// </summary>
        /// <param name="equipoId">ID del equipo</param>
        /// <param name="torneoId">ID del torneo</param>
        /// <returns>Lista de multas del equipo</returns>
        [HttpGet("equipos/{equipoId}/torneos/{torneoId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,CAPITAN")]
        public async Task<IActionResult> ObtenerMultasPorEquipo(int equipoId, int torneoId)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de multas por equipo - EquipoId: {EquipoId}, TorneoId: {TorneoId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                equipoId, torneoId, usuarioId, ipCliente);

            try
            {
                var multas = await _multaService.ObtenerMultasPorEquipoAsync(equipoId, torneoId);
                return Ok(ApiRespuesta<List<MultaResponse>>.Success(multas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener multas por equipo");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener las multas"));
            }
        }

        /// <summary>
        /// Obtiene las multas de un jugador en un torneo específico
        /// </summary>
        /// <param name="jugadorId">ID del jugador</param>
        /// <param name="torneoId">ID del torneo</param>
        /// <returns>Lista de multas del jugador</returns>
        [HttpGet("jugadores/{jugadorId}/torneos/{torneoId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,CAPITAN")]
        public async Task<IActionResult> ObtenerMultasPorJugador(int jugadorId, int torneoId)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de multas por jugador - JugadorId: {JugadorId}, TorneoId: {TorneoId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                jugadorId, torneoId, usuarioId, ipCliente);

            try
            {
                var multas = await _multaService.ObtenerMultasPorJugadorAsync(jugadorId, torneoId);
                return Ok(ApiRespuesta<List<MultaResponse>>.Success(multas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener multas por jugador");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener las multas"));
            }
        }

        /// <summary>
        /// Obtiene el resumen financiero de un torneo
        /// </summary>
        /// <param name="id">ID del torneo</param>
        /// <returns>Resumen financiero del torneo</returns>
        [HttpGet("torneos/{id}/resumen")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ObtenerResumenFinanciero(int id)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de resumen financiero - TorneoId: {TorneoId}, UsuarioId: {UsuarioId}, IP: {Ip}",
                id, usuarioId, ipCliente);

            try
            {
                var resumen = await _multaService.ObtenerResumenFinancieroAsync(id);
                return Ok(ApiRespuesta<ResumenFinancieroResponse>.Success(resumen));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen financiero - TorneoId: {TorneoId}", id);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener el resumen financiero"));
            }
        }

        /// <summary>
        /// Obtiene estadísticas generales de multas
        /// </summary>
        /// <returns>Estadísticas de multas</returns>
        [HttpGet("estadisticas")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> ObtenerEstadisticasMultas()
        {
            var usuarioId = ObtenerUsuarioActualId();

            _logger.LogInformation("Consulta de estadísticas de multas - UsuarioId: {UsuarioId}", usuarioId);

            try
            {
                var estadisticas = await _multaService.ObtenerEstadisticasMultasAsync();
                return Ok(ApiRespuesta<object>.Success(estadisticas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de multas");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener estadísticas"));
            }
        }

        #endregion

        #region ========== PROCESOS AUTOMÁTICOS ==========

        /// <summary>
        /// Aplica multas automáticas basadas en eventos de partido
        /// </summary>
        /// <param name="partidoId">ID del partido</param>
        /// <param name="eventoId">ID del evento (opcional)</param>
        /// <returns>Multas aplicadas</returns>
        [HttpPost("aplicar-automaticas")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> AplicarMultasAutomaticas([FromQuery] int partidoId, [FromQuery] int? eventoId = null)
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Aplicación de multas automáticas - PartidoId: {PartidoId}, EventoId: {EventoId}, UsuarioId: {UsuarioId}",
                partidoId, eventoId, usuarioId);

            try
            {
                var multas = await _multaService.AplicarMultasAutomaticasAsync(partidoId, eventoId, ipCliente, userAgent);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioId,
                    "APLICAR_MULTAS_AUTOMATICAS",
                    "multas",
                    partidoId,
                    $"Multas aplicadas: {multas.Count}");

                return Ok(ApiRespuesta<object>.Success(new { Cantidad = multas.Count, Multas = multas }, "Multas automáticas aplicadas"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar multas automáticas");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al aplicar multas automáticas"));
            }
        }

        /// <summary>
        /// Envía recordatorios de pago para multas próximas a vencer
        /// </summary>
        /// <returns>Resultado del envío</returns>
        [HttpPost("enviar-recordatorios")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> EnviarRecordatoriosPago()
        {
            var usuarioId = ObtenerUsuarioActualId();

            _logger.LogInformation("Envío de recordatorios de pago - UsuarioId: {UsuarioId}", usuarioId);

            try
            {
                await _multaService.EnviarRecordatoriosPagoAsync();

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "ENVIAR_RECORDATORIOS_PAGO", "multas", null);

                return Ok(ApiRespuesta<object>.Success(null, "Recordatorios enviados exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar recordatorios de pago");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al enviar recordatorios"));
            }
        }

        /// <summary>
        /// Marca automáticamente las multas vencidas
        /// </summary>
        /// <returns>Resultado del proceso</returns>
        [HttpPost("marcar-vencidas")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> MarcarMultasVencidas()
        {
            var usuarioId = ObtenerUsuarioActualId();

            _logger.LogInformation("Marcado de multas vencidas - UsuarioId: {UsuarioId}", usuarioId);

            try
            {
                await _multaService.MarcarMultasVencidasAsync();

                await _auditoriaService.RegistrarExitoAsync(usuarioId, "MARCAR_MULTAS_VENCIDAS", "multas", null);

                return Ok(ApiRespuesta<object>.Success(null, "Multas vencidas marcadas exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar multas vencidas");
                return StatusCode(500, ApiRespuesta<object>.Error("Error al marcar multas vencidas"));
            }
        }

        #endregion

        #region ========== MÉTODOS PRIVADOS ==========

        private int ObtenerUsuarioActualId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Usuario no identificado");
            return id;
        }

        private string ObtenerIpCliente()
        {
            var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return string.IsNullOrEmpty(ip) ? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "IP desconocida" : ip;
        }

        #endregion
    }
}