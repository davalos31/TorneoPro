using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.DTOs.Usuarios.Request
{
    public class FiltrarUsuarioRequest : PaginacionRequest
    {
        public string? Buscar { get; set; }
        public int? IdTipoUsuario { get; set; }
        public int? IdRol { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public string? Estado { get; set; }
        public bool? SoloActivos { get; set; }
        public bool? EmailVerificado { get; set; }
        public DateTime? FechaRegistroDesde { get; set; }
        public DateTime? FechaRegistroHasta { get; set; }
    }
}
