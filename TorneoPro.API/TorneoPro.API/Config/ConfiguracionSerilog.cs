using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace TorneoPro.API.Config
{
    public static class ConfiguracionSerilog
    {
        public static void ConfigurarLogging(WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            var environment = builder.Environment;

     
            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "TorneoPro.API")
                .Enrich.WithProperty("Environment", environment.EnvironmentName);

           
            if (environment.IsDevelopment())
            {
                loggerConfiguration.MinimumLevel.Debug();
                loggerConfiguration.MinimumLevel.Override("Microsoft", LogEventLevel.Information);
                loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
                loggerConfiguration.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);
            }
            else
            {
                loggerConfiguration.MinimumLevel.Information();
                loggerConfiguration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
                loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Error);
                loggerConfiguration.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error);
            }

          
            ConfigureSinks(loggerConfiguration, configuration, environment);

       
            Log.Logger = loggerConfiguration.CreateLogger();
            builder.Host.UseSerilog();
        }

        private static void ConfigureSinks(
            LoggerConfiguration loggerConfig,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            
            loggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information
            );

           
            var logPath = configuration["Serilog:WriteTo:File:Path"] ?? "logs/log-.txt";
            loggerConfig.WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information
            );

          
            var compactLogPath = configuration["Serilog:WriteTo:CompactFile:Path"] ?? "logs/log-compact-.clef";
            loggerConfig.WriteTo.File(
                new Serilog.Formatting.Compact.CompactJsonFormatter(),
                path: compactLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 50 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                restrictedToMinimumLevel: LogEventLevel.Information
            );
        }

        public static void CerrarYFlush()
        {
            Log.CloseAndFlush();
        }
    }
}