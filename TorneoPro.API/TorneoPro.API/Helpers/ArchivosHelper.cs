using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TorneoPro.API.Helpers
{
    public class ArchivosHelper
    {
        private readonly string _basePath;

        public ArchivosHelper(string basePath = "uploads")
        {
            _basePath = basePath;
        }

        /// <summary>
        /// Guarda un archivo desde un array de bytes
        /// </summary>
        /// <param name="archivoBytes">Contenido del archivo en bytes</param>
        /// <param name="subCarpeta">Subcarpeta dentro de uploads (ej: "actas", "firmas")</param>
        /// <param name="nombreArchivo">Nombre del archivo (incluye extensión)</param>
        /// <returns>Ruta relativa del archivo guardado</returns>
        public async Task<string> GuardarArchivoBytesAsync(byte[] archivoBytes, string subCarpeta, string nombreArchivo)
        {
            if (archivoBytes == null || archivoBytes.Length == 0)
            {
                throw new ArgumentException("El archivo es requerido");
            }

            // Validar extensión - permitir PDF y documentos
            var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
            var extensionesPermitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".doc", ".docx", ".xls", ".xlsx" };

            if (!extensionesPermitidas.Contains(extension))
            {
                throw new ArgumentException($"Extensión no permitida. Permitidas: {string.Join(", ", extensionesPermitidas)}");
            }

            // Crear directorio
            var directorio = Path.Combine(_basePath, subCarpeta);
            if (!Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            var rutaCompleta = Path.Combine(directorio, nombreArchivo);

            // Guardar archivo
            await File.WriteAllBytesAsync(rutaCompleta, archivoBytes);

            // Retornar ruta relativa
            return Path.Combine(subCarpeta, nombreArchivo).Replace("\\", "/");
        }

        public async Task<string> GuardarImagenAsync(IFormFile imagen, string subCarpeta, int maxSizeMB = 5)
        {
            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            return await GuardarArchivoAsync(imagen, subCarpeta, null, extensionesPermitidas);
        }

        public async Task<string> GuardarArchivoAsync(
            IFormFile archivo,
            string subCarpeta,
            string? nombrePersonalizado = null,
            string[]? extensionesPermitidas = null)
        {
            if (archivo == null || archivo.Length == 0)
            {
                throw new ArgumentException("El archivo es requerido");
            }

            // Validar extensión
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (extensionesPermitidas != null && !extensionesPermitidas.Contains(extension))
            {
                throw new ArgumentException($"Extensión no permitida. Permitidas: {string.Join(", ", extensionesPermitidas)}");
            }

            // Validar tamaño (por defecto 10MB)
            var maxSize = 10 * 1024 * 1024;
            if (archivo.Length > maxSize)
            {
                throw new ArgumentException($"El archivo excede el tamaño máximo de {maxSize / (1024 * 1024)}MB");
            }

            // Crear directorio
            var directorio = Path.Combine(_basePath, subCarpeta);
            if (!Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            // Generar nombre único
            var nombreArchivo = string.IsNullOrEmpty(nombrePersonalizado)
                ? $"{Guid.NewGuid():N}{extension}"
                : $"{nombrePersonalizado}{extension}";

            var rutaCompleta = Path.Combine(directorio, nombreArchivo);

            // Guardar archivo
            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Retornar ruta relativa
            return Path.Combine(subCarpeta, nombreArchivo).Replace("\\", "/");
        }

        public async Task<byte[]> LeerArchivoAsync(string rutaRelativa)
        {
            var rutaCompleta = Path.Combine(_basePath, rutaRelativa);

            if (!File.Exists(rutaCompleta))
            {
                throw new FileNotFoundException("Archivo no encontrado");
            }

            return await File.ReadAllBytesAsync(rutaCompleta);
        }

        public bool EliminarArchivo(string rutaRelativa)
        {
            var rutaCompleta = Path.Combine(_basePath, rutaRelativa);

            if (!File.Exists(rutaCompleta))
            {
                return false;
            }

            File.Delete(rutaCompleta);
            return true;
        }

        public string ObtenerUrlArchivo(string rutaRelativa, string baseUrl)
        {
            if (string.IsNullOrEmpty(rutaRelativa))
            {
                return string.Empty;
            }

            return $"{baseUrl.TrimEnd('/')}/{rutaRelativa.Replace("\\", "/")}";
        }

        public long ObtenerTamanioArchivo(string rutaRelativa)
        {
            var rutaCompleta = Path.Combine(_basePath, rutaRelativa);

            if (!File.Exists(rutaCompleta))
            {
                return 0;
            }

            return new FileInfo(rutaCompleta).Length;
        }

        public string ObtenerExtension(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant();
        }

        public bool EsImagen(IFormFile archivo)
        {
            var extensionesImagen = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var extension = ObtenerExtension(archivo.FileName);
            return extensionesImagen.Contains(extension);
        }

        public bool EsPdf(IFormFile archivo)
        {
            return ObtenerExtension(archivo.FileName) == ".pdf";
        }

        public bool EsDocumentoWord(IFormFile archivo)
        {
            var extension = ObtenerExtension(archivo.FileName);
            return extension == ".doc" || extension == ".docx";
        }

        public async Task<string> GuardarImagenAsync(
            IFormFile imagen,
            string subCarpeta,
            int maxWidth = 1920,
            int maxHeight = 1080)
        {
            // Validar que sea imagen
            if (!EsImagen(imagen))
            {
                throw new ArgumentException("El archivo no es una imagen válida");
            }

            // Por ahora guardamos la imagen original
            // En una implementación real, aquí se redimensionaría la imagen
            return await GuardarArchivoAsync(
                imagen,
                subCarpeta,
                extensionesPermitidas: new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }
            );
        }

        public async Task<string> GuardarArchivoPdfAsync(
            IFormFile pdf,
            string subCarpeta,
            string? nombrePersonalizado = null)
        {
            if (!EsPdf(pdf))
            {
                throw new ArgumentException("El archivo debe ser un PDF");
            }

            return await GuardarArchivoAsync(
                pdf,
                subCarpeta,
                nombrePersonalizado,
                new[] { ".pdf" }
            );
        }
    }
}