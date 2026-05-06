using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Net.Http;

namespace TorneoPro.API.Config
{
    public class OpenApiFileUploadOperationFilter : IOpenApiDocumentTransformer
    {
        private static readonly string[] FileParameterNames = { "foto", "archivo", "file", "imagen", "image" };

        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            foreach (var path in document.Paths)
            {
                foreach (var operation in path.Value.Operations)
                {
                    // Verificar si es POST o PUT
                    if ((operation.Key == HttpMethod.Post || operation.Key == HttpMethod.Put) &&
                        TryGetFileParameterName(operation.Value, path.Key, out var fileParameterName))
                    {
                        operation.Value.RequestBody = CreateFileUploadRequestBody(fileParameterName);

                        // Limpiar los parámetros de archivo si existen
                        if (operation.Value.Parameters != null)
                        {
                            var parametersToRemove = operation.Value.Parameters
                                .Where(p => FileParameterNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                                .ToList();

                            foreach (var param in parametersToRemove)
                            {
                                operation.Value.Parameters.Remove(param);
                            }
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        private bool TryGetFileParameterName(
            OpenApiOperation operation,
            string pathKey,
            out string fileParameterName)
        {
            fileParameterName = null!;

            // Verificar rutas específicas de canchas
            if (pathKey.Contains("/canchas/", StringComparison.OrdinalIgnoreCase) &&
                pathKey.Contains("/foto", StringComparison.OrdinalIgnoreCase))
            {
                fileParameterName = "foto";
                return true;
            }

            // Verificar rutas específicas de usuarios
            if (pathKey.Contains("/usuarios/", StringComparison.OrdinalIgnoreCase) &&
                pathKey.Contains("/actualizar-foto", StringComparison.OrdinalIgnoreCase))
            {
                fileParameterName = "foto";
                return true;
            }

            // Verificar parámetros en la operación
            if (operation.Parameters != null)
            {
                foreach (var param in operation.Parameters)
                {
                    if (FileParameterNames.Contains(param.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        fileParameterName = param.Name.ToLowerInvariant();
                        return true;
                    }
                }
            }

            // Verificar por nombre en la ruta
            foreach (var paramName in FileParameterNames)
            {
                if (pathKey.Contains($"/{paramName}", StringComparison.OrdinalIgnoreCase))
                {
                    fileParameterName = paramName;
                    return true;
                }
            }

            return false;
        }

        private static OpenApiRequestBody CreateFileUploadRequestBody(string parameterName)
        {
            return new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>
                            {
                                [parameterName] = new OpenApiSchema
                                {
                                    Type = JsonSchemaType.String,
                                    Format = "binary",
                                    Description = "Archivo a subir"
                                }
                            },
                            Required = new HashSet<string> { parameterName }
                        }
                    }
                }
            };
        }
    }
}