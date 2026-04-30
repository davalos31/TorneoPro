
using QRCoder;
using System;
using System.IO;

namespace TorneoPro.API.Helpers
{
    public class QRHelper
    {
        public byte[] GenerarQRCode(string contenido, int pixelesPorModulo = 20)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrCodeData))
                {
                    using (var bitmap = qrCode.GetGraphic(pixelesPorModulo))
                    {
                        using (var stream = new MemoryStream())
                        {
                            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            return stream.ToArray();
                        }
                    }
                }
            }
        }

        public byte[] GenerarQRCodeConLogo(string contenido, byte[] logoBytes, int pixelesPorModulo = 20)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrCodeData))
                {
                    using (var qrBitmap = qrCode.GetGraphic(pixelesPorModulo))
                    {
                        // Cargar logo
                        using (var msLogo = new MemoryStream(logoBytes))
                        using (var logoBitmap = new System.Drawing.Bitmap(msLogo))
                        {
                            // Calcular tamaño del logo (20% del QR)
                            int logoSize = (int)(qrBitmap.Width * 0.2);
                            int x = (qrBitmap.Width - logoSize) / 2;
                            int y = (qrBitmap.Height - logoSize) / 2;

                            using (var graphics = System.Drawing.Graphics.FromImage(qrBitmap))
                            {
                                graphics.DrawImage(logoBitmap, new System.Drawing.Rectangle(x, y, logoSize, logoSize));
                            }

                            using (var stream = new MemoryStream())
                            {
                                qrBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                                return stream.ToArray();
                            }
                        }
                    }
                }
            }
        }

        public string GenerarQRCodeBase64(string contenido, int pixelesPorModulo = 20)
        {
            var qrBytes = GenerarQRCode(contenido, pixelesPorModulo);
            return Convert.ToBase64String(qrBytes);
        }

        public byte[] GenerarQRCodeParaCredencial(
            int idJugador,
            int idEquipo,
            int idTorneo,
            string baseUrl)
        {
            var contenido = $"{baseUrl}/api/credenciales/validar?jugador={idJugador}&equipo={idEquipo}&torneo={idTorneo}";
            return GenerarQRCode(contenido, 15);
        }

        public byte[] GenerarQRCodeParaActa(int idPartido, string baseUrl)
        {
            var contenido = $"{baseUrl}/api/partidos/{idPartido}/acta";
            return GenerarQRCode(contenido, 20);
        }

        public byte[] GenerarQRCodeParaEnlace(string codigoEnlace, string baseUrl)
        {
            var contenido = $"{baseUrl}/api/enlaces/{codigoEnlace}/usar";
            return GenerarQRCode(contenido, 20);
        }

        public string DecodificarQRCode(byte[] qrBytes)
        {
            // Para decodificar se necesitaría una librería como ZXing
            // Esta es una implementación básica usando QRCoder (solo codificación)
            throw new NotImplementedException("Decodificación no implementada en esta versión");
        }

        public byte[] GenerarQRCodeConFormatoPersonalizado(
            string contenido,
            string formato = "png",
            int tamaño = 300,
            string? colorFondo = null,
            string? colorQR = null)
        {
            // Esta es una implementación más avanzada que permitiría personalizar colores
            // Por simplicidad, retornamos el QR estándar
            return GenerarQRCode(contenido, tamaño / 20);
        }
    }
}