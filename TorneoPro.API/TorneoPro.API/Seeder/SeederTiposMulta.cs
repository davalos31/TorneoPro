using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederTiposMulta
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederTiposMulta> _logger;

        public SeederTiposMulta(TorneoProContext context, ILogger<SeederTiposMulta> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.tipos_multa.AnyAsync())
            {
                _logger.LogInformation("tipos_multa ya contiene datos. Seeding omitido.");
                return;
            }

            var tiposMulta = new List<tipos_multum>
            {
                // Disciplinarias
                new tipos_multum
                {
                    codigo = "TARJETA_AMARILLA",
                    nombre = "Tarjeta Amarilla",
                    descripcion = "Multa por acumulación de tarjetas amarillas",
                    categoria = "DISCIPLINARIA",
                    activo = true
                },
                new tipos_multum
                {
                    codigo = "TARJETA_ROJA",
                    nombre = "Tarjeta Roja",
                    descripcion = "Multa por expulsión",
                    categoria = "DISCIPLINARIA",
                    activo = true
                },
                new tipos_multum
                {
                    codigo = "AGRESION",
                    nombre = "Agresión",
                    descripcion = "Multa por agresión a jugador o árbitro",
                    categoria = "DISCIPLINARIA",
                    activo = true
                },
                
                // Administrativas
                new tipos_multum
                {
                    codigo = "INSCRIPCION_TARDIA",
                    nombre = "Inscripción Tardía",
                    descripcion = "Multa por inscripción fuera de plazo",
                    categoria = "ADMINISTRATIVA",
                    activo = true
                },
                new tipos_multum
                {
                    codigo = "DOCUMENTACION_INCOMPLETA",
                    nombre = "Documentación Incompleta",
                    descripcion = "Multa por documentación incompleta",
                    categoria = "ADMINISTRATIVA",
                    activo = true
                },
                new tipos_multum
                {
                    codigo = "INCUMPLIMIENTO_REGLAMENTO",
                    nombre = "Incumplimiento de Reglamento",
                    descripcion = "Multa por incumplimiento del reglamento",
                    categoria = "ADMINISTRATIVA",
                    activo = true
                },
                
                // Reglamentarias
                new tipos_multum
                {
                    codigo = "WALKOVER",
                    nombre = "Walk Over",
                    descripcion = "Multa por no presentarse a un partido",
                    categoria = "REGLAMENTARIA",
                    activo = true
                },
                new tipos_multum
                {
                    codigo = "ALINEACION_INDEBIDA",
                    nombre = "Alineación Indebida",
                    descripcion = "Multa por alineación de jugador no habilitado",
                    categoria = "REGLAMENTARIA",
                    activo = true
                },
                new tipos_multum
                {
                    codigo = "CONDUCTA_ANTIDEPORTIVA",
                    nombre = "Conducta Antideportiva",
                    descripcion = "Multa por conducta antideportiva",
                    categoria = "REGLAMENTARIA",
                    activo = true
                },
                new tipos_multum
                {
                    codigo = "DANO_INSTALACIONES",
                    nombre = "Daño a Instalaciones",
                    descripcion = "Multa por daño a instalaciones deportivas",
                    categoria = "REGLAMENTARIA",
                    activo = true
                }
            };

            await _context.tipos_multa.AddRangeAsync(tiposMulta);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {tiposMulta.Count} tipos de multa");
        }
    }
}