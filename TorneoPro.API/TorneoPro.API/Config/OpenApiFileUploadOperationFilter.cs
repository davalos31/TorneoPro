using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Net.Http; 
using System; 

namespace TorneoPro.API.Config
{
    public class OpenApiFileUploadOperationFilter : IOpenApiDocumentTransformer
    {
        private static readonly string[] FileParameterNames = { "foto", "archivo", "file" };

        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            foreach (var path in document.Paths)
            {
                foreach (var operation in path.Value.Operations)
                {
                  
                    if ((operation.Key == HttpMethod.Post || operation.Key == HttpMethod.Put) &&
                        TryGetFileParameterName(operation.Value, path.Key, out var fileParameterName))
                    {
                        operation.Value.RequestBody = CreateFileUploadRequestBody(fileParameterName);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private bool TryGetFileParameterName(OpenApiOperation operation, string pathKey, out string fileParameterName)
        {
            fileParameterName = null;

            // Revisar parámetros explícitos en la operación
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

        private OpenApiRequestBody CreateFileUploadRequestBody(string parameterName)
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