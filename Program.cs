using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using Datadog.Trace; // Added for trace correlation

var host = new HostBuilder()
  .ConfigureFunctionsWorkerDefaults()
  .ConfigureAppConfiguration(appConfigBuilder =>
  {
      appConfigBuilder
          .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)
          .AddUserSecrets<Program>(optional: true, reloadOnChange: false);
  })
  .ConfigureServices((context, services) =>
  {
      // Application Insights for structured logging
      services.AddApplicationInsightsTelemetryWorkerService();
      services.ConfigureFunctionsApplicationInsights();
      services.AddApplicationConfiguration(context.Configuration);
      
      // Datadog tracing is auto-injected via the Datadog.Trace.Bundle package
      // and configured via environment variables
  })
  .ConfigureLogging((hostingContext, logging) =>
  {
      // Configure Application Insights logging
      logging.Services.Configure<LoggerFilterOptions>(options =>
      {
          var defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
              == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
          if (defaultRule is not null)
          {
              options.Rules.Remove(defaultRule);
          }
      });
      
      // Enable scopes for better trace correlation
      logging.AddSimpleConsole(options => 
      {
          options.IncludeScopes = true;
      });
      
      // Set minimum log level
      logging.SetMinimumLevel(LogLevel.Information);
  })
  .Build();

host.Run();

// Make the Program class public for integration tests
public partial class Program { }

// Extension method for Application Configuration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Add any additional configuration services here
        // For example, custom services, options patterns, etc.
        return services;
    }
} 