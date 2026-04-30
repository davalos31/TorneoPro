using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederFasesCatalogo
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederFasesCatalogo> _logger;

        public SeederFasesCatalogo(TorneoProContext context, ILogger<SeederFasesCatalogo> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.fases_torneo_catalogos.AnyAsync())
            {
                _logger.LogInformation("fases_torneo_catalogos ya contiene datos. Seeding omitido.");
                return;
            }

            var fases = new List<fases_torneo_catalogo>
            {
                new fases_torneo_catalogo
                {
                    codigo = "GRUPOS",
                    nombre = "Fase de Grupos",
                    orden = 1,
                    equipos_requeridos = null,
                    descripcion = "Fase inicial con grupos",
                    activo = true
                },
                new fases_torneo_catalogo
                {
                    codigo = "DIECISEISAVOS",
                    nombre = "Dieciseisavos de Final",
                    orden = 2,
                    equipos_requeridos = 32,
                    descripcion = "Ronda de 32 equipos",
                    activo = true
                },
                new fases_torneo_catalogo
                {
                    codigo = "OCTAVOS",
                    nombre = "Octavos de Final",
                    orden = 3,
                    equipos_requeridos = 16,
                    descripcion = "Ronda de 16 equipos",
                    activo = true
                },
                new fases_torneo_catalogo
                {
                    codigo = "CUARTOS",
                    nombre = "Cuartos de Final",
                    orden = 4,
                    equipos_requeridos = 8,
                    descripcion = "Ronda de 8 equipos",
                    activo = true
                },
                new fases_torneo_catalogo
                {
                    codigo = "SEMIFINALES",
                    nombre = "Semifinales",
                    orden = 5,
                    equipos_requeridos = 4,
                    descripcion = "Ronda de 4 equipos",
                    activo = true
                },
                new fases_torneo_catalogo
                {
                    codigo = "TERCER_LUGAR",
                    nombre = "Tercer Lugar",
                    orden = 6,
                    equipos_requeridos = 2,
                    descripcion = "Partido por el tercer puesto",
                    activo = true
                },
                new fases_torneo_catalogo
                {
                    codigo = "FINAL",
                    nombre = "Final",
                    orden = 7,
                    equipos_requeridos = 2,
                    descripcion = "Partido final del torneo",
                    activo = true
                }
            };

            await _context.fases_torneo_catalogos.AddRangeAsync(fases);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {fases.Count} fases de torneo");
        }
    }
}