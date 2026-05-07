using TorneoPro.API.DTOs.Shared;
using TorneoPro.API.DTOs.Usuarios.Request;
using TorneoPro.API.DTOs.Usuarios.Response;

namespace TorneoPro.API.Servicios.Interfaces.Usuarios
{
    public interface IUsuarioService
    {
        /// <summary>
        /// Listar usuarios paginado con filtros
        /// </summary>
        Task<ResultadoPaginado<UsuarioResponse>> ObtenerTodosAsync(PaginacionRequest solicitud);

        /// <summary>
        /// Obtener usuario por ID con detalles
        /// </summary>
        Task<UsuarioDetalleResponse?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Actualizar datos personales del usuario
        /// </summary>
        Task<UsuarioResponse> ActualizarAsync(int id, ActualizarUsuarioRequest solicitud);

        /// <summary>
        /// Actualizar foto de perfil
        /// </summary>
        Task<string> ActualizarFotoAsync(int id, IFormFile foto);

        /// <summary>
        /// Cambiar contraseña - Verifica actual, valida nueva y actualiza hash
        /// </summary>
        Task CambiarContrasenaAsync(int id, CambiarPasswordRequest solicitud);

        /// <summary>
        /// Desactivar usuario (eliminación lógica)
        /// </summary>
        Task DesactivarAsync(int id);

        /// <summary>
        /// Obtener todos los roles activos del usuario
        /// </summary>
        Task<List<RolResponse>> ObtenerRolesAsync(int usuarioId);

        /// <summary>
        /// Asignar rol a usuario - Valida jerarquía y asigna rol con contexto torneo/equipo
        /// </summary>
        Task<RolResponse> AsignarRolAsync(int usuarioId, AsignarRolRequest solicitud);

        /// <summary>
        /// Revocar rol activo del usuario
        /// </summary>
        Task RevocarRolAsync(int usuarioId, int rolId);

        /// <summary>
        /// Reactivar usuario desactivado
        /// </summary>
        Task ReactivarAsync(int id);

        /// <summary>
        /// Obtener estadísticas de usuarios
        /// </summary>
        Task<UsuarioEstadisticasResponse> ObtenerEstadisticasAsync();

        /// <summary>
        /// Listar usuarios con filtros avanzados
        /// </summary>
        Task<ResultadoPaginado<UsuarioResponse>> ObtenerTodosAvanzadoAsync(FiltrarUsuarioRequest solicitud);
    }
}