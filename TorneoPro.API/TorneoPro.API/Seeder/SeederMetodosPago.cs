using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederMetodosPago
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederMetodosPago> _logger;

        public SeederMetodosPago(TorneoProContext context, ILogger<SeederMetodosPago> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.metodos_pagos.AnyAsync())
            {
                _logger.LogInformation("metodos_pagos ya contiene datos. Seeding omitido.");
                return;
            }

            var metodosPago = new List<metodos_pago>
            {
                new metodos_pago
                {
                    codigo = "EFECTIVO",
                    nombre = "Efectivo",
                    descripcion = "Pago en efectivo en oficinas",
                    requiere_comprobante = true,
                    es_automatico = false,
                    activo = true
                },
                new metodos_pago
                {
                    codigo = "TRANSFERENCIA",
                    nombre = "Transferencia Bancaria",
                    descripcion = "Transferencia bancaria nacional",
                    requiere_comprobante = true,
                    es_automatico = false,
                    activo = true
                },
                new metodos_pago
                {
                    codigo = "TARJETA_CREDITO",
                    nombre = "Tarjeta de Crédito",
                    descripcion = "Pago con tarjeta de crédito (online)",
                    requiere_comprobante = false,
                    es_automatico = true,
                    activo = true
                },
                new metodos_pago
                {
                    codigo = "TARJETA_DEBITO",
                    nombre = "Tarjeta de Débito",
                    descripcion = "Pago con tarjeta de débito (online)",
                    requiere_comprobante = false,
                    es_automatico = true,
                    activo = true
                },
                new metodos_pago
                {
                    codigo = "QR",
                    nombre = "Código QR",
                    descripcion = "Pago mediante código QR",
                    requiere_comprobante = false,
                    es_automatico = true,
                    activo = true
                },
                new metodos_pago
                {
                    codigo = "DEPOSITO",
                    nombre = "Depósito Bancario",
                    descripcion = "Depósito en cuenta bancaria",
                    requiere_comprobante = true,
                    es_automatico = false,
                    activo = true
                },
                new metodos_pago
                {
                    codigo = "PAYPAL",
                    nombre = "PayPal",
                    descripcion = "Pago mediante PayPal",
                    requiere_comprobante = false,
                    es_automatico = true,
                    activo = true
                }
            };

            await _context.metodos_pagos.AddRangeAsync(metodosPago);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Se insertaron {metodosPago.Count} métodos de pago");
        }
    }
}