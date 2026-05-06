namespace TorneoPro.API.DTOs.AccesoTemporal.Response
{
    public class DeepLinkInfoResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string NombreEntidad { get; set; } = string.Empty;
        public DateTime FechaExpiracion { get; set; }
        public bool EsValido { get; set; }
        public string? ErrorMensaje { get; set; }
        public string DeepLink { get; set; } = string.Empty;
    }
}
