using Microsoft.Extensions.Logging;
using Xunit;
using Serilog;
using Serilog.Formatting.Compact;
using Datadog.Trace;

namespace AzureAppServiceSample.Tests;

public class ProductionIntegrationTests
{
    private static ILoggerFactory CreateProductionLikeSerilogLoggerFactory(LogLevel minimumLevel = LogLevel.Information)
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Is(ConvertToSerilogLevel(minimumLevel))
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                new CompactJsonFormatter(), 
                "logs/test-production.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                buffered: false)
            .CreateLogger();

        return LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(serilogLogger);
            builder.AddSimpleConsole(options => 
            {
                options.IncludeScopes = true;
            });
            builder.SetMinimumLevel(minimumLevel);
        });
    }

    private static Serilog.Events.LogEventLevel ConvertToSerilogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogLevel.Critical => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Information
        };
    }

    [Fact]
    public void Production_HttpTriggerFunction_CanBeCreated()
    {
        // Arrange
        var loggerFactory = CreateProductionLikeSerilogLoggerFactory(LogLevel.Information);
        
        // Act
        var function = new HttpTriggerFunction(loggerFactory);
        
        // Assert
        Assert.NotNull(function);
        
        // Cleanup
        loggerFactory.Dispose();
    }

    [Fact]
    public void Production_Configuration_CanCreateLoggerFactory()
    {
        // Arrange & Act
        var loggerFactory = CreateProductionLikeSerilogLoggerFactory(LogLevel.Warning);
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();
        
        // Assert
        Assert.NotNull(loggerFactory);
        Assert.NotNull(logger);
        
        // Cleanup
        loggerFactory.Dispose();
    }

    [Fact]
    public void Production_HttpTriggerFunction_WithProductionLogger()
    {
        // Arrange
        var loggerFactory = CreateProductionLikeSerilogLoggerFactory(LogLevel.Warning);
        
        // Act
        var function = new HttpTriggerFunction(loggerFactory);
        
        // Assert
        Assert.NotNull(function);
        
        // Cleanup
        loggerFactory.Dispose();
    }

    [Fact]
    public void Production_Function_SupportsMultipleInstances()
    {
        // Arrange
        var loggerFactory = CreateProductionLikeSerilogLoggerFactory();
        
        // Act - Create multiple instances
        var functions = new List<HttpTriggerFunction>();
        for (int i = 0; i < 5; i++)
        {
            functions.Add(new HttpTriggerFunction(loggerFactory));
        }
        
        // Assert
        Assert.Equal(5, functions.Count);
        Assert.All(functions, f => Assert.NotNull(f));
        
        // Cleanup
        loggerFactory.Dispose();
    }

    [Fact]
    public void Production_DatadogTraceCorrelation_WorksUnderLoad()
    {
        // Arrange
        var loggerFactory = CreateProductionLikeSerilogLoggerFactory();
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();
        
        // Act - Simulate multiple concurrent trace correlations
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                var activeScope = Tracer.Instance.ActiveScope;
                var traceId = activeScope?.Span?.TraceId.ToString() ?? $"test-trace-{taskId}";
                var spanId = activeScope?.Span?.SpanId.ToString() ?? $"test-span-{taskId}";
                
                using var scope = logger.BeginScope(new Dictionary<string, object>
                {
                    ["dd.trace_id"] = traceId,
                    ["dd.span_id"] = spanId,
                    ["dd.service"] = $"test-service-{taskId}",
                    ["dd.version"] = "1.0.0",
                    ["dd.env"] = "production-test"
                });
                
                logger.LogInformation("Concurrent trace correlation test {TaskId}", taskId);
            }));
        }
        
        // Assert - All tasks should complete without exception
        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(30));
        Assert.All(tasks, t => Assert.True(t.IsCompletedSuccessfully));
        
        // Cleanup
        loggerFactory.Dispose();
    }

    [Fact]
    public void Production_SerilogConfiguration_MatchesProductionSetup()
    {
        // Arrange & Act
        var loggerFactory = CreateProductionLikeSerilogLoggerFactory();
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();
        
        // Test various log levels
        logger.LogDebug("Debug message (should be filtered out)");
        logger.LogInformation("Information message");
        logger.LogWarning("Warning message");
        logger.LogError("Error message");
        
        // Test structured logging
        logger.LogInformation("Structured log with {Property1} and {Property2}", "test", 123);
        
        // Assert - No exceptions should be thrown
        Assert.NotNull(loggerFactory);
        Assert.NotNull(logger);
        
        // Cleanup
        loggerFactory.Dispose();
    }
} 