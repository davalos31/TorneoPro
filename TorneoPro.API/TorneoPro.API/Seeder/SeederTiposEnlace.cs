using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederTiposEnlace
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederTiposEnlace> _logger;

        public SeederTiposEnlace(TorneoProContext context, ILogger<SeederTiposEnlace> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.tipos_enlaces.AnyAsync())
            {
                _logger.LogInformation("tipos_enlaces ya contiene datos. Seeding omitido.");
                return;
            }

            var tiposEnlace = new List<tipos_enlace>
            {
                new tipos_enlace
                {
                    codigo = "CAPITAN",
                    nombre = "Enlace de Capitán",
                    descripcion = "Invitar a usuario como capitán de equipo",
                    permite_expiracion = true,
                    max_usos_recomendado = 1,
                    alcance = "EQUIPO",
                    activo = true
                },
                new tipos_enlace
                {
                    codigo = "ARBITRO",
                    nombre = "Enlace de Árbitro",
                    descripcion = "Invitar a usuario como árbitro de torneo",
                    permite_expiracion = true,
                    max_usos_recomendado = 1,
                    alcance = "TORNEO",
                    activo = true
                },
                new tipos_enlace
                {
                    codigo = "SUBADMIN",
                    nombre = "Enlace de Subadmin",
                    descripcion = "Invitar a usuario como subadministrador",
                    permite_expiracion = true,
                    max_usos_recomendado = 1,
                    alcance = "TORNEO",
                    activo = true
                },
                new tipos_enlace
                {
                    codigo = "JUGADOR",
                    nombre = "Enlace de Jugador",
                    descripcion = "Invitar a usuario como jugador de equipo",
                    permite_expiracion = true,
                    max_usos_recomendado = 1,
                    alcance = "EQUIPO",
                    activo = true
                },
                new tipos_enlace
                {
                    codigo = "EQUIPO",
                    nombre = "Enlace de Equipo",
                    descripcion = "Enlace para unirse a equipo",
                    permite_expiracion = true,
                    max_usos_recomendado = 25,
                    alcance = "EQUIPO",
                    activo = true
                },
                new tipos_enlace
                {
                    codigo = "TORNEO",
                    nombre = "Enlace de Torneo",
                    descripcion = "Enlace para inscribirse en torneo",
                    permite_expiracion = true,
                    max_usos_recomendado = 100,
                    alcance = "TORNEO",
                    activo = true
                }
            };

            await _context.tipos_enlaces.AddRangeAsync(tiposEnlace);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {tiposEnlace.Count} tipos de enlace");
        }
    }
}