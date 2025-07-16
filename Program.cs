using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using Serilog;
using Serilog.Formatting.Compact;
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
      // Application Insights removed - using Datadog for APM (auto-injected)
      // Serilog handles all logging requirements
  })
  .ConfigureLogging((hostingContext, logging) =>
  {
      // Clear all default providers to prevent double logging
      logging.ClearProviders();
      
      // Configure Serilog as the primary logger with manual Datadog trace correlation
      // https://docs.datadoghq.com/tracing/other_telemetry/connect_logs_and_traces/dotnet/?tab=microsoftextensionslogging#manual-injection
      var serilogLogger = new LoggerConfiguration()
          .MinimumLevel.Information()
          .Enrich.FromLogContext()
          .WriteTo.Console()
          .WriteTo.File(
              new CompactJsonFormatter(), 
              "logs/serilog.log",
              rollingInterval: RollingInterval.Day,
              retainedFileCountLimit: 7,
              buffered: false) // Disable buffering for immediate writes
          .CreateLogger();
      
      logging.AddSerilog(serilogLogger);
      
      // Enable scopes - REQUIRED for Datadog correlation identifier injection
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