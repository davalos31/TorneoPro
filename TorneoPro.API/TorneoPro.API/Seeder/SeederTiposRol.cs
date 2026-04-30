using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederTiposRol
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederTiposRol> _logger;

        public SeederTiposRol(TorneoProContext context, ILogger<SeederTiposRol> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.tipos_rols.AnyAsync())
            {
                _logger.LogInformation("tipos_rols ya contiene datos. Seeding omitido.");
                return;
            }

            var tiposRol = new List<tipos_rol>
            {
                new tipos_rol
                {
                    codigo = "SUPER_ADMIN",
                    nombre = "Super Administrador",
                    descripcion = "Control total del sistema",
                    nivel_jerarquia = 1,
                    activo = true
                },
                new tipos_rol
                {
                    codigo = "ADMIN",
                    nombre = "Administrador",
                    descripcion = "Administración general de torneos",
                    nivel_jerarquia = 2,
                    activo = true
                },
                new tipos_rol
                {
                    codigo = "SUB_ADMIN",
                    nombre = "Sub Administrador",
                    descripcion = "Administración limitada a torneos asignados",
                    nivel_jerarquia = 3,
                    activo = true
                },
                new tipos_rol
                {
                    codigo = "ARBITRO",
                    nombre = "Árbitro",
                    descripcion = "Gestión de partidos y actas",
                    nivel_jerarquia = 4,
                    activo = true
                },
                new tipos_rol
                {
                    codigo = "CAPITAN",
                    nombre = "Capitán",
                    descripcion = "Gestión de equipo y jugadores",
                    nivel_jerarquia = 5,
                    activo = true
                },
                new tipos_rol
                {
                    codigo = "JUGADOR",
                    nombre = "Jugador",
                    descripcion = "Participación en torneos",
                    nivel_jerarquia = 6,
                    activo = true
                }
            };

            await _context.tipos_rols.AddRangeAsync(tiposRol);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {tiposRol.Count} tipos de rol");
        }
    }
}