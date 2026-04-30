using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederDeportes
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederDeportes> _logger;

        public SeederDeportes(TorneoProContext context, ILogger<SeederDeportes> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.deportes.AnyAsync())
            {
                _logger.LogInformation("deportes ya contiene datos. Seeding omitido.");
                return;
            }

            var deportes = new List<deporte>
            {
                new deporte
                {
                    codigo = "FUT",
                    nombre = "Fútbol",
                    descripcion = "Fútbol 11",
                    jugadores_por_equipo = 11,
                    permite_empate = true,
                    tiempo_partido_minutos = 90,
                    tiempo_medio_tiempo_minutos = 45,
                    permite_prorroga = true,
                    tiempo_prorroga_minutos = 30,
                    permite_penales = true,
                    activo = true
                },
                new deporte
                {
                    codigo = "FUT7",
                    nombre = "Fútbol 7",
                    descripcion = "Fútbol 7",
                    jugadores_por_equipo = 7,
                    permite_empate = true,
                    tiempo_partido_minutos = 60,
                    tiempo_medio_tiempo_minutos = 30,
                    permite_prorroga = true,
                    tiempo_prorroga_minutos = 20,
                    permite_penales = true,
                    activo = true
                },
                new deporte
                {
                    codigo = "FUTSAL",
                    nombre = "Futsal",
                    descripcion = "Fútbol de salón",
                    jugadores_por_equipo = 5,
                    permite_empate = true,
                    tiempo_partido_minutos = 40,
                    tiempo_medio_tiempo_minutos = 20,
                    permite_prorroga = false,
                    tiempo_prorroga_minutos = 0,
                    permite_penales = true,
                    activo = true
                },
                new deporte
                {
                    codigo = "BAS",
                    nombre = "Baloncesto",
                    descripcion = "Baloncesto",
                    jugadores_por_equipo = 5,
                    permite_empate = false,
                    tiempo_partido_minutos = 40,
                    tiempo_medio_tiempo_minutos = 20,
                    permite_prorroga = true,
                    tiempo_prorroga_minutos = 10,
                    permite_penales = false,
                    activo = true
                },
                new deporte
                {
                    codigo = "VOL",
                    nombre = "Voleibol",
                    descripcion = "Voleibol",
                    jugadores_por_equipo = 6,
                    permite_empate = false,
                    tiempo_partido_minutos = 0,
                    tiempo_medio_tiempo_minutos = 0,
                    permite_prorroga = false,
                    tiempo_prorroga_minutos = 0,
                    permite_penales = false,
                    activo = true
                },
                new deporte
                {
                    codigo = "HAN",
                    nombre = "Handball",
                    descripcion = "Balonmano",
                    jugadores_por_equipo = 7,
                    permite_empate = true,
                    tiempo_partido_minutos = 60,
                    tiempo_medio_tiempo_minutos = 30,
                    permite_prorroga = true,
                    tiempo_prorroga_minutos = 10,
                    permite_penales = true,
                    activo = true
                }
            };

            await _context.deportes.AddRangeAsync(deportes);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {deportes.Count} deportes");
        }
    }
}