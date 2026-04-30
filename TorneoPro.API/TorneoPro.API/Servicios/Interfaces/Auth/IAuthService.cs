using TorneoPro.API.DTOs.Auth.Request;
using TorneoPro.API.DTOs.Auth.Response;
using TorneoPro.API.DTOs.Usuarios.Response;

namespace TorneoPro.API.Servicios.Interfaces.Auth
{
    public interface IAuthService
    {
        /// <summary>
        /// Iniciar sesión - Valida credenciales, genera JWT y token de actualización
        /// </summary>
        Task<AuthResponse> IniciarSesionAsync(LoginRequest solicitud);

        /// <summary>
        /// Registrar nuevo usuario - Crea usuario, hashea contraseña, envía email de verificación
        /// </summary>
        Task<UsuarioResponse> RegistrarAsync(RegistrarUsuarioRequest solicitud);

        /// <summary>
        /// Renovar JWT usando token de actualización
        /// </summary>
        Task<AuthResponse> RenovarTokenAsync(string tokenActualizacion);

        /// <summary>
        /// Cerrar sesión - Invalida tokens de actualización del usuario
        /// </summary>
        Task CerrarSesionAsync(int usuarioId);

        /// <summary>
        /// Solicitar recuperación de contraseña - Genera token y envía email
        /// </summary>
        Task OlvideContrasenaAsync(string email);

        /// <summary>
        /// Restablecer contraseña con token - Valida token (24h) y actualiza contraseña
        /// </summary>
        Task RestablecerContrasenaAsync(RestablecerPasswordRequest solicitud);

        /// <summary>
        /// Verificar email con token - Activa cuenta tras verificar token
        /// </summary>
        Task VerificarEmailAsync(string token);

        /// <summary>
        /// Obtener datos del usuario autenticado
        /// </summary>
        Task<UsuarioResponse> ObtenerUsuarioActualAsync(int usuarioId);
    }
}