using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederMotivosReprogramacion
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederMotivosReprogramacion> _logger;

        public SeederMotivosReprogramacion(TorneoProContext context, ILogger<SeederMotivosReprogramacion> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.motivos_reprogramacions.AnyAsync())
            {
                _logger.LogInformation("motivos_reprogramacions ya contiene datos. Seeding omitido.");
                return;
            }

            var motivos = new List<motivos_reprogramacion>
            {
                new motivos_reprogramacion
                {
                    codigo = "CLIMA",
                    nombre = "Condiciones Climáticas",
                    descripcion = "Reprogramación por mal clima (lluvia, tormenta, etc.)",
                    requiere_aprobacion = true,
                    activo = true
                },
                new motivos_reprogramacion
                {
                    codigo = "CANCHA",
                    nombre = "Indisponibilidad de Cancha",
                    descripcion = "La cancha no está disponible en la fecha programada",
                    requiere_aprobacion = true,
                    activo = true
                },
                new motivos_reprogramacion
                {
                    codigo = "ARBITRO",
                    nombre = "Indisponibilidad de Árbitro",
                    descripcion = "El árbitro asignado no puede asistir",
                    requiere_aprobacion = true,
                    activo = true
                },
                new motivos_reprogramacion
                {
                    codigo = "EQUIPO",
                    nombre = "Solicitud de Equipo",
                    descripcion = "Solicitud de reprogramación por parte de un equipo",
                    requiere_aprobacion = true,
                    activo = true
                },
                new motivos_reprogramacion
                {
                    codigo = "FUERZA_MAYOR",
                    nombre = "Fuerza Mayor",
                    descripcion = "Caso de fuerza mayor (emergencia, accidente, etc.)",
                    requiere_aprobacion = true,
                    activo = true
                },
                new motivos_reprogramacion
                {
                    codigo = "FECHA_FESTIVA",
                    nombre = "Fecha Festiva",
                    descripcion = "Coincidencia con fecha festiva importante",
                    requiere_aprobacion = false,
                    activo = true
                },
                new motivos_reprogramacion
                {
                    codigo = "MANTENIMIENTO",
                    nombre = "Mantenimiento de Cancha",
                    descripcion = "La cancha está en mantenimiento",
                    requiere_aprobacion = true,
                    activo = true
                },
                new motivos_reprogramacion
                {
                    codigo = "OTRO",
                    nombre = "Otro Motivo",
                    descripcion = "Otro motivo no especificado",
                    requiere_aprobacion = true,
                    activo = true
                }
            };

            await _context.motivos_reprogramacions.AddRangeAsync(motivos);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {motivos.Count} motivos de reprogramación");
        }
    }
}