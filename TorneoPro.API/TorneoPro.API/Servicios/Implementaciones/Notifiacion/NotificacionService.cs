using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Implementaciones.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Email;
using TorneoPro.API.Servicios.Interfaces.Notificacion;

namespace TorneoPro.API.Servicios.Implementaciones.Notifiacion
{
    public class NotificacionService : INotificacionService
    {
        private readonly TorneoProContext _contexto;
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificacionService> _logger;
        private readonly IAuditoriaService _auditoriaService;

        public NotificacionService(
            TorneoProContext contexto,
            IEmailService emailService,
            ILogger<NotificacionService> logger,
            IAuditoriaService auditoriaService)
        {
            _contexto = contexto;
            _emailService = emailService;
            _logger = logger;
            _auditoriaService = auditoriaService;
        }

        public async Task<ResultadoPaginado<NotificacionResponse>> ObtenerMisNotificacionesAsync(
            int usuarioId,
            FiltrarNotificacionRequest solicitud)
        {
            var query = _contexto.notificaciones
                .Include(n => n.id_tipo_notificacionNavigation)
                .Include(n => n.id_torneoNavigation)
                .Include(n => n.id_equipoNavigation)
                .Where(n => n.id_usuario_destino == usuarioId && n.archivada == false)
                .AsQueryable();

            if (solicitud.Leida.HasValue)
                query = query.Where(n => n.leida == solicitud.Leida.Value);

            if (solicitud.Enviada.HasValue)
                query = query.Where(n => n.enviada == solicitud.Enviada.Value);

            if (solicitud.IdTipoNotificacion.HasValue)
                query = query.Where(n => n.id_tipo_notificacion == solicitud.IdTipoNotificacion.Value);

            if (!string.IsNullOrWhiteSpace(solicitud.Prioridad))
                query = query.Where(n => n.prioridad == solicitud.Prioridad);

            if (solicitud.FechaDesde.HasValue)
                query = query.Where(n => n.fecha_creacion >= solicitud.FechaDesde.Value);

            if (solicitud.FechaHasta.HasValue)
                query = query.Where(n => n.fecha_creacion <= solicitud.FechaHasta.Value);

            var totalItems = await query.CountAsync();

            query = query.OrderByDescending(n => n.fecha_creacion);

            var notificaciones = await query
                .Skip((solicitud.Pagina - 1) * solicitud.TamanoPagina)
                .Take(solicitud.TamanoPagina)
                .ToListAsync();

            var items = notificaciones.Select(n => MapearNotificacionResponse(n)).ToList();

            return ResultadoPaginado<NotificacionResponse>.Crear(items, totalItems, solicitud.Pagina, solicitud.TamanoPagina);
        }

        public async Task<NotificacionResponse?> ObtenerNotificacionPorIdAsync(int notificacionId, int usuarioId)
        {
            var notificacion = await _contexto.notificaciones
                .Include(n => n.id_tipo_notificacionNavigation)
                .Include(n => n.id_torneoNavigation)
                .Include(n => n.id_equipoNavigation)
                .FirstOrDefaultAsync(n => n.id == notificacionId && n.id_usuario_destino == usuarioId);

            if (notificacion == null)
                return null;

            return MapearNotificacionResponse(notificacion);
        }

        public async Task MarcarComoLeidaAsync(int notificacionId, int usuarioId)
        {
            var notificacion = await _contexto.notificaciones
                .FirstOrDefaultAsync(n => n.id == notificacionId && n.id_usuario_destino == usuarioId);

            if (notificacion == null)
                throw new KeyNotFoundException("Notificación no encontrada");

            notificacion.leida = true;
            notificacion.fecha_lectura = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            _logger.LogInformation("Notificación marcada como leída - NotificacionId: {NotificacionId}, UsuarioId: {UsuarioId}",
                notificacionId, usuarioId);
        }

        public async Task MarcarTodasComoLeidasAsync(int usuarioId)
        {
            var notificaciones = await _contexto.notificaciones
                .Where(n => n.id_usuario_destino == usuarioId && n.leida == false && n.archivada == false)
                .ToListAsync();

            foreach (var notificacion in notificaciones)
            {
                notificacion.leida = true;
                notificacion.fecha_lectura = DateTime.UtcNow;
            }

            await _contexto.SaveChangesAsync();

            _logger.LogInformation("Todas las notificaciones marcadas como leídas - UsuarioId: {UsuarioId}, Cantidad: {Cantidad}",
                usuarioId, notificaciones.Count);
        }

        public async Task ArchivarNotificacionAsync(int notificacionId, int usuarioId)
        {
            var notificacion = await _contexto.notificaciones
                .FirstOrDefaultAsync(n => n.id == notificacionId && n.id_usuario_destino == usuarioId);

            if (notificacion == null)
                throw new KeyNotFoundException("Notificación no encontrada");

            notificacion.archivada = true;

            await _contexto.SaveChangesAsync();

            _logger.LogInformation("Notificación archivada - NotificacionId: {NotificacionId}, UsuarioId: {UsuarioId}",
                notificacionId, usuarioId);
        }

        public async Task<NotificacionResponse> EnviarNotificacionAsync(int usuarioIdOrigen, EnviarNotificacionRequest solicitud)
        {
            // Validar que el usuario destino existe
            var usuarioDestino = await _contexto.usuarios.FindAsync(solicitud.IdUsuarioDestino);
            if (usuarioDestino == null)
                throw new KeyNotFoundException("Usuario destino no encontrado");

            // Validar tipo de notificación
            var tipoNotificacion = await _contexto.tipos_notificacions.FindAsync(solicitud.IdTipoNotificacion);
            if (tipoNotificacion == null)
                throw new KeyNotFoundException("Tipo de notificación no encontrado");

            var notificacion = new notificacione
            {
                codigo = CodigoHelper.GenerarCodigo("NOT", 10),
                id_tipo_notificacion = solicitud.IdTipoNotificacion,
                id_usuario_destino = solicitud.IdUsuarioDestino,
                titulo = solicitud.Titulo,
                mensaje = solicitud.Mensaje,
                prioridad = solicitud.Prioridad ?? "MEDIA",
                id_torneo = solicitud.IdTorneo,
                id_equipo = solicitud.IdEquipo,
                id_partido = solicitud.IdPartido,
                id_multa = solicitud.IdMulta,
                id_suspension = solicitud.IdSuspension,
                accion_url = solicitud.AccionUrl,
                accion_tipo = solicitud.AccionTipo,
                fecha_programada_envio = solicitud.FechaProgramadaEnvio,
                leida = false,
                archivada = false,
                fecha_creacion = DateTime.UtcNow,
                enviada = false
            };

            // Si no hay fecha programada, enviar inmediatamente
            if (!solicitud.FechaProgramadaEnvio.HasValue || solicitud.FechaProgramadaEnvio.Value <= DateTime.UtcNow)
            {
                await ProcesarEnvioNotificacion(notificacion, usuarioDestino, solicitud.Canales, tipoNotificacion);
                notificacion.enviada = true;
                notificacion.fecha_envio = DateTime.UtcNow;
            }

            _contexto.notificaciones.Add(notificacion);
            await _contexto.SaveChangesAsync();

            await _auditoriaService.RegistrarExitoAsync(
                usuarioIdOrigen,
                "ENVIAR_NOTIFICACION",
                "notificaciones",
                notificacion.id,
                AuditoriaService.SerializarDatos(new
                {
                    Destino = solicitud.IdUsuarioDestino,
                    Tipo = solicitud.IdTipoNotificacion,
                    Titulo = solicitud.Titulo,
                    Canales = solicitud.Canales
                }));

            _logger.LogInformation("Notificación enviada - Id: {NotificacionId}, Destino: {UsuarioDestino}, Tipo: {TipoNotificacion}",
                notificacion.id, solicitud.IdUsuarioDestino, tipoNotificacion.nombre);

            return MapearNotificacionResponse(notificacion, tipoNotificacion);
        }

        public async Task<int> EnviarNotificacionMasivaAsync(int usuarioIdOrigen, NotificacionMasivaRequest solicitud)
        {
            var usuariosDestino = await ObtenerUsuariosDestino(solicitud);

            if (!usuariosDestino.Any())
                throw new InvalidOperationException("No hay usuarios destino para enviar la notificación");

            var tipoNotificacion = await _contexto.tipos_notificacions.FindAsync(solicitud.IdTipoNotificacion);
            if (tipoNotificacion == null)
                throw new KeyNotFoundException("Tipo de notificación no encontrado");

            var notificacionesCreadas = new List<notificacione>();
            var notificacionesEnviadas = 0;

            foreach (var usuarioDestino in usuariosDestino)
            {
                var notificacion = new notificacione
                {
                    codigo = CodigoHelper.GenerarCodigo("NOT", 10),
                    id_tipo_notificacion = solicitud.IdTipoNotificacion,
                    id_usuario_destino = usuarioDestino.id,
                    titulo = solicitud.Titulo,
                    mensaje = solicitud.Mensaje,
                    prioridad = solicitud.Prioridad ?? "MEDIA",
                    id_torneo = solicitud.IdTorneo,
                    id_equipo = solicitud.IdEquipo,
                    accion_url = solicitud.AccionUrl,
                    leida = false,
                    archivada = false,
                    fecha_creacion = DateTime.UtcNow,
                    enviada = false
                };

                // Enviar según canales configurados
                await ProcesarEnvioNotificacion(notificacion, usuarioDestino, solicitud.Canales, tipoNotificacion);
                notificacion.enviada = true;
                notificacion.fecha_envio = DateTime.UtcNow;
                notificacionesEnviadas++;

                notificacionesCreadas.Add(notificacion);
            }

            await _contexto.notificaciones.AddRangeAsync(notificacionesCreadas);
            await _contexto.SaveChangesAsync();

            await _auditoriaService.RegistrarExitoAsync(
                usuarioIdOrigen,
                "ENVIAR_NOTIFICACION_MASIVA",
                "notificaciones",
                null,
                AuditoriaService.SerializarDatos(new
                {
                    CantidadDestinos = usuariosDestino.Count,
                    Tipo = solicitud.IdTipoNotificacion,
                    Titulo = solicitud.Titulo,
                    Canales = solicitud.Canales
                }));

            _logger.LogInformation("Notificación masiva enviada - Destinatarios: {Cantidad}, Tipo: {TipoNotificacion}",
                usuariosDestino.Count, tipoNotificacion.nombre);

            return notificacionesEnviadas;
        }

        public async Task<List<PreferenciaNotificacionResponse>> ObtenerPreferenciasAsync(int usuarioId)
        {
            var preferencias = await _contexto.preferencias_notificaciones
                .Include(p => p.id_tipo_notificacionNavigation)
                .Where(p => p.id_usuario == usuarioId)
                .ToListAsync();

            if (!preferencias.Any())
            {
                // Crear preferencias por defecto para todos los tipos de notificación
                await CrearPreferenciasPorDefecto(usuarioId);
                preferencias = await _contexto.preferencias_notificaciones
                    .Include(p => p.id_tipo_notificacionNavigation)
                    .Where(p => p.id_usuario == usuarioId)
                    .ToListAsync();
            }

            return preferencias.Select(p => new PreferenciaNotificacionResponse
            {
                Id = p.id,
                IdTipoNotificacion = p.id_tipo_notificacion,
                TipoNotificacion = p.id_tipo_notificacionNavigation?.nombre ?? "",
                Categoria = p.id_tipo_notificacionNavigation?.categoria ?? "",
                Activado = p.activado ?? true,
                PushActivado = p.push_activado ?? true,
                EmailActivado = p.email_activado ?? true,
                SmsActivado = p.sms_activado ?? false,
                FechaModificacion = p.fecha_modificacion ?? DateTime.UtcNow
            }).ToList();
        }

        public async Task ActualizarPreferenciasAsync(int usuarioId, int idTipoNotificacion, ActualizarPreferenciaRequest solicitud)
        {
            var preferencia = await _contexto.preferencias_notificaciones
                .FirstOrDefaultAsync(p => p.id_usuario == usuarioId && p.id_tipo_notificacion == idTipoNotificacion);

            if (preferencia == null)
                throw new KeyNotFoundException("Preferencia no encontrada");

            if (solicitud.Activado.HasValue)
                preferencia.activado = solicitud.Activado.Value;

            if (solicitud.PushActivado.HasValue)
                preferencia.push_activado = solicitud.PushActivado.Value;

            if (solicitud.EmailActivado.HasValue)
                preferencia.email_activado = solicitud.EmailActivado.Value;

            if (solicitud.SmsActivado.HasValue)
                preferencia.sms_activado = solicitud.SmsActivado.Value;

            preferencia.fecha_modificacion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            await _auditoriaService.RegistrarExitoAsync(
                usuarioId,
                "ACTUALIZAR_PREFERENCIAS_NOTIFICACION",
                "preferencias_notificaciones",
                preferencia.id,
                AuditoriaService.SerializarDatos(solicitud));

            _logger.LogInformation("Preferencias de notificación actualizadas - UsuarioId: {UsuarioId}, TipoNotificacionId: {TipoId}",
                usuarioId, idTipoNotificacion);
        }

        public async Task ProcesarNotificacionesPendientesAsync()
        {
            var notificacionesPendientes = await _contexto.notificaciones
                .Include(n => n.id_usuario_destinoNavigation)
                .Include(n => n.id_tipo_notificacionNavigation)
                .Where(n => (n.enviada == null || n.enviada == false) && n.fecha_programada_envio.HasValue && n.fecha_programada_envio.Value <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var notificacion in notificacionesPendientes)
            {
                try
                {
                    var canales = new List<string> { notificacion.canal ?? "IN_APP" };
                    await ProcesarEnvioNotificacion(
                        notificacion,
                        notificacion.id_usuario_destinoNavigation,
                        canales,
                        notificacion.id_tipo_notificacionNavigation);

                    notificacion.enviada = true;
                    notificacion.fecha_envio = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar notificación pendiente - NotificacionId: {NotificacionId}", notificacion.id);
                }
            }

            await _contexto.SaveChangesAsync();

            _logger.LogInformation("Notificaciones pendientes procesadas - Cantidad: {Cantidad}", notificacionesPendientes.Count);
        }

        public async Task<NotificacionEstadisticasResponse> ObtenerEstadisticasAsync(int usuarioId)
        {
            var notificaciones = await _contexto.notificaciones
                .Where(n => n.id_usuario_destino == usuarioId)
                .ToListAsync();

            var estadisticas = new NotificacionEstadisticasResponse
            {
                TotalNotificaciones = notificaciones.Count,
                NoLeidas = notificaciones.Count(n => (n.leida == null || n.leida == false) && (n.archivada == null || n.archivada == false)),
                Archivadas = notificaciones.Count(n => n.archivada == true),
                Ultimas24Horas = notificaciones.Count(n => n.fecha_creacion >= DateTime.UtcNow.AddHours(-24)),
                PorPrioridad = notificaciones.GroupBy(n => n.prioridad ?? "MEDIA")
                    .ToDictionary(g => g.Key, g => g.Count()),
                PorTipo = notificaciones.GroupBy(n => n.id_tipo_notificacion.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return estadisticas;
        }

        #region Métodos Privados

        private async Task<List<usuario>> ObtenerUsuariosDestino(NotificacionMasivaRequest solicitud)
        {
            var query = _contexto.usuarios.AsQueryable();

            if (solicitud.IdsUsuarios != null && solicitud.IdsUsuarios.Any())
            {
                query = query.Where(u => solicitud.IdsUsuarios.Contains(u.id));
            }
            else if (solicitud.IdsRoles != null && solicitud.IdsRoles.Any())
            {
                query = query.Where(u => _contexto.usuarios_roles
                    .Any(ur => ur.id_usuario == u.id && solicitud.IdsRoles.Contains(ur.id_rol) && ur.estado == "ACTIVO"));
            }
            else if (solicitud.IdTorneo.HasValue)
            {
                query = query.Where(u => _contexto.usuarios_roles
                    .Any(ur => ur.id_usuario == u.id && ur.id_torneo == solicitud.IdTorneo.Value && ur.estado == "ACTIVO"));
            }
            else if (solicitud.IdEquipo.HasValue)
            {
                query = query.Where(u => _contexto.usuarios_roles
                    .Any(ur => ur.id_usuario == u.id && ur.id_equipo == solicitud.IdEquipo.Value && ur.estado == "ACTIVO"));
            }

            return await query.Where(u => u.activo == true).ToListAsync();
        }

        private async Task ProcesarEnvioNotificacion(
            notificacione notificacion,
            usuario usuarioDestino,
            List<string> canales,
            tipos_notificacion? tipoNotificacion)
        {
            var preferencias = await _contexto.preferencias_notificaciones
                .FirstOrDefaultAsync(p => p.id_usuario == usuarioDestino.id && p.id_tipo_notificacion == notificacion.id_tipo_notificacion);

            foreach (var canal in canales)
            {
                switch (canal.ToUpperInvariant())
                {
                    case "IN_APP":
                        notificacion.canal = "IN_APP";
                        break;

                    case "EMAIL":
                        if (preferencias?.email_activado != false)
                        {
                            try
                            {
                                await _emailService.EnviarNotificacionEmailAsync(
                                    usuarioDestino.email,
                                    notificacion.titulo,
                                    notificacion.mensaje,
                                    usuarioDestino.nombres);
                                notificacion.email_enviado = true;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error al enviar email de notificación - Destino: {Email}", usuarioDestino.email);
                            }
                        }
                        break;

                    case "PUSH":
                        // Implementar envío push (Firebase Cloud Messaging, etc.)
                        notificacion.push_enviado = true;
                        break;

                    case "SMS":
                        if (preferencias?.sms_activado != false && !string.IsNullOrEmpty(usuarioDestino.telefono))
                        {
                            // Implementar envío SMS
                            notificacion.sms_enviado = true;
                        }
                        break;
                }
            }
        }

        private async Task CrearPreferenciasPorDefecto(int usuarioId)
        {
            var tiposNotificacion = await _contexto.tipos_notificacions
                .Where(t => t.activo == true && t.permite_configuracion == true)
                .ToListAsync();

            foreach (var tipo in tiposNotificacion)
            {
                var preferencia = new preferencias_notificacione
                {
                    codigo = CodigoHelper.GenerarCodigo("PREF", 10),
                    id_usuario = usuarioId,
                    id_tipo_notificacion = tipo.id,
                    activado = true,
                    push_activado = true,
                    email_activado = true,
                    sms_activado = false,
                    fecha_modificacion = DateTime.UtcNow
                };
                _contexto.preferencias_notificaciones.Add(preferencia);
            }

            await _contexto.SaveChangesAsync();
        }

        public async Task<PreferenciaNotificacionResponse?> ObtenerPreferenciasPorTipoAsync(int usuarioId, int idTipoNotificacion)
        {
            var preferencia = await _contexto.preferencias_notificaciones
                .Where(p => p.id_usuario == usuarioId && p.id_tipo_notificacion == idTipoNotificacion)
                .Select(p => new PreferenciaNotificacionResponse
                {
                    Id = p.id,
                    IdTipoNotificacion = p.id_tipo_notificacion,

                    Activado = p.activado ?? false,
                    PushActivado = p.push_activado ?? false,
                    EmailActivado = p.email_activado ?? false,
                    SmsActivado = p.sms_activado ?? false,

                    TipoNotificacion = p.id_tipo_notificacionNavigation != null
                        ? p.id_tipo_notificacionNavigation.nombre
                        : string.Empty,

                    Categoria = p.id_tipo_notificacionNavigation != null
                        ? p.id_tipo_notificacionNavigation.categoria
                        : string.Empty,

                    FechaModificacion = p.fecha_modificacion ?? DateTime.MinValue
                })
                .FirstOrDefaultAsync();

            return preferencia;
        }

        private NotificacionResponse MapearNotificacionResponse(notificacione notificacion, tipos_notificacion? tipo = null)
        {
            return new NotificacionResponse
            {
                Id = notificacion.id,
                Codigo = notificacion.codigo,
                IdTipoNotificacion = notificacion.id_tipo_notificacion,
                TipoNotificacion = tipo?.nombre ?? notificacion.id_tipo_notificacionNavigation?.nombre ?? "",
                Titulo = notificacion.titulo,
                Mensaje = notificacion.mensaje,
                Prioridad = notificacion.prioridad ?? "MEDIA",
                Leida = notificacion.leida ?? false,
                FechaLectura = notificacion.fecha_lectura,
                Archivada = notificacion.archivada ?? false,
                FechaCreacion = notificacion.fecha_creacion ?? DateTime.UtcNow,
                FechaProgramadaEnvio = notificacion.fecha_programada_envio,
                Enviada = notificacion.enviada ?? false,
                FechaEnvio = notificacion.fecha_envio,
                Canal = notificacion.canal ?? "IN_APP",
                AccionUrl = notificacion.accion_url,
                AccionTipo = notificacion.accion_tipo,
                IdTorneo = notificacion.id_torneo,
                Torneo = notificacion.id_torneoNavigation?.nombre,
                IdEquipo = notificacion.id_equipo,
                Equipo = notificacion.id_equipoNavigation?.nombre,
                IdPartido = notificacion.id_partido,
                IdMulta = notificacion.id_multa,
                IdSuspension = notificacion.id_suspension
            };
        }

        #endregion
    }
}