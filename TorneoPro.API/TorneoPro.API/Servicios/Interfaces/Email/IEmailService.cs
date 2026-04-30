namespace TorneoPro.API.Servicios.Interfaces.Email
{
    public interface IEmailService
    {
        Task EnviarVerificacionEmailAsync(string email, string token, string nombre);
        Task EnviarRecuperacionPasswordAsync(string email, string token, string nombre);
        Task EnviarNotificacionEmailAsync(string email, string asunto, string mensaje, string? nombre = null);
    }
}
