using System;
using System.Security.Cryptography;
using System.Text;

namespace TorneoPro.API.Helpers
{
    public static class CodigoHelper
    {
        private static readonly Random _random = new Random();

        public static string GenerarCodigo(string prefijo, int longitud = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var stringBuilder = new StringBuilder();

            for (int i = 0; i < longitud; i++)
            {
                stringBuilder.Append(chars[_random.Next(chars.Length)]);
            }

            return $"{prefijo}{stringBuilder}";
        }

        public static string GenerarCodigoUnico(string prefijo)
        {
            return $"{prefijo}{DateTime.Now:yyyyMMddHHmmss}{_random.Next(1000, 9999)}";
        }

        public static string GenerarCodigoNumerico(int longitud = 6)
        {
            var stringBuilder = new StringBuilder();

            for (int i = 0; i < longitud; i++)
            {
                stringBuilder.Append(_random.Next(0, 10));
            }

            return stringBuilder.ToString();
        }

        public static string GenerarTokenVerificacion()
        {
            byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public static string GenerarCodigoEnlace()
        {
            byte[] tokenBytes = RandomNumberGenerator.GetBytes(24);
            return Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        public static string GenerarNumeroActa(int torneoId, int partidoId)
        {
            return $"ACTA-{torneoId:D4}-{partidoId:D6}-{DateTime.Now:yyyyMMdd}";
        }

        public static string GenerarNumeroCredencial(int jugadorId, int equipoId, int torneoId)
        {
            return $"CRED-{torneoId:D4}-{equipoId:D4}-{jugadorId:D6}";
        }

        public static string GenerarCodigoPartido(int torneoId, int jornada, int localId, int visitanteId)
        {
            return $"PART-{torneoId:D4}-J{jornada:D2}-L{localId:D4}-V{visitanteId:D4}";
        }

        public static string GenerarCodigoMulta(int torneoId, int tipoMultaId, int secuencia)
        {
            return $"MUL-{torneoId:D4}-{tipoMultaId:D2}-{secuencia:D6}";
        }

        public static string GenerarReferenciaPago(int multaId, int intento)
        {
            return $"PAGO-{multaId:D8}-{intento:D2}-{DateTime.Now:yyyyMMdd}";
        }

        public static string GenerarCodigoSuspension(int torneoId, int jugadorId, DateTime fecha)
        {
            return $"SUS-{torneoId:D4}-{jugadorId:D6}-{fecha:yyyyMMdd}";
        }

        public static string GenerarCodigoEquipo(string nombre)
        {
            // Limpiar el nombre: quitar tildes, espacios, caracteres especiales
            string limpio = nombre
                .ToUpperInvariant()
                .Normalize(NormalizationForm.FormD)
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "");

            // Quitar diacríticos
            var sb = new StringBuilder();
            foreach (char c in limpio)
            {
                if (char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            string baseCodigo = sb.ToString();

            // Tomar primeros 8 caracteres + timestamp
            if (baseCodigo.Length > 8)
            {
                baseCodigo = baseCodigo.Substring(0, 8);
            }

            return $"{baseCodigo}{DateTime.Now:yyMMddHHmm}";
        }
    }
}