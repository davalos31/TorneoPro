using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederFormatosTorneo
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederFormatosTorneo> _logger;

        public SeederFormatosTorneo(TorneoProContext context, ILogger<SeederFormatosTorneo> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.tipos_formato_torneos.AnyAsync())
            {
                _logger.LogInformation("tipos_formato_torneos ya contiene datos. Seeding omitido.");
                return;
            }

            var formatos = new List<tipos_formato_torneo>
            {
                new tipos_formato_torneo
                {
                    codigo = "LIGA",
                    nombre = "Liga",
                    descripcion = "Todos contra todos, puntos por partido",
                    activo = true
                },
                new tipos_formato_torneo
                {
                    codigo = "ELIMINATORIA",
                    nombre = "Eliminatoria",
                    descripcion = "Eliminación directa con llaves",
                    activo = true
                },
                new tipos_formato_torneo
                {
                    codigo = "GRUPOS_ELIMINATORIA",
                    nombre = "Grupos + Eliminatoria",
                    descripcion = "Fase de grupos seguida de eliminación directa",
                    activo = true
                }
            };

            await _context.tipos_formato_torneos.AddRangeAsync(formatos);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {formatos.Count} formatos de torneo");
        }
    }
}