using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TorneoPro.API.Config;

/// <summary>
/// Transformer que inyecta el esquema de seguridad Bearer JWT en el documento OpenAPI.
/// Requerido en .NET 10 porque OpenApiSecurityScheme.Reference fue eliminado.
/// Fuente: https://www.c-sharpcorner.com/article/fixing-openapi-transform-for-scalar-to-add-a-global-jwt-auth-header-in-net-10/
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider
) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            // 1. Definir el esquema Bearer
            var bearerScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingrese el token JWT. Ejemplo: eyJhbGci..."
            };

        
            document.Components ??= new OpenApiComponents();
            document.AddComponent("Bearer", bearerScheme);

          
            var securityRequirement = new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            };

     
            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security ??= new List<OpenApiSecurityRequirement>();
                operation.Value.Security.Add(securityRequirement);
            }
        }
    }
}