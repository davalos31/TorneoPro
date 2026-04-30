namespace TorneoPro.API.DTOs.Usuarios.Response
{
    public class UsuarioResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FotoPerfil { get; set; }
        public string TipoUsuario { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool Activo { get; set; }
        public DateTime FechaRegistro { get; set; }

        // NUEVOS CAMPOS DE DOCUMENTO
        public int? IdTipoDocumento { get; set; }
        public string TipoDocumentoNombre { get; set; } = string.Empty;
        public string? NumeroDocumento { get; set; }
    }
}