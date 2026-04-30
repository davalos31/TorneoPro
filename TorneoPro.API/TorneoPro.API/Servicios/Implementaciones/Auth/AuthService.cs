using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using TorneoPro.API.Data;
using TorneoPro.API.DTOs.Auth;
using TorneoPro.API.DTOs.Auth.Request;
using TorneoPro.API.DTOs.Auth.Response;
using TorneoPro.API.DTOs.Notificaciones;
using TorneoPro.API.DTOs.Usuarios;
using TorneoPro.API.DTOs.Usuarios.Response;
using TorneoPro.API.Helpers;
using TorneoPro.API.Models;
using TorneoPro.API.Servicios.Interfaces.Auth;
using TorneoPro.API.Servicios.Interfaces.Email;
using TorneoPro.API.Servicios.Interfaces.Notificacion;

namespace TorneoPro.API.Servicios.Implementaciones.Auth
{
    public class AuthService : IAuthService
    {
        private readonly TorneoProContext _contexto;
        private readonly JwtHelper _jwtHelper;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuracion;
        private readonly INotificacionService _notificacionService;

        public AuthService(
            TorneoProContext contexto,
            JwtHelper jwtHelper,
            IEmailService emailService,
            ILogger<AuthService> logger,
            IConfiguration configuracion,
            INotificacionService notificacionService)
        {
            _contexto = contexto;
            _jwtHelper = jwtHelper;
            _emailService = emailService;
            _logger = logger;
            _configuracion = configuracion;
            _notificacionService = notificacionService;
        }

     
        public async Task<AuthResponse> IniciarSesionAsync(LoginRequest solicitud)
        {
            var usuario = await _contexto.usuarios
                .Include(u => u.id_tipo_usuarioNavigation)
                .Include(u => u.id_tipo_documentoNavigation)  
                .FirstOrDefaultAsync(u => u.email == solicitud.Email && u.activo == true);

            if (usuario == null)
            {
                throw new UnauthorizedAccessException("Email o contraseña incorrectos");
            }

            if (!HashHelper.VerifyPassword(solicitud.Password, usuario.password_hash))
            {
                usuario.intentos_fallidos++;
                await _contexto.SaveChangesAsync();

                if (usuario.intentos_fallidos >= 5)
                {
                    usuario.bloqueado_hasta = DateTime.UtcNow.AddMinutes(60);
                    await _contexto.SaveChangesAsync();
                    throw new UnauthorizedAccessException("Cuenta bloqueada por 60 minutos debido a múltiples intentos fallidos");
                }

                throw new UnauthorizedAccessException("Email o contraseña incorrectos");
            }

            if (usuario.email_verificado != true)
            {
                throw new UnauthorizedAccessException("Debes verificar tu email antes de iniciar sesión");
            }

            if (usuario.bloqueado_hasta.HasValue && usuario.bloqueado_hasta.Value > DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException($"Cuenta bloqueada hasta {usuario.bloqueado_hasta.Value:HH:mm}");
            }

            // Resetear intentos fallidos
            usuario.intentos_fallidos = 0;
            usuario.bloqueado_hasta = null;
            usuario.fecha_ultima_conexion = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();

            var roles = await ObtenerRolesUsuario(usuario.id);
            var accessToken = _jwtHelper.GenerateAccessToken(usuario, roles);
            var refreshToken = _jwtHelper.GenerateRefreshToken();
            var expirationMinutes = int.Parse(_configuracion["Jwt:AccessTokenExpirationMinutes"] ?? "60");
            await GuardarRefreshToken(usuario.id, refreshToken);

            var zonaBolivia = TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time");
            var ahoraBolivia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaBolivia);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = ahoraBolivia.AddMinutes(expirationMinutes),
                Usuario = MapearUsuarioAuthResponse(usuario, roles) 
            };
        }

        
        public async Task<UsuarioResponse> RegistrarAsync(RegistrarUsuarioRequest solicitud)
        {

            var emailExiste = await _contexto.usuarios.AnyAsync(u => u.email == solicitud.Email);
            if (emailExiste)
            {
                throw new InvalidOperationException("El email ya está registrado");
            }

         
            var tipoDocumentoExiste = await _contexto.tipos_documentos
                .AnyAsync(td => td.id == solicitud.IdTipoDocumento && td.activo == true);

            if (!tipoDocumentoExiste)
            {
                throw new InvalidOperationException("El tipo de documento seleccionado no es válido");
            }

           
            var documentoExiste = await _contexto.usuarios
                .AnyAsync(u => u.numero_documento == solicitud.NumeroDocumento
                               && u.id_tipo_documento == solicitud.IdTipoDocumento);

            if (documentoExiste)
            {
                throw new InvalidOperationException("El número de documento ya está registrado para este tipo de documento");
            }

            var esDocumentoValido = ValidarFormatoDocumento(solicitud.IdTipoDocumento, solicitud.NumeroDocumento);
            if (!esDocumentoValido)
            {
                throw new InvalidOperationException("El formato del número de documento no es válido para el tipo seleccionado");
            }


            var usuario = new usuario
            {
                codigo = CodigoHelper.GenerarCodigo("USR", 8),
                nombres = solicitud.Nombres,
                apellidos = solicitud.Apellidos,
                email = solicitud.Email,
                telefono = solicitud.Telefono,
                id_tipo_usuario = solicitud.IdTipoUsuario,
                id_tipo_documento = solicitud.IdTipoDocumento,
                numero_documento = solicitud.NumeroDocumento,
                activo = true,
                email_verificado = false,
                fecha_registro = DateTime.UtcNow
            };


            var salt = HashHelper.GenerateSalt();
            usuario.password_hash = HashHelper.HashPassword(solicitud.Password, salt);
            usuario.salt = salt;

            _contexto.usuarios.Add(usuario);
            await _contexto.SaveChangesAsync();

            var rolJugador = await _contexto.tipos_rols.FirstOrDefaultAsync(r => r.codigo == "JUGADOR");
            if (rolJugador != null)
            {
                var usuarioRol = new usuarios_role
                {
                    codigo = CodigoHelper.GenerarCodigo("UR", 8),
                    id_usuario = usuario.id,
                    id_rol = rolJugador.id,
                    fecha_inicio = DateOnly.FromDateTime(DateTime.UtcNow),
                    estado = "ACTIVO",
                    origen_asignacion = "AUTOMATICO"
                };
                _contexto.usuarios_roles.Add(usuarioRol);
                await _contexto.SaveChangesAsync();
            }

            var tokenVerificacion = CodigoHelper.GenerarTokenVerificacion();
            var tokenRecuperacion = new tokens_recuperacion
            {
                id_usuario = usuario.id,
                token = tokenVerificacion,
                fecha_expiracion = DateTime.UtcNow.AddHours(24),
                activo = true
            };
            _contexto.tokens_recuperacions.Add(tokenRecuperacion);
            await _contexto.SaveChangesAsync();

            
            await _emailService.EnviarVerificacionEmailAsync(usuario.email, tokenVerificacion, usuario.nombres);

           
            var usuarioConRelaciones = await _contexto.usuarios
                .Include(u => u.id_tipo_usuarioNavigation)
                .Include(u => u.id_tipo_documentoNavigation)  
                .FirstOrDefaultAsync(u => u.id == usuario.id);

           
            var roles = await ObtenerRolesUsuario(usuario.id);
            var usuarioResponse = MapearUsuarioResponse(usuarioConRelaciones!, roles);

            return usuarioResponse;
        }


        public async Task<AuthResponse> RenovarTokenAsync(string tokenActualizacion)
        {
            var token = await _contexto.tokens_recuperacions
                .FirstOrDefaultAsync(t => t.token == tokenActualizacion && t.activo == true && t.usado == false);

            if (token == null || token.fecha_expiracion < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Token de actualización inválido o expirado");
            }

            var usuario = await _contexto.usuarios
                .Include(u => u.id_tipo_usuarioNavigation)
                .FirstOrDefaultAsync(u => u.id == token.id_usuario && u.activo == true);

            if (usuario == null)
            {
                throw new UnauthorizedAccessException("Usuario no encontrado");
            }

            
            token.usado = true;
            await _contexto.SaveChangesAsync();

            var roles = await ObtenerRolesUsuario(usuario.id);
            var nuevoAccessToken = _jwtHelper.GenerateAccessToken(usuario, roles);
            var nuevoRefreshToken = _jwtHelper.GenerateRefreshToken();

            await GuardarRefreshToken(usuario.id, nuevoRefreshToken);

            return new AuthResponse
            {
                AccessToken = nuevoAccessToken,
                RefreshToken = nuevoRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                Usuario = MapearUsuarioAuthResponse(usuario, roles)
            };
        }

        public async Task CerrarSesionAsync(int usuarioId)
        {
            var tokens = await _contexto.tokens_recuperacions
                .Where(t => t.id_usuario == usuarioId && t.activo == true && t.usado == false)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.usado = true;
            }

            await _contexto.SaveChangesAsync();
        }

        public async Task OlvideContrasenaAsync(string email)
        {
            var usuario = await _contexto.usuarios.FirstOrDefaultAsync(u => u.email == email && u.activo == true);
            if (usuario == null)
            {
                return;
            }

            var token = CodigoHelper.GenerarTokenVerificacion();
            var tokenRecuperacion = new tokens_recuperacion
            {
                id_usuario = usuario.id,
                token = token,
                fecha_expiracion = DateTime.UtcNow.AddHours(24),
                activo = true
            };

            _contexto.tokens_recuperacions.Add(tokenRecuperacion);
            await _contexto.SaveChangesAsync();

            await _emailService.EnviarRecuperacionPasswordAsync(usuario.email, token, usuario.nombres);
        }

        public async Task RestablecerContrasenaAsync(RestablecerPasswordRequest solicitud)
        {
            var token = await _contexto.tokens_recuperacions
                .FirstOrDefaultAsync(t => t.token == solicitud.Token && t.activo == true && t.usado == false);

            if (token == null || token.fecha_expiracion < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Token inválido o expirado");
            }

            var usuario = await _contexto.usuarios.FindAsync(token.id_usuario);
            if (usuario == null || usuario.email != solicitud.Email)
            {
                throw new InvalidOperationException("Email no coincide con el token");
            }

            var salt = HashHelper.GenerateSalt();
            usuario.password_hash = HashHelper.HashPassword(solicitud.NewPassword, salt);
            usuario.salt = salt;
            usuario.intentos_fallidos = 0;

            token.usado = true;
            await _contexto.SaveChangesAsync();
            await _notificacionService.EnviarNotificacionAsync(usuario.id, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = usuario.id,
                IdTipoNotificacion = 2, 
                Titulo = "Contraseña actualizada",
                Mensaje = "Tu contraseña ha sido restablecida exitosamente. Si no fuiste tú, contacta al administrador inmediatamente.",
                Prioridad = "ALTA"
            });
        }

        public async Task VerificarEmailAsync(string token)
        {
            var tokenRecuperacion = await _contexto.tokens_recuperacions
                .FirstOrDefaultAsync(t => t.token == token && t.activo == true && t.usado == false);

            if (tokenRecuperacion == null || tokenRecuperacion.fecha_expiracion < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Token inválido o expirado");
            }

            var usuario = await _contexto.usuarios.FindAsync(tokenRecuperacion.id_usuario);
            if (usuario == null)
            {
                throw new InvalidOperationException("Usuario no encontrado");
            }

            usuario.email_verificado = true;
            tokenRecuperacion.usado = true;
            await _contexto.SaveChangesAsync();

            await _notificacionService.EnviarNotificacionAsync(usuario.id, new EnviarNotificacionRequest
            {
                IdUsuarioDestino = usuario.id,
                IdTipoNotificacion = 1, 
                Titulo = "¡Email verificado!",
                Mensaje = $"Hola {usuario.nombres}, tu email ha sido verificado exitosamente. Ya puedes disfrutar de todas las funciones de TorneoPro.",
                Prioridad = "MEDIA"
            });
        }

        public async Task<UsuarioResponse> ObtenerUsuarioActualAsync(int usuarioId)
        {
            var usuario = await _contexto.usuarios
                .Include(u => u.id_tipo_usuarioNavigation)
                .Include(u => u.id_tipo_documentoNavigation)
                .FirstOrDefaultAsync(u => u.id == usuarioId && u.activo == true);

            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            var roles = await ObtenerRolesUsuario(usuario.id);
            return MapearUsuarioResponse(usuario, roles);
        }

        private async Task<List<string>> ObtenerRolesUsuario(int usuarioId)
        {
            return await _contexto.usuarios_roles
                .Where(ur => ur.id_usuario == usuarioId && ur.estado == "ACTIVO" && ur.activo == true)
                .Join(_contexto.tipos_rols,
                    ur => ur.id_rol,
                    r => r.id,
                    (ur, r) => r.codigo)
                .ToListAsync();
        }

        private async Task GuardarRefreshToken(int usuarioId, string refreshToken)
        {
            var token = new tokens_recuperacion
            {
                id_usuario = usuarioId,
                token = refreshToken,
                fecha_expiracion = DateTime.UtcNow.AddDays(7),
                activo = true
            };

            _contexto.tokens_recuperacions.Add(token);
            await _contexto.SaveChangesAsync();
        }

        
        private UsuarioResponse MapearUsuarioResponse(usuario usuario, List<string> roles)
        {
            return new UsuarioResponse
            {
                Id = usuario.id,
                Codigo = usuario.codigo,
                NombreCompleto = $"{usuario.nombres} {usuario.apellidos}",
                Email = usuario.email,
                FotoPerfil = usuario.foto_perfil_url,
                TipoUsuario = usuario.id_tipo_usuarioNavigation?.nombre ?? "",
                Roles = roles,
                Activo = usuario.activo ?? false,
                FechaRegistro = usuario.fecha_registro ?? DateTime.UtcNow,
                 // NUEVOS CAMPOS DE DOCUMENTO
                IdTipoDocumento = usuario.id_tipo_documento,
                TipoDocumentoNombre = usuario.id_tipo_documentoNavigation?.nombre ?? "",
                NumeroDocumento = usuario.numero_documento,
            };
        }

 
        private UsuarioAuthResponse MapearUsuarioAuthResponse(usuario usuario, List<string> roles)
        {
            return new UsuarioAuthResponse
            {
                Id = usuario.id,
                Codigo = usuario.codigo,
                NombreCompleto = $"{usuario.nombres} {usuario.apellidos}",
                Email = usuario.email,
                FotoPerfil = usuario.foto_perfil_url,
                Roles = roles,
                // Si también necesitas documentos en la respuesta de autenticación
                IdTipoDocumento = usuario.id_tipo_documento,
                TipoDocumentoNombre = usuario.id_tipo_documentoNavigation?.nombre ?? "",
                NumeroDocumento = usuario.numero_documento,
            };
        }

        /// <summary>
        /// Valida el formato del documento según su tipo
        /// </summary>
        private bool ValidarFormatoDocumento(int idTipoDocumento, string numeroDocumento)
        {
            if (string.IsNullOrWhiteSpace(numeroDocumento))
                return false;

            numeroDocumento = numeroDocumento.Trim();
            var formatoValido = false;

            switch (idTipoDocumento)
            {
                case 1: // CC - Cédula de Ciudadanía
                    formatoValido = System.Text.RegularExpressions.Regex.IsMatch(numeroDocumento, @"^\d{8,11}$");
                    break;
                case 2: // CE - Cédula de Extranjería
                    formatoValido = System.Text.RegularExpressions.Regex.IsMatch(numeroDocumento, @"^[A-Za-z]\d{8,10}$");
                    break;
                case 3: // Pasaporte
                    formatoValido = System.Text.RegularExpressions.Regex.IsMatch(numeroDocumento, @"^[A-Za-z0-9]{6,12}$");
                    break;
                case 4: // TI - Tarjeta de
                        // 
                    formatoValido = System.Text.RegularExpressions.Regex.IsMatch(numeroDocumento, @"^\d{8,12}$");
                    break;
                case 5: // Registro Civil
                    formatoValido = System.Text.RegularExpressions.Regex.IsMatch(numeroDocumento, @"^\d{10,12}$");
                    break;
                case 6: // NIT
                    formatoValido = System.Text.RegularExpressions.Regex.IsMatch(numeroDocumento, @"^\d{9,12}$");
                    break;
                default:
                    formatoValido = !string.IsNullOrWhiteSpace(numeroDocumento) && numeroDocumento.Length <= 20;
                    break;
            }

            return formatoValido;
        }
    }
}