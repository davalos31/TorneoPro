using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using TorneoPro.API.Config;
using TorneoPro.API.Data;
using TorneoPro.API.Helpers;
using TorneoPro.API.Hubs;
using TorneoPro.API.Interfaces.Estadistica_Vivo;
using TorneoPro.API.Middleware;
using TorneoPro.API.Seeder;

QuestPDF.Settings.License = LicenseType.Community;
ExcelPackage.License.SetNonCommercialPersonal("TorneoPro.Api");

// ============================================================
// 2. BUILDER Y SERVICIOS
// ============================================================
var builder = WebApplication.CreateBuilder(args);

ConfiguracionSerilog.ConfigurarLogging(builder);

// 2.1. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("Production", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                             ?? new[] {
                                 "http://localhost:5293",
                                 "https://localhost:7247",
                             };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 2.2. Controladores + Razor Pages
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// AGREGAR SOPORTE PARA RAZOR PAGES
builder.Services.AddRazorPages();



// 2.3. OpenAPI + Scalar
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddDocumentTransformer<OpenApiFileUploadOperationFilter>();

    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title = "TorneoPro API",
            Version = "v1",
            Description = "API para la gestión de torneos deportivos profesionales",
            Contact = new()
            {
                Name = "TorneoPro Support",
                Email = "support@torneopro.com",
                Url = new Uri("https://torneopro.com")
            }
        };
        return Task.CompletedTask;
    });
});

// 2.4. Base de Datos
builder.Services.AddDbContext<TorneoProContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("TorneoProContext");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(60);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// 2.5. JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurada");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

   
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();



builder.Services.AddMemoryCache();


builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<ArchivosHelper>();
builder.Services.AddScoped<QRHelper>();


builder.Services.AddSignalR();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TorneoProContext>();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                ?? httpContext.Request.Headers["X-Forwarded-For"].ToString()
                ?? httpContext.Connection.RemoteIpAddress?.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});


var app = builder.Build();


var estadisticaEnVivoService = app.Services.GetRequiredService<IEstadisticaEnVivoService>();
estadisticaEnVivoService.IniciarServicio();


using (var scope = app.Services.CreateScope())
{
    try
    {
        await SeederPrincipal.InitializeAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al ejecutar los seeders");
        if (app.Environment.IsProduction()) throw;
    }
}


app.UseMiddleware<MiddlewareExcepciones>();
app.UseMiddleware<MiddlewareLimiteTasa>();
app.UseMiddleware<MiddlewareJwt>();
app.UseMiddleware<MiddlewareAuditoria>();


if (app.Environment.IsDevelopment())
    app.UseCors("AllowAll");
else
    app.UseCors("Production");


if (app.Environment.IsProduction())
    app.UseHttpsRedirection();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("TorneoPro API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithPreferredScheme("Bearer");
    });
}


app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();


app.UseRateLimiter();


app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    AllowCachingResponses = false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});


app.MapRazorPages();
app.MapControllers();
app.MapHub<EstadisticasHub>("/hubs/estadisticas");



app.UseStaticFiles();

// 4.11. Redirección raíz → Scalar
app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();


app.Run();


public class ApiHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly ILogger<ApiHealthCheck> _logger;

    public ApiHealthCheck(ILogger<ApiHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API funcionando correctamente"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en health check");
            return Task.FromResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Error en API", ex));
        }
    }
}