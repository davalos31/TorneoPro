using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederTiposUsuario
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederTiposUsuario> _logger;

        public SeederTiposUsuario(TorneoProContext context, ILogger<SeederTiposUsuario> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.tipos_usuarios.AnyAsync())
            {
                _logger.LogInformation("tipos_usuarios ya contiene datos. Seeding omitido.");
                return;
            }

            var tiposUsuario = new List<tipos_usuario>
            {
                new tipos_usuario
                {
                    codigo = "JUG",
                    nombre = "Jugador",
                    descripcion = "Usuario con rol de jugador",
                    activo = true
                },
                new tipos_usuario
                {
                    codigo = "ARB",
                    nombre = "Árbitro",
                    descripcion = "Usuario con rol de árbitro",
                    activo = true
                },
                new tipos_usuario
                {
                    codigo = "CAP",
                    nombre = "Capitán",
                    descripcion = "Usuario con rol de capitán de equipo",
                    activo = true
                },
                new tipos_usuario
                {
                    codigo = "ADM",
                    nombre = "Administrador",
                    descripcion = "Usuario con permisos administrativos",
                    activo = true
                },
                new tipos_usuario
                {
                    codigo = "SUP",
                    nombre = "Super Administrador",
                    descripcion = "Usuario con permisos totales",
                    activo = true
                }
            };

            await _context.tipos_usuarios.AddRangeAsync(tiposUsuario);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {tiposUsuario.Count} tipos de usuario");
        }
    }
}