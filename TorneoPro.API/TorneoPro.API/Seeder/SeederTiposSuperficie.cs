using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederTiposSuperficie
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederTiposSuperficie> _logger;

        public SeederTiposSuperficie(TorneoProContext context, ILogger<SeederTiposSuperficie> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.tipos_superficies.AnyAsync())
            {
                _logger.LogInformation("tipos_superficies ya contiene datos. Seeding omitido.");
                return;
            }

            var superficies = new List<tipos_superficie>
            {
                new tipos_superficie
                {
                    codigo = "CESPED_NATURAL",
                    nombre = "Césped Natural",
                    descripcion = "Superficie de césped natural",
                    activo = true
                },
                new tipos_superficie
                {
                    codigo = "CESPED_SINTETICO",
                    nombre = "Césped Sintético",
                    descripcion = "Superficie de césped sintético",
                    activo = true
                },
                new tipos_superficie
                {
                    codigo = "PARQUET",
                    nombre = "Parquet",
                    descripcion = "Superficie de madera (parquet)",
                    activo = true
                },
                new tipos_superficie
                {
                    codigo = "CEMENTO",
                    nombre = "Cemento",
                    descripcion = "Superficie de cemento pulido",
                    activo = true
                },
                new tipos_superficie
                {
                    codigo = "TIERRA",
                    nombre = "Tierra",
                    descripcion = "Superficie de tierra batida",
                    activo = true
                },
                new tipos_superficie
                {
                    codigo = "POLIURETANO",
                    nombre = "Poliuretano",
                    descripcion = "Superficie de poliuretano (atletismo, baloncesto)",
                    activo = true
                },
                new tipos_superficie
                {
                    codigo = "CAUCHO",
                    nombre = "Caucho",
                    descripcion = "Superficie de caucho sintético",
                    activo = true
                }
            };

            await _context.tipos_superficies.AddRangeAsync(superficies);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {superficies.Count} tipos de superficie");
        }
    }
}