using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TorneoPro.API.Data;

namespace TorneoPro.API.Seeder
{
    public class SeederPrincipal
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederPrincipal> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public SeederPrincipal(
            TorneoProContext context,
            ILogger<SeederPrincipal> logger,
            ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public async Task SeedAllAsync()
        {
            _logger.LogInformation("Iniciando proceso de seeding...");

            // Verificar si ya hay datos — si existe cualquier registro, omitir todo
            if (await HayDatosExistentesAsync())
            {
                _logger.LogInformation("La base de datos ya contiene datos. Seeding omitido.");
                return;
            }

            // ✅ FIX: SqlServerRetryingExecutionStrategy (EnableRetryOnFailure) NO permite
            // transacciones manuales directas. Se debe usar CreateExecutionStrategy().ExecuteAsync()
            // para que EF Core pueda reintentar toda la unidad de trabajo como un bloque atómico.
            // Documentación: https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    await SeedCatalogoAsync();
                    await SeedUsuariosAdminAsync();

                    await transaction.CommitAsync();
                    _logger.LogInformation("Seeding completado exitosamente.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error durante el seeding. Se realizó rollback completo.");
                    throw;
                }
            });
        }

        /// <summary>
        /// Verifica si alguna tabla clave ya tiene datos.
        /// Si cualquiera tiene registros, se asume que el seeding ya fue ejecutado.
        /// </summary>
        private async Task<bool> HayDatosExistentesAsync()
        {
            return await _context.tipos_documentos.AnyAsync() ||
                   await _context.tipos_usuarios.AnyAsync() ||
                   await _context.tipos_rols.AnyAsync() ||
                   await _context.deportes.AnyAsync() ||
                   await _context.usuarios.AnyAsync();
        }

        private async Task SeedCatalogoAsync()
        {
            _logger.LogInformation("Seeding catálogos...");

            // Orden respeta dependencias entre tablas (de menos a más dependiente)
            await new SeederTiposDocumento(_context, _loggerFactory.CreateLogger<SeederTiposDocumento>()).SeedAsync();
            await new SeederTiposUsuario(_context, _loggerFactory.CreateLogger<SeederTiposUsuario>()).SeedAsync();
            await new SeederTiposRol(_context, _loggerFactory.CreateLogger<SeederTiposRol>()).SeedAsync();
            await new SeederDeportes(_context, _loggerFactory.CreateLogger<SeederDeportes>()).SeedAsync();
            await new SeederFormatosTorneo(_context, _loggerFactory.CreateLogger<SeederFormatosTorneo>()).SeedAsync();
            await new SeederFasesCatalogo(_context, _loggerFactory.CreateLogger<SeederFasesCatalogo>()).SeedAsync();
            await new SeederTiposEnlace(_context, _loggerFactory.CreateLogger<SeederTiposEnlace>()).SeedAsync();
            await new SeederTiposNotificacion(_context, _loggerFactory.CreateLogger<SeederTiposNotificacion>()).SeedAsync();
            await new SeederTiposMulta(_context, _loggerFactory.CreateLogger<SeederTiposMulta>()).SeedAsync();
            await new SeederMetodosPago(_context, _loggerFactory.CreateLogger<SeederMetodosPago>()).SeedAsync();
            await new SeederMotivosReprogramacion(_context, _loggerFactory.CreateLogger<SeederMotivosReprogramacion>()).SeedAsync();
            await new SeederTiposSuperficie(_context, _loggerFactory.CreateLogger<SeederTiposSuperficie>()).SeedAsync();

            _logger.LogInformation("Catálogos seedeados correctamente.");
        }

        private async Task SeedUsuariosAdminAsync()
        {
            _logger.LogInformation("Seeding usuarios admin...");
            await new SeederUsuariosAdmin(_context, _loggerFactory.CreateLogger<SeederUsuariosAdmin>()).SeedAsync();
            _logger.LogInformation("Usuarios admin seedeados correctamente.");
        }

        /// <summary>
        /// Punto de entrada llamado desde Program.cs.
        /// El scope ya viene creado desde Program.cs — no se crea uno interno.
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<TorneoProContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<SeederPrincipal>>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            try
            {
                // Verificar conectividad antes de intentar migrar
                var puedeConectar = await context.Database.CanConnectAsync();
                if (!puedeConectar)
                {
                    logger.LogError("No se pudo conectar a la base de datos. Seeding cancelado.");
                    throw new InvalidOperationException("Sin conexión a la base de datos al iniciar.");
                }

                // Aplicar migraciones pendientes
                var migracionesPendientes = await context.Database.GetPendingMigrationsAsync();
                if (migracionesPendientes.Any())
                {
                    logger.LogInformation("Aplicando {Count} migración(es) pendiente(s)...",
                        migracionesPendientes.Count());
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Migraciones aplicadas correctamente.");
                }
                else
                {
                    logger.LogInformation("No hay migraciones pendientes.");
                }

                // Ejecutar seeding
                var seeder = new SeederPrincipal(context, logger, loggerFactory);
                await seeder.SeedAllAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al inicializar la base de datos.");
                throw;
            }
        }
    }
}