namespace TorneoPro.API.DTOs.Usuarios.Request
{
    public class RevocarRolRequest
    {
        public int IdUsuario { get; set; }
        public int IdRol { get; set; }
        public string? Motivo { get; set; }
    }
}