using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TorneoPro.API.DTOs.Auth.Request;
using TorneoPro.API.DTOs.Auth.Response;
using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.DTOs.Usuarios.Response;
using TorneoPro.API.Servicios.Implementaciones.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Auditoria;
using TorneoPro.API.Servicios.Interfaces.Auth;

namespace TorneoPro.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<AuthController> _logger;


        public AuthController(
            IAuthService authService,
            IAuditoriaService auditoriaService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _auditoriaService = auditoriaService;
            _logger = logger;
        }

        /// <summary>
        /// Iniciar sesión en el sistema
        /// </summary>
        [HttpPost("iniciar-sesion")]
        [AllowAnonymous]
        public async Task<IActionResult> IniciarSesion([FromBody] LoginRequest solicitud)
        {
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Intento de inicio de sesión - Email: {Email}, IP: {Ip}, UserAgent: {UserAgent}",
                solicitud.Email, ipCliente, userAgent);

            try
            {
                var respuesta = await _authService.IniciarSesionAsync(solicitud);

                await _auditoriaService.RegistrarInicioSesionAsync(
                    solicitud.Email,
                    ipCliente,
                    userAgent,
                    true);

                _logger.LogInformation("Inicio de sesión exitoso - UsuarioId: {UsuarioId}, Email: {Email}, IP: {Ip}",
                    respuesta.Usuario.Id, solicitud.Email, ipCliente);

                return Ok(ApiRespuesta<AuthResponse>.Success(respuesta, "Inicio de sesión exitoso"));
            }
            catch (UnauthorizedAccessException ex)
            {
                await _auditoriaService.RegistrarInicioSesionAsync(
                    solicitud.Email,
                    ipCliente,
                    userAgent,
                    false,
                    ex.Message);

                _logger.LogWarning("Inicio de sesión fallido - Email: {Email}, IP: {Ip}, Motivo: {Motivo}",
                    solicitud.Email, ipCliente, ex.Message);
                return Unauthorized(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarInicioSesionAsync(
                    solicitud.Email,
                    ipCliente,
                    userAgent,
                    false,
                    "Error interno del servidor");

                _logger.LogError(ex, "Error en inicio de sesión - Email: {Email}, IP: {Ip}", solicitud.Email, ipCliente);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al iniciar sesión"));
            }
        }

        /// <summary>
        /// Registrar nuevo usuario (solo administradores)
        /// </summary>
        [HttpPost("registrar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioRequest solicitud)
        {
            var usuarioActualId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("Intento de registro de usuario - Administrador: {AdminId}, EmailNuevo: {Email}, IP: {Ip}",
                usuarioActualId, solicitud.Email, ipCliente);

            try
            {
                var respuesta = await _authService.RegistrarAsync(solicitud);

                await _auditoriaService.RegistrarExitoAsync(
                    usuarioActualId,
                    "CREAR_USUARIO",
                    "usuarios",
                    respuesta.Id,
                    AuditoriaService.SerializarDatos(new
                    {
                        solicitud.Nombres,
                        solicitud.Apellidos,
                        solicitud.Email,
                        solicitud.IdTipoUsuario,
                        solicitud.Telefono
                    }));

                _logger.LogInformation("Usuario registrado exitosamente - NuevoUsuarioId: {NuevoId}, Email: {Email}, RegistradoPor: {AdminId}",
                    respuesta.Id, solicitud.Email, usuarioActualId);

                return Ok(ApiRespuesta<UsuarioResponse>.Success(respuesta, "Usuario registrado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "CREAR_USUARIO",
                    ex.Message,
                    "usuarios",
                    null,
                    AuditoriaService.SerializarDatos(solicitud));

                _logger.LogWarning("Registro de usuario fallido - Email: {Email}, Motivo: {Motivo}", solicitud.Email, ex.Message);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                await _auditoriaService.RegistrarErrorAsync(
                    usuarioActualId,
                    "CREAR_USUARIO",
                    "Error interno del servidor",
                    "usuarios",
                    null,
                    AuditoriaService.SerializarDatos(solicitud));

                _logger.LogError(ex, "Error en registro de usuario - Email: {Email}", solicitud.Email);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al registrar usuario"));
            }
        }

        /// <summary>
        /// Renovar token JWT usando token de actualización
        /// </summary>
        [HttpPost("renovar-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RenovarToken([FromBody] RefrescarTokenRequest solicitud)
        {
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Intento de renovación de token - IP: {Ip}", ipCliente);

            try
            {
                var respuesta = await _authService.RenovarTokenAsync(solicitud.TokenActualizacion);

                await _auditoriaService.RegistrarExitoAsync(
                    respuesta.Usuario.Id,
                    "RENOVAR_TOKEN",
                    "tokens_recuperacion",
                    null);

                _logger.LogInformation("Token renovado exitosamente - UsuarioId: {UsuarioId}, IP: {Ip}",
                    respuesta.Usuario.Id, ipCliente);

                return Ok(ApiRespuesta<AuthResponse>.Success(respuesta, "Token renovado exitosamente"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Renovación de token fallida - IP: {Ip}, Motivo: {Motivo}", ipCliente, ex.Message);
                return Unauthorized(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al renovar token - IP: {Ip}", ipCliente);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al renovar token"));
            }
        }

        /// <summary>
        /// Cerrar sesión
        /// </summary>
        [HttpPost("cerrar-sesion")]
        [Authorize]
        public async Task<IActionResult> CerrarSesion()
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Cierre de sesión - UsuarioId: {UsuarioId}, IP: {Ip}", usuarioId, ipCliente);

            try
            {
                await _authService.CerrarSesionAsync(usuarioId);

                await _auditoriaService.RegistrarCierreSesionAsync(usuarioId, ipCliente);

                _logger.LogInformation("Sesión cerrada exitosamente - UsuarioId: {UsuarioId}", usuarioId);

                return Ok(ApiRespuesta<object>.Success(null, "Sesión cerrada exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en cierre de sesión - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al cerrar sesión"));
            }
        }

        /// <summary>
        /// Solicitar recuperación de contraseña
        /// </summary>
        [HttpPost("olvide-contrasena")]
        [AllowAnonymous]
        public async Task<IActionResult> OlvideContrasena([FromBody] OlvidePasswordRequest solicitud)
        {
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Solicitud de recuperación de contraseña - Email: {Email}, IP: {Ip}",
                solicitud.Email, ipCliente);

            try
            {
                await _authService.OlvideContrasenaAsync(solicitud.Email);

                _logger.LogInformation("Solicitud de recuperación procesada - Email: {Email}", solicitud.Email);

                return Ok(ApiRespuesta<object>.Success(null, "Si el email existe, recibirás instrucciones para recuperar tu contraseña"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en solicitud de recuperación - Email: {Email}", solicitud.Email);
                return Ok(ApiRespuesta<object>.Success(null, "Si el email existe, recibirás instrucciones para recuperar tu contraseña"));
            }
        }

        /// <summary>
        /// Restablecer contraseña con token
        /// </summary>
        [HttpPost("restablecer-contrasena")]
        [AllowAnonymous]
        public async Task<IActionResult> RestablecerContrasena([FromBody] RestablecerPasswordRequest solicitud)
        {
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Intento de restablecimiento de contraseña - Email: {Email}, IP: {Ip}",
                solicitud.Email, ipCliente);

            try
            {
                await _authService.RestablecerContrasenaAsync(solicitud);

                _logger.LogInformation("Contraseña restablecida exitosamente - Email: {Email}", solicitud.Email);

                return Ok(ApiRespuesta<object>.Success(null, "Contraseña restablecida exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Restablecimiento de contraseña fallido - Email: {Email}, Motivo: {Motivo}",
                    solicitud.Email, ex.Message);
                return BadRequest(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer contraseña - Email: {Email}", solicitud.Email);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al restablecer contraseña"));
            }
        }

        

        [HttpGet("verificar-email/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> VerificarEmailPage(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Error - TorneoPro</title>
    <meta charset='utf-8'>
    <style>
        body {
            font-family: Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }
        .container {
            text-align: center;
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .error-icon { color: #f44336; font-size: 80px; margin-bottom: 20px; }
        h1 { color: #333; }
        p { color: #666; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='error-icon'>✗</div>
        <h1>Token no válido</h1>
        <p>El token de verificación no fue proporcionado.</p>
    </div>
</body>
</html>
", "text/html");
            }

            try
            {
                await _authService.VerificarEmailAsync(token);

                return Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Email Verificado - TorneoPro</title>
    <meta charset='utf-8'>
    <style>
        body {
            font-family: Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }
        .container {
            text-align: center;
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            animation: fadeIn 0.5s ease-in;
        }
        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(-20px); }
            to { opacity: 1; transform: translateY(0); }
        }
        .success-icon {
            color: #4CAF50;
            font-size: 80px;
            margin-bottom: 20px;
        }
        h1 { color: #333; margin-bottom: 20px; }
        p { color: #666; margin-bottom: 10px; line-height: 1.6; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='success-icon'>✓</div>
        <h1>¡Email Verificado!</h1>
        <p>Tu correo electrónico ha sido verificado exitosamente.</p>
        <p>Ya puedes cerrar esta ventana e iniciar sesión.</p>
    </div>
</body>
</html>
", "text/html");
            }
            catch (InvalidOperationException ex)
            {
                return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>Error - TorneoPro</title>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }}
        .container {{
            text-align: center;
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            max-width: 500px;
        }}
        .error-icon {{
            color: #f44336;
            font-size: 80px;
            margin-bottom: 20px;
        }}
        h1 {{ color: #333; margin-bottom: 20px; }}
        p {{ color: #666; line-height: 1.6; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='error-icon'>✗</div>
        <h1>Error de Verificación</h1>
        <p>{ex.Message}</p>
    </div>
</body>
</html>
", "text/html");
            }
            catch
            {
                return Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Error - TorneoPro</title>
    <meta charset='utf-8'>
    <style>
        body {
            font-family: Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }
        .container {
            text-align: center;
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .error-icon {
            color: #f44336;
            font-size: 80px;
            margin-bottom: 20px;
        }
        h1 { color: #333; }
        p { color: #666; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='error-icon'>✗</div>
        <h1>Error del Sistema</h1>
        <p>Ocurrió un error al verificar el email. Por favor intenta nuevamente.</p>
    </div>
</body>
</html>
", "text/html");
            }
        }


        /// <summary>
        /// Obtener información del usuario autenticado
        /// </summary>
        [HttpGet("mi-perfil")]
        [Authorize]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            var usuarioId = ObtenerUsuarioActualId();
            var ipCliente = ObtenerIpCliente();

            _logger.LogInformation("Consulta de perfil propio - UsuarioId: {UsuarioId}, IP: {Ip}", usuarioId, ipCliente);

            try
            {
                var respuesta = await _authService.ObtenerUsuarioActualAsync(usuarioId);

                _logger.LogInformation("Perfil consultado exitosamente - UsuarioId: {UsuarioId}", usuarioId);

                return Ok(ApiRespuesta<UsuarioResponse>.Success(respuesta));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Usuario no encontrado - UsuarioId: {UsuarioId}", usuarioId);
                return NotFound(ApiRespuesta<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perfil - UsuarioId: {UsuarioId}", usuarioId);
                return StatusCode(500, ApiRespuesta<object>.Error("Error al obtener información del usuario"));
            }
        }

        private int ObtenerUsuarioActualId()
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out var usuarioId))
            {
                throw new UnauthorizedAccessException("Usuario no identificado");
            }

            return usuarioId;
        }

        private string ObtenerIpCliente()
        {
            var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip))
            {
                ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            return ip ?? "IP desconocida";
        }
    }
}