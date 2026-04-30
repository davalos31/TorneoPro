using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederTiposDocumento
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederTiposDocumento> _logger;

        public SeederTiposDocumento(TorneoProContext context, ILogger<SeederTiposDocumento> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.tipos_documentos.AnyAsync())
            {
                _logger.LogInformation("tipos_documentos ya contiene datos. Seeding omitido.");
                return;
            }

            var tiposDocumento = new List<tipos_documento>
            {
                new tipos_documento
                {
                    codigo = "CI",
                    nombre = "Cédula de Identidad",
                    descripcion = "Cédula de identidad nacional",
                    requiere_foto = true,
                    patron_validacion = @"^\d{7,10}$",
                    activo = true
                },
                new tipos_documento
                {
                    codigo = "PAS",
                    nombre = "Pasaporte",
                    descripcion = "Pasaporte internacional",
                    requiere_foto = true,
                    patron_validacion = @"^[A-Z0-9]{6,12}$",
                    activo = true
                },
                new tipos_documento
                {
                    codigo = "RUC",
                    nombre = "RUC",
                    descripcion = "Registro Único de Contribuyentes",
                    requiere_foto = false,
                    patron_validacion = @"^\d{11,13}$",
                    activo = true
                },
                new tipos_documento
                {
                    codigo = "DNI",
                    nombre = "Documento Nacional de Identidad",
                    descripcion = "Documento nacional de identidad",
                    requiere_foto = true,
                    patron_validacion = @"^\d{8}$",
                    activo = true
                },
                new tipos_documento
                {
                    codigo = "LC",
                    nombre = "Libreta Cívica",
                    descripcion = "Libreta de enrolamiento cívico",
                    requiere_foto = true,
                    patron_validacion = @"^\d{7,8}$",
                    activo = true
                },
                new tipos_documento
                {
                    codigo = "LE",
                    nombre = "Libreta de Enrolamiento",
                    descripcion = "Libreta de enrolamiento militar",
                    requiere_foto = true,
                    patron_validacion = @"^\d{7,8}$",
                    activo = true
                }
            };

            await _context.tipos_documentos.AddRangeAsync(tiposDocumento);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {tiposDocumento.Count} tipos de documento");
        }
    }
}