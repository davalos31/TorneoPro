using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederTiposNotificacion
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederTiposNotificacion> _logger;

        public SeederTiposNotificacion(TorneoProContext context, ILogger<SeederTiposNotificacion> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.tipos_notificacions.AnyAsync())
            {
                _logger.LogInformation("tipos_notificacions ya contiene datos. Seeding omitido.");
                return;
            }

            var tiposNotificacion = new List<tipos_notificacion>
            {
                // Sistema
                new tipos_notificacion
                {
                    codigo = "BIENVENIDA",
                    nombre = "Bienvenida",
                    descripcion = "Notificación de bienvenida al sistema",
                    categoria = "SISTEMA",
                    prioridad = "MEDIA",
                    permite_configuracion = true,
                    activo = true
                },
                new tipos_notificacion
                {
                    codigo = "VERIFICACION_EMAIL",
                    nombre = "Verificación de Email",
                    descripcion = "Notificación para verificar correo electrónico",
                    categoria = "SISTEMA",
                    prioridad = "ALTA",
                    permite_configuracion = false,
                    activo = true
                },
                
                // Torneo
                new tipos_notificacion
                {
                    codigo = "INICIO_TORNEO",
                    nombre = "Inicio de Torneo",
                    descripcion = "Notificación de inicio de torneo",
                    categoria = "TORNEO",
                    prioridad = "ALTA",
                    permite_configuracion = true,
                    activo = true
                },
                new tipos_notificacion
                {
                    codigo = "FIN_TORNEO",
                    nombre = "Fin de Torneo",
                    descripcion = "Notificación de finalización de torneo",
                    categoria = "TORNEO",
                    prioridad = "MEDIA",
                    permite_configuracion = true,
                    activo = true
                },
                new tipos_notificacion
                {
                    codigo = "INSCRIPCION_ABIERTA",
                    nombre = "Inscripción Abierta",
                    descripcion = "Notificación de apertura de inscripciones",
                    categoria = "TORNEO",
                    prioridad = "MEDIA",
                    permite_configuracion = true,
                    activo = true
                },
                
                // Equipo
                new tipos_notificacion
                {
                    codigo = "INVITACION_EQUIPO",
                    nombre = "Invitación a Equipo",
                    descripcion = "Notificación de invitación a un equipo",
                    categoria = "EQUIPO",
                    prioridad = "MEDIA",
                    permite_configuracion = true,
                    activo = true
                },
                new tipos_notificacion
                {
                    codigo = "APROBACION_EQUIPO",
                    nombre = "Aprobación de Equipo",
                    descripcion = "Notificación de aprobación de equipo en torneo",
                    categoria = "EQUIPO",
                    prioridad = "MEDIA",
                    permite_configuracion = true,
                    activo = true
                },
                
                // Partido
                new tipos_notificacion
                {
                    codigo = "PARTIDO_PROGRAMADO",
                    nombre = "Partido Programado",
                    descripcion = "Notificación de programación de partido",
                    categoria = "PARTIDO",
                    prioridad = "MEDIA",
                    permite_configuracion = true,
                    activo = true
                },
                new tipos_notificacion
                {
                    codigo = "PARTIDO_REPROGRAMADO",
                    nombre = "Partido Reprogramado",
                    descripcion = "Notificación de reprogramación de partido",
                    categoria = "PARTIDO",
                    prioridad = "ALTA",
                    permite_configuracion = true,
                    activo = true
                },
                new tipos_notificacion
                {
                    codigo = "RESULTADO_PARTIDO",
                    nombre = "Resultado de Partido",
                    descripcion = "Notificación de resultado final de partido",
                    categoria = "PARTIDO",
                    prioridad = "MEDIA",
                    permite_configuracion = true,
                    activo = true
                },
                
                // Financiero
                new tipos_notificacion
                {
                    codigo = "MULTA_GENERADA",
                    nombre = "Multa Generada",
                    descripcion = "Notificación de generación de multa",
                    categoria = "FINANCIERO",
                    prioridad = "ALTA",
                    permite_configuracion = true,
                    activo = true
                },
                new tipos_notificacion
                {
                    codigo = "MULTA_VENCIDA",
                    nombre = "Multa Vencida",
                    descripcion = "Notificación de multa vencida",
                    categoria = "FINANCIERO",
                    prioridad = "URGENTE",
                    permite_configuracion = true,
                    activo = true
                },
                new tipos_notificacion
                {
                    codigo = "PAGO_REGISTRADO",
                    nombre = "Pago Registrado",
                    descripcion = "Notificación de registro de pago",
                    categoria = "FINANCIERO",
                    prioridad = "MEDIA",
                    permite_configuracion = true,
                    activo = true
                },
                
                // Disciplina
                new tipos_notificacion
                {
                    codigo = "SUSPENSION_JUGADOR",
                    nombre = "Suspensión de Jugador",
                    descripcion = "Notificación de suspensión de jugador",
                    categoria = "DISCIPLINA",
                    prioridad = "ALTA",
                    permite_configuracion = true,
                    activo = true
                },
                new tipos_notificacion
                {
                    codigo = "PERFIL_ACTUALIZADO",
                    nombre = "Perfil Actualizado",
                    descripcion = "Notificación de actualización de perfil",
                    categoria = "SISTEMA",
                    prioridad = "BAJA",
                    permite_configuracion = true,
                    activo = true
                },
                new tipos_notificacion
                {
                    codigo = "REHABILITACION_JUGADOR",
                    nombre = "Rehabilitación de Jugador",
                    descripcion = "Notificación de rehabilitación de jugador",
                    categoria = "DISCIPLINA",
                    prioridad = "MEDIA",
                    permite_configuracion = true,
                    activo = true
                },
                new tipos_notificacion
                {
                    codigo = "REMOVIDO_EQUIPO",
                    nombre = "Removido de Equipo",
                    descripcion = "Notificación cuando un jugador es removido de un equipo",
                    categoria = "EQUIPO",
                    prioridad = "MEDIA",
                    permite_configuracion = true,
                    activo = true
                }
            };

            await _context.tipos_notificacions.AddRangeAsync(tiposNotificacion);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {tiposNotificacion.Count} tipos de notificación");
        }
    }
}