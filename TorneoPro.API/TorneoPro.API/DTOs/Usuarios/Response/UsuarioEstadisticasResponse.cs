namespace TorneoPro.API.DTOs.Usuarios.Response
{
    public class UsuarioEstadisticasResponse
    {
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }
        public int UsuariosInactivos { get; set; }
        public int UsuariosVerificados { get; set; }
        public Dictionary<string, int> UsuariosPorTipo { get; set; } = new();
        public Dictionary<string, int> UsuariosPorRol { get; set; } = new();
        public Dictionary<string, int> UsuariosPorMes { get; set; } = new();
    }
}
