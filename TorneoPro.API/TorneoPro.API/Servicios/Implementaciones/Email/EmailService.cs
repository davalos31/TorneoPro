using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using TorneoPro.API.Servicios.Interfaces;
using TorneoPro.API.Servicios.Interfaces.Email;

namespace TorneoPro.API.Servicios.Implementaciones.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuracion;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailService(IConfiguration configuracion, ILogger<EmailService> logger , IHttpContextAccessor httpContextAccessor)
        {
            _configuracion = configuracion;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }



        public async Task EnviarVerificacionEmailAsync(string email, string token, string nombre)
        {
            var asunto = "Verifica tu cuenta - TorneoPro";

            var baseUrl = ObtenerBaseUrl();

            var mensaje = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Verificar Email - TorneoPro</title>
</head>
<body style='margin:0; padding:0; font-family: Arial, sans-serif; background:#f4f6f8;'>

    <table width='100%' cellpadding='0' cellspacing='0' style='padding:20px;'>
        <tr>
            <td align='center'>

                <table width='500' cellpadding='0' cellspacing='0'
                       style='background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 10px rgba(0,0,0,0.1);'>

                    <!-- HEADER -->
                    <tr>
                        <td style='background:linear-gradient(135deg,#4CAF50,#2E7D32); padding:25px; text-align:center; color:white;'>
                            <h1 style='margin:0; font-size:22px;'>TorneoPro</h1>
                            <p style='margin:5px 0 0; font-size:14px;'>Verificación de cuenta</p>
                        </td>
                    </tr>

                    <!-- CONTENIDO -->
                    <tr>
                        <td style='padding:30px; color:#333;'>

                            <h2 style='margin-top:0;'>¡Bienvenido, {nombre}! 🎉</h2>

                            <p style='color:#555;'>
                                Gracias por registrarte en TorneoPro. Para completar tu registro,
                                por favor verifica tu correo electrónico.
                            </p>

                            <div style='text-align:center; margin:30px 0;'>
                                <a href='{baseUrl}/api/auth/verificar-email/{token}'
                                   style='background:#4CAF50; color:white; padding:14px 30px;
                                          text-decoration:none; border-radius:6px;
                                          font-size:16px; font-weight:bold; display:inline-block;'>
                                    Verificar Email
                                </a>
                            </div>

                            <p style='color:#999; font-size:13px;'>
                                Este enlace expirará en <strong>24 horas</strong>.
                            </p>

                            <p style='color:#999; font-size:13px;'>
                                Si el botón no funciona, copia y pega este enlace en tu navegador:
                            </p>

                            <p style='font-size:12px; color:#4CAF50; word-break:break-all;'>
                                {baseUrl}/api/auth/verificar-email-page/{token}
                            </p>

                        </td>
                    </tr>

                    <!-- FOOTER -->
                    <tr>
                        <td style='background:#f4f6f8; padding:15px; text-align:center; font-size:12px; color:#aaa;'>
                            © {DateTime.UtcNow.Year} TorneoPro
                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>
";

            await EnviarCorreoAsync(email, asunto, mensaje);
        }


        private string ObtenerBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            
                return $"{request.Scheme}://{request.Host}";
            
        }



        public async Task EnviarRecuperacionPasswordAsync(string email, string token, string nombre)
        {
            var asunto = "Recuperación de contraseña - TorneoPro";

            // Deep link (NO se toca) configurar maui
            var deepLink = $"torneopro://restablecer-contrasena?token={token}";

            var mensaje = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
</head>
<body style='margin:0; padding:0; font-family: Arial, sans-serif; background:#f4f6f8;'>

    <table width='100%' cellpadding='0' cellspacing='0' style='padding:20px;'>
        <tr>
            <td align='center'>

                <table width='500' cellpadding='0' cellspacing='0'
                       style='background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 10px rgba(0,0,0,0.1);'>

                    <!-- HEADER -->
                    <tr>
                        <td style='background:linear-gradient(135deg,#667eea,#764ba2); padding:25px; text-align:center; color:white;'>
                            <h1 style='margin:0; font-size:22px;'>TorneoPro</h1>
                            <p style='margin:5px 0 0; font-size:14px;'>Recuperación de contraseña</p>
                        </td>
                    </tr>

                    <!-- CONTENIDO -->
                    <tr>
                        <td style='padding:30px; color:#333;'>

                            <h2 style='margin-top:0;'>Hola {nombre} 👋</h2>

                            <p style='color:#555;'>
                                Recibimos una solicitud para restablecer tu contraseña.
                            </p>

                            <p style='color:#555;'>
                                Haz clic en el botón para crear una nueva contraseña:
                            </p>

                            <div style='text-align:center; margin:30px 0;'>
                                <a href='{deepLink}'
                                   style='background:#667eea; color:white; padding:14px 30px;
                                          text-decoration:none; border-radius:6px;
                                          font-size:16px; font-weight:bold; display:inline-block;'>
                                    Restablecer Contraseña
                                </a>
                            </div>

                            <p style='color:#999; font-size:13px;'>
                                Este enlace expirará en <strong>24 horas</strong>.
                            </p>

                            <p style='color:#999; font-size:13px;'>
                                Si no solicitaste este cambio, ignora este mensaje.
                            </p>

                        </td>
                    </tr>

                    <!-- FOOTER -->
                    <tr>
                        <td style='background:#f4f6f8; padding:15px; text-align:center; font-size:12px; color:#aaa;'>
                            © {DateTime.UtcNow.Year} TorneoPro
                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>";

            await EnviarCorreoAsync(email, asunto, mensaje);
        }



        public async Task EnviarNotificacionEmailAsync(string email, string asunto, string mensaje, string? nombre = null)
        {
            var cuerpo = string.IsNullOrEmpty(nombre)
                ? mensaje
                : $@"
                    <h1>Hola {nombre}</h1>
                    {mensaje}
                ";

            await EnviarCorreoAsync(email, asunto, cuerpo);
        }

        private async Task EnviarCorreoAsync(string destino, string asunto, string cuerpoHtml)
        {
            try
            {
                var smtpServer = _configuracion["Email:SmtpServer"];
                var smtpPort = int.Parse(_configuracion["Email:SmtpPort"] ?? "587");
                var senderEmail = _configuracion["Email:SenderEmail"];
                var senderName = _configuracion["Email:SenderName"];
                var enableSsl = bool.Parse(_configuracion["Email:EnableSsl"] ?? "true");

                using var cliente = new SmtpClient(smtpServer, smtpPort);
                cliente.EnableSsl = enableSsl;
                cliente.UseDefaultCredentials = false;
                cliente.Credentials = new NetworkCredential(senderEmail, _configuracion["Email:Password"]);

                using var mensaje = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = asunto,
                    Body = cuerpoHtml,
                    IsBodyHtml = true
                };
                mensaje.To.Add(destino);

                await cliente.SendMailAsync(mensaje);
                _logger.LogInformation("Email enviado a {Destino}", destino);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email a {Destino}", destino);
                throw;
            }
        }
    }
}