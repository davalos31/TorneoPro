namespace TorneoPro.API.DTOs.Auth.Response
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UsuarioAuthResponse Usuario { get; set; } = new();
    }
}