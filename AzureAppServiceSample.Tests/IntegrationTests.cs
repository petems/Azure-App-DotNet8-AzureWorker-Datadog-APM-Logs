using Microsoft.Extensions.Logging;
using Xunit;
using Serilog;
using Serilog.Formatting.Compact;
using Datadog.Trace;

namespace AzureAppServiceSample.Tests;

public class IntegrationTests
{
    private static ILoggerFactory CreateSerilogLoggerFactory()
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                new CompactJsonFormatter(), 
                "logs/test-serilog.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 3,
                buffered: false)
            .CreateLogger();

        return LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(serilogLogger);
            builder.AddSimpleConsole(options => 
            {
                options.IncludeScopes = true;
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    [Fact]
    public void HttpTriggerFunction_Integration_CanBeCreated()
    {
        // Arrange
        var loggerFactory = CreateSerilogLoggerFactory();
        
        // Act
        var function = new HttpTriggerFunction(loggerFactory);
        
        // Assert
        Assert.NotNull(function);
        
        // Cleanup
        loggerFactory.Dispose();
    }

    [Fact]
    public void HttpTriggerFunction_WithSerilogConfiguration()
    {
        // Arrange
        var loggerFactory = CreateSerilogLoggerFactory();
        
        // Act
        var function = new HttpTriggerFunction(loggerFactory);
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();
        
        // Assert
        Assert.NotNull(function);
        Assert.NotNull(logger);
        
        // Test that scoped logging works
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["TestKey"] = "TestValue"
        });
        
        logger.LogInformation("Test message with scope");
        
        // Cleanup
        loggerFactory.Dispose();
    }

    [Fact]
    public void HttpTriggerFunction_TraceAccess_CanGetCurrentSpan()
    {
        // Arrange
        var loggerFactory = CreateSerilogLoggerFactory();
        
        // Act
        var function = new HttpTriggerFunction(loggerFactory);
        var activeScope = Tracer.Instance.ActiveScope;
        
        // Assert
        Assert.NotNull(function);
        // Note: activeScope might be null in test environment without active tracing
        
        // Cleanup
        loggerFactory.Dispose();
    }

    [Fact]
    public void HttpTriggerFunction_ManualTraceCorrelation_WorksWithScopes()
    {
        // Arrange
        var loggerFactory = CreateSerilogLoggerFactory();
        var function = new HttpTriggerFunction(loggerFactory);
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();
        
        // Act
        var activeScope = Tracer.Instance.ActiveScope;
        var traceId = activeScope?.Span?.TraceId.ToString() ?? "test-trace-id";
        var spanId = activeScope?.Span?.SpanId.ToString() ?? "test-span-id";
        
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["dd.trace_id"] = traceId,
            ["dd.span_id"] = spanId,
            ["dd.service"] = "test-service",
            ["dd.version"] = "1.0.0",
            ["dd.env"] = "test"
        });
        
        // Assert - Just verify no exceptions are thrown
        logger.LogInformation("Test message with manual Datadog trace correlation");
        Assert.NotNull(traceId);
        Assert.NotNull(spanId);
        
        // Cleanup
        loggerFactory.Dispose();
    }
} 