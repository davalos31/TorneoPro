using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TorneoPro.API.Data;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;

namespace TorneoPro.API.Seeder
{
    public class SeederUsuariosAdmin
    {
        private readonly TorneoProContext _context;
        private readonly ILogger<SeederUsuariosAdmin> _logger;

        public SeederUsuariosAdmin(TorneoProContext context, ILogger<SeederUsuariosAdmin> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            // Verificar si ya existen usuarios admin por email
            if (await _context.usuarios.AnyAsync(u => u.email == "superadmin@torneopro.com" || u.email == "admin@torneopro.com"))
            {
                _logger.LogInformation("Usuarios administradores ya existen. Seeding omitido.");
                return;
            }

            // Obtener tipos necesarios
            var tipoDocumento = await _context.tipos_documentos.FirstOrDefaultAsync(t => t.codigo == "CI");
            var tipoUsuarioSuperAdmin = await _context.tipos_usuarios.FirstOrDefaultAsync(t => t.codigo == "SUP");
            var tipoUsuarioAdmin = await _context.tipos_usuarios.FirstOrDefaultAsync(t => t.codigo == "ADM");
            var rolSuperAdmin = await _context.tipos_rols.FirstOrDefaultAsync(r => r.codigo == "SUPER_ADMIN");
            var rolAdmin = await _context.tipos_rols.FirstOrDefaultAsync(r => r.codigo == "ADMIN");

            var usuariosAdmin = new List<usuario>();
            var usuariosRoles = new List<usuarios_role>();

            // Fecha actual como DateTime
            var fechaActual = DateTime.UtcNow;

            // Super Administrador
            var superAdmin = new usuario
            {
                codigo = CodigoHelper.GenerarCodigo("SA", 8),
                id_tipo_usuario = tipoUsuarioSuperAdmin?.id ?? 5,
                nombres = "Super",
                apellidos = "Administrador",
                email = "superadmin@torneopro.com",
                telefono = "77777777",
                email_verificado = true,
                telefono_verificado = true,
                activo = true,
                fecha_registro = fechaActual,
                // Asignar documento único para evitar NULL, NULL
                id_tipo_documento = tipoDocumento?.id ?? 1,
                numero_documento = "SA-ADMIN-001"
            };

            var salt = HashHelper.GenerateSalt();
            superAdmin.password_hash = HashHelper.HashPassword("Admin123!", salt);
            superAdmin.salt = salt;

            usuariosAdmin.Add(superAdmin);

            // Administrador General
            var adminGeneral = new usuario
            {
                codigo = CodigoHelper.GenerarCodigo("AD", 8),
                id_tipo_usuario = tipoUsuarioAdmin?.id ?? 2,
                nombres = "Admin",
                apellidos = "General",
                email = "admin@torneopro.com",
                telefono = "77777778",
                email_verificado = true,
                telefono_verificado = true,
                activo = true,
                fecha_registro = fechaActual,
                // Asignar documento único para evitar NULL, NULL
                id_tipo_documento = tipoDocumento?.id ?? 1,
                numero_documento = "AD-ADMIN-001"
            };

            var saltAdmin = HashHelper.GenerateSalt();
            adminGeneral.password_hash = HashHelper.HashPassword("Admin123!", saltAdmin);
            adminGeneral.salt = saltAdmin;

            usuariosAdmin.Add(adminGeneral);

            await _context.usuarios.AddRangeAsync(usuariosAdmin);
            await _context.SaveChangesAsync();

            // Asignar roles
            if (rolSuperAdmin != null && superAdmin.id > 0)
            {
                usuariosRoles.Add(new usuarios_role
                {
                    codigo = CodigoHelper.GenerarCodigo("UR", 8),
                    id_usuario = superAdmin.id,
                    id_rol = rolSuperAdmin.id,
                    fecha_inicio = DateOnly.FromDateTime(fechaActual),
                    estado = "ACTIVO",
                    origen_asignacion = "AUTOMATICO",
                    fecha_asignacion = fechaActual,
                    activo = true
                });
            }

            if (rolAdmin != null && adminGeneral.id > 0)
            {
                usuariosRoles.Add(new usuarios_role
                {
                    codigo = CodigoHelper.GenerarCodigo("UR", 8),
                    id_usuario = adminGeneral.id,
                    id_rol = rolAdmin.id,
                    fecha_inicio = DateOnly.FromDateTime(fechaActual),
                    estado = "ACTIVO",
                    origen_asignacion = "AUTOMATICO",
                    fecha_asignacion = fechaActual,
                    activo = true
                });
            }

            if (usuariosRoles.Any())
            {
                await _context.usuarios_roles.AddRangeAsync(usuariosRoles);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Se insertaron {usuariosAdmin.Count} usuarios administradores");
            _logger.LogInformation("Credenciales de acceso:");
            _logger.LogInformation("Super Administrador: superadmin@torneopro.com / Admin123!");
            _logger.LogInformation("Administrador General: admin@torneopro.com / Admin123!");
        }
    }
}