namespace TorneoPro.API.DTOs.Auth.Response
{
    public class UsuarioAuthResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FotoPerfil { get; set; }
        public List<string> Roles { get; set; } = new();

        // NUEVOS CAMPOS DE DOCUMENTO (opcional, según necesidad)
        public int? IdTipoDocumento { get; set; }
        public string TipoDocumentoNombre { get; set; } = string.Empty;
        public string? NumeroDocumento { get; set; }
    }
}