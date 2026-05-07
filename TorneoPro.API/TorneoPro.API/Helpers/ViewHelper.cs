using System.Text;
using System.Text.Encodings.Web;
using TorneoPro.API.DTOs.AccesoTemporal.Response;

namespace TorneoPro.API.Helpers
{
    /// <summary>
    /// Helper para generar contenido HTML reutilizable para invitaciones
    /// </summary>
    public static class ViewHelper
    {
        /// <summary>
        /// Genera la página HTML completa para una invitación
        /// </summary>
        /// <param name="info">Información de la invitación</param>
        /// <param name="baseUrl">URL base de la aplicación</param>
        /// <returns>HTML completo de la página de invitación</returns>
        public static string GenerarPaginaInvitacion(InviteInfoResponse info, string baseUrl)
        {
            var icono = GetIconoPorTipo(info.Tipo);
            var tituloEntidad = GetTituloEntidad(info.Tipo);
            var encodedTitulo = HtmlEncoder.Default.Encode(info.Titulo);
            var encodedMensaje = HtmlEncoder.Default.Encode(info.Mensaje);
            var encodedNombreEntidad = HtmlEncoder.Default.Encode(info.NombreEntidad);
            var encodedDeepLink = HtmlEncoder.Default.Encode(info.DeepLink);
            var encodedToken = HtmlEncoder.Default.Encode(info.Token);
            var fechaExpiracion = info.FechaExpiracion.ToString("dd/MM/yyyy HH:mm");

            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, viewport-fit=cover'>
    <title>{encodedTitulo} - TorneoPro</title>
    <link rel='stylesheet' href='{baseUrl}/css/invite.css'>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h1>⚽ TorneoPro</h1>
            <p>Plataforma de gestión deportiva</p>
        </div>
        <div class='content'>
            <div class='icon'>{icono}</div>
            <div class='title'>{encodedTitulo}</div>
            <div class='subtitle'>{encodedMensaje}</div>
            
            <div class='info-box'>
                <div class='info-label'>{tituloEntidad}</div>
                <div class='info-value'>{encodedNombreEntidad}</div>
                <div class='expiry'>⏰ Expira: {fechaExpiracion} hs</div>
            </div>

            <button class='btn btn-primary' id='openAppBtn'>
                <span id='btnText'>📱 Abrir en la aplicación</span>
                <span id='btnLoader' class='loader hidden'></span>
            </button>

            <a href='#' id='fallbackLink' class='btn btn-secondary' style='display:none'>
                🌐 Continuar en el navegador
            </a>
        </div>
        <div class='footer'>
            © {DateTime.UtcNow.Year} TorneoPro · Todos los derechos reservados
        </div>
    </div>

    <script>
        const deepLink = '{encodedDeepLink}';
        const token = '{encodedToken}';
        const fallbackUrl = '{baseUrl}/invite/fallback/{encodedToken}';
    </script>
    <script src='{baseUrl}/js/invite.js'></script>
</body>
</html>";
        }

        /// <summary>
        /// Genera la página de error HTML
        /// </summary>
        /// <param name="mensaje">Mensaje de error</param>
        /// <param name="baseUrl">URL base de la aplicación</param>
        /// <returns>HTML completo de la página de error</returns>
        public static string GenerarPaginaError(string mensaje, string baseUrl)
        {
            var encodedMensaje = HtmlEncoder.Default.Encode(mensaje);

            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Error - TorneoPro</title>
    <link rel='stylesheet' href='{baseUrl}/css/invite.css'>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h1>⚽ TorneoPro</h1>
            <p>Plataforma de gestión deportiva</p>
        </div>
        <div class='content'>
            <div class='icon'>🔗❌</div>
            <div class='title'>Enlace no válido</div>
            <div class='subtitle'>{encodedMensaje}</div>
            <a href='/' class='btn btn-primary'>Volver a TorneoPro</a>
        </div>
        <div class='footer'>
            © {DateTime.UtcNow.Year} TorneoPro · Todos los derechos reservados
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Genera la página de redirección al login
        /// </summary>
        /// <param name="token">Token de invitación</param>
        /// <param name="nombreEntidad">Nombre de la entidad (equipo, partido, etc.)</param>
        /// <param name="returnUrl">URL de retorno después del login</param>
        /// <param name="baseUrl">URL base de la aplicación</param>
        /// <returns>HTML completo de la página de login</returns>
        public static string GenerarPaginaLogin(string token, string nombreEntidad, string returnUrl, string baseUrl)
        {
            var encodedToken = HtmlEncoder.Default.Encode(token);
            var encodedNombreEntidad = HtmlEncoder.Default.Encode(nombreEntidad);
            var encodedReturnUrl = HtmlEncoder.Default.Encode(returnUrl);

            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Iniciar sesión - TorneoPro</title>
    <link rel='stylesheet' href='{baseUrl}/css/invite.css'>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h1>⚽ TorneoPro</h1>
            <p>Inicia sesión para continuar</p>
        </div>
        <div class='content'>
            <div class='invite-banner'>
                <span class='icon-small'>👥</span>
                <span>Inicia sesión para <strong>unirte a {encodedNombreEntidad}</strong></span>
            </div>

            <form method='post' action='/api/auth/login-page' id='loginForm'>
                <input type='hidden' name='returnUrl' value='{encodedReturnUrl}' />
                <input type='hidden' name='token' value='{encodedToken}' />
                
                <div class='field'>
                    <label for='email'>Correo electrónico</label>
                    <input type='email' id='email' name='email' placeholder='tu@email.com' required />
                </div>

                <div class='field'>
                    <label for='password'>Contraseña</label>
                    <div class='password-wrap'>
                        <input type='password' id='password' name='password' placeholder='••••••••' required />
                        <button type='button' class='toggle-pw' onclick='togglePassword()'>👁</button>
                    </div>
                </div>

                <button type='submit' class='btn btn-primary' id='submitBtn'>
                    <span id='btnText'>Iniciar sesión</span>
                    <span id='btnLoader' class='loader hidden'></span>
                </button>
            </form>

            <div class='register-link'>
                ¿No tienes cuenta? <a href='/registro?token={encodedToken}'>Regístrate aquí</a>
            </div>
        </div>
        <div class='footer'>
            © {DateTime.UtcNow.Year} TorneoPro · Todos los derechos reservados
        </div>
    </div>

    <script>
        function togglePassword() {{
            const pw = document.getElementById('password');
            pw.type = pw.type === 'password' ? 'text' : 'password';
        }}

        document.getElementById('loginForm')?.addEventListener('submit', () => {{
            const btn = document.getElementById('submitBtn');
            btn.disabled = true;
            document.getElementById('btnLoader')?.classList.remove('hidden');
            document.getElementById('btnText')?.style.setProperty('opacity', '0.6');
        }});
    </script>
</body>
</html>";
        }

        /// <summary>
        /// Genera la página de registro para nuevos usuarios
        /// </summary>
        /// <param name="token">Token de invitación</param>
        /// <param name="nombreEquipo">Nombre del equipo</param>
        /// <param name="baseUrl">URL base de la aplicación</param>
        /// <returns>HTML completo de la página de registro</returns>
        public static string GenerarPaginaRegistro(string token, string nombreEquipo, string baseUrl)
        {
            var encodedToken = HtmlEncoder.Default.Encode(token);
            var encodedNombreEquipo = HtmlEncoder.Default.Encode(nombreEquipo);

            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Registro - TorneoPro</title>
    <link rel='stylesheet' href='{baseUrl}/css/invite.css'>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h1>⚽ TorneoPro</h1>
            <p>Crea tu cuenta y únete al equipo</p>
        </div>
        <div class='content'>
            <div class='invite-banner'>
                <span class='icon-small'>👥</span>
                <span>Te han invitado a <strong>{encodedNombreEquipo}</strong></span>
            </div>

            <form method='post' action='/api/auth/register-page' id='registerForm'>
                <input type='hidden' name='token' value='{encodedToken}' />
                
                <div class='field'>
                    <label for='nombres'>Nombres</label>
                    <input type='text' id='nombres' name='nombres' required />
                </div>

                <div class='field'>
                    <label for='apellidos'>Apellidos</label>
                    <input type='text' id='apellidos' name='apellidos' required />
                </div>

                <div class='field'>
                    <label for='email'>Correo electrónico</label>
                    <input type='email' id='email' name='email' required />
                </div>

                <div class='field'>
                    <label for='password'>Contraseña</label>
                    <div class='password-wrap'>
                        <input type='password' id='password' name='password' required />
                        <button type='button' class='toggle-pw' onclick='togglePassword()'>👁</button>
                    </div>
                </div>

                <div class='field'>
                    <label for='confirmPassword'>Confirmar contraseña</label>
                    <input type='password' id='confirmPassword' name='confirmPassword' required />
                </div>

                <button type='submit' class='btn btn-primary' id='submitBtn'>
                    <span id='btnText'>Registrarse y unirse al equipo</span>
                    <span id='btnLoader' class='loader hidden'></span>
                </button>
            </form>

            <div class='login-link'>
                ¿Ya tienes cuenta? <a href='/login?token={encodedToken}'>Inicia sesión</a>
            </div>
        </div>
        <div class='footer'>
            © {DateTime.UtcNow.Year} TorneoPro · Todos los derechos reservados
        </div>
    </div>

    <script>
        function togglePassword() {{
            const pw = document.getElementById('password');
            pw.type = pw.type === 'password' ? 'text' : 'password';
        }}

        document.getElementById('registerForm')?.addEventListener('submit', (e) => {{
            const password = document.getElementById('password').value;
            const confirm = document.getElementById('confirmPassword').value;
            
            if (password !== confirm) {{
                e.preventDefault();
                alert('Las contraseñas no coinciden');
                return;
            }}
            
            const btn = document.getElementById('submitBtn');
            btn.disabled = true;
            document.getElementById('btnLoader')?.classList.remove('hidden');
            document.getElementById('btnText')?.style.setProperty('opacity', '0.6');
        }});
    </script>
</body>
</html>";
        }

        /// <summary>
        /// Genera la página HTML de éxito al unirse a un equipo
        /// </summary>
        public static string GenerarPaginaExitoEquipo(string nombreEquipo, string nombreJugador)
        {
            var encodedEquipo = HtmlEncoder.Default.Encode(nombreEquipo);
            var encodedJugador = HtmlEncoder.Default.Encode(nombreJugador);

            return $@"
    <!DOCTYPE html>
    <html lang='es'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>¡Bienvenido! - TorneoPro</title>
        <link rel='stylesheet' href='/css/invite.css'>
    </head>
    <body>
        <div class='card'>
            <div class='header'>
                <h1>⚽ TorneoPro</h1>
                <p>Plataforma de gestión deportiva</p>
            </div>
            <div class='body'>
                <div class='icon'>✅</div>
                <div class='title'>¡Te has unido exitosamente!</div>
                <div class='subtitle'>
                    Ya eres parte del equipo. Puedes iniciar sesión en la app para ver tu perfil y estadísticas.
                </div>
                <div class='info-box'>
                    <div class='info-label'>Equipo</div>
                    <div class='info-value'>🏆 {encodedEquipo}</div>
                    <div class='player-info'>👤 {encodedJugador}</div>
                </div>
            </div>
            <div class='footer'>
                © {DateTime.UtcNow.Year} TorneoPro · Todos los derechos reservados
            </div>
        </div>
    </body>
    </html>";
        }

        /// <summary>
        /// Genera el HTML para el email de notificación
        /// </summary>
        public static string GenerarHtmlEmailNotificacion(string titulo, string mensaje, string nombreUsuario, string? accionUrl = null, string? prioridad = "MEDIA")
        {
            var badgeClass = prioridad?.ToLowerInvariant() switch
            {
                "alta" => "badge-alta",
                "urgente" => "badge-alta",
                "media" => "badge-media",
                "baja" => "badge-baja",
                _ => "badge-media"
            };

            var badgeText = prioridad?.ToUpperInvariant() ?? "MEDIA";

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>{HtmlEncoder.Default.Encode(titulo)} - TorneoPro</title>
                <style>
                    body {{
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
                        background-color: #f4f6f8;
                        margin: 0;
                        padding: 20px;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 0 auto;
                        background: #ffffff;
                        border-radius: 16px;
                        overflow: hidden;
                        box-shadow: 0 4px 12px rgba(0,0,0,0.1);
                    }}
                    .header {{
                        background: linear-gradient(135deg, #1E3A8A 0%, #2563EB 100%);
                        padding: 24px;
                        text-align: center;
                        color: white;
                    }}
                    .header h1 {{
                        font-size: 24px;
                        margin: 0;
                        letter-spacing: -0.5px;
                    }}
                    .header p {{
                        font-size: 13px;
                        margin: 5px 0 0;
                        opacity: 0.8;
                    }}
                    .content {{
                        padding: 28px;
                    }}
                    .greeting {{
                        font-size: 18px;
                        font-weight: 600;
                        color: #1F2937;
                        margin-bottom: 16px;
                    }}
                    .message {{
                        color: #374151;
                        line-height: 1.6;
                        margin-bottom: 24px;
                    }}
                    .badge {{
                        display: inline-block;
                        padding: 4px 12px;
                        border-radius: 20px;
                        font-size: 11px;
                        font-weight: 600;
                        margin-bottom: 20px;
                    }}
                    .badge-alta {{ background: #FEE2E2; color: #DC2626; }}
                    .badge-media {{ background: #FEF3C7; color: #D97706; }}
                    .badge-baja {{ background: #D1FAE5; color: #059669; }}
                    .button {{
                        display: inline-block;
                        padding: 12px 28px;
                        background: #2563EB;
                        color: white;
                        text-decoration: none;
                        border-radius: 40px;
                        font-weight: 600;
                        font-size: 14px;
                        margin-top: 8px;
                        transition: background 0.2s;
                    }}
                    .button:hover {{
                        background: #1D4ED8;
                    }}
                    .divider {{
                        border-top: 1px solid #E5E7EB;
                        margin: 24px 0 16px;
                    }}
                    .footer {{
                        background: #F9FAFB;
                        padding: 16px 24px;
                        text-align: center;
                        font-size: 11px;
                        color: #9CA3AF;
                        border-top: 1px solid #E5E7EB;
                    }}
                    .footer a {{
                        color: #2563EB;
                        text-decoration: none;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>⚽ TorneoPro</h1>
                        <p>Plataforma de gestión deportiva</p>
                    </div>
                    <div class='content'>
                        <div class='greeting'>¡Hola {HtmlEncoder.Default.Encode(nombreUsuario)}!</div>
                        <div class='badge {badgeClass}'>Prioridad: {badgeText}</div>
                        <div class='message'>{HtmlEncoder.Default.Encode(mensaje)}</div>
                        {(string.IsNullOrEmpty(accionUrl) ? "" : $@"
                        <div style='text-align: center;'>
                            <a href='{accionUrl}' class='button'>Ver detalles</a>
                        </div>")}
                        <div class='divider'></div>
                        <p style='font-size: 12px; color: #6B7280; margin: 0;'>
                            Este es un mensaje automático de TorneoPro. Por favor no responda a este correo.
                        </p>
                    </div>
                    <div class='footer'>
                        <p>© {DateTime.UtcNow.Year} TorneoPro · Todos los derechos reservados</p>
                        <p><a href='{accionUrl ?? "#"}'>Configurar notificaciones</a> | <a href='#'>Centro de ayuda</a></p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private static string GetIconoPorTipo(string tipo) => tipo switch
        {
            "EQUIPO" => "👥",
            "PARTIDO" => "⚽",
            "TORNEO" => "🏆",
            _ => "📨"
        };

        private static string GetTituloEntidad(string tipo) => tipo switch
        {
            "EQUIPO" => "Equipo",
            "PARTIDO" => "Partido",
            "TORNEO" => "Torneo",
            _ => "Información"
        };
    }

   
}