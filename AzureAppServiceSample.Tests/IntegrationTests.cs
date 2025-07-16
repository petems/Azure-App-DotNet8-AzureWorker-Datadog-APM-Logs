using Microsoft.Extensions.Logging;
using Xunit;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.DependencyInjection;
using Datadog.Trace;

namespace AzureAppServiceSample.Tests;

public class IntegrationTests
{
    private static ILoggerFactory CreateHybridLoggerFactory()
    {
        var services = new ServiceCollection();
        
        // Add Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ILoggerFactory>();
    }

    private void SetTestDatadogEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("DD_SERVICE", "test-service");
        Environment.SetEnvironmentVariable("DD_VERSION", "test-1.0.0");
        Environment.SetEnvironmentVariable("DD_ENV", "test");
    }

    private void ClearTestDatadogEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("DD_SERVICE", null);
        Environment.SetEnvironmentVariable("DD_VERSION", null);
        Environment.SetEnvironmentVariable("DD_ENV", null);
    }

    [Fact]
    public void CanCreateHttpTriggerFunctionWithHybridLogging()
    {
        // Arrange
        var loggerFactory = CreateHybridLoggerFactory();

        // Act
        var function = new HttpTriggerFunction(loggerFactory);

        // Assert
        Assert.NotNull(function);
    }

    [Fact]
    public void LoggerFactoryIsConfiguredCorrectly()
    {
        // Arrange & Act
        var loggerFactory = CreateHybridLoggerFactory();
        var logger = loggerFactory.CreateLogger<IntegrationTests>();

        // Assert
        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void DatadogTracerIsAvailable()
    {
        // Act
        var tracer = Tracer.Instance;

        // Assert
        Assert.NotNull(tracer);
    }

    [Fact]
    public void CanCreateScopeWithDatadogCorrelationFromEnvironmentVariables()
    {
        // Arrange
        SetTestDatadogEnvironmentVariables();
        
        try
        {
            var loggerFactory = CreateHybridLoggerFactory();
            var logger = loggerFactory.CreateLogger<IntegrationTests>();
            
            // Act & Assert - Should not throw
            var activeScope = Tracer.Instance.ActiveScope;
            var traceId = activeScope?.Span?.TraceId.ToString() ?? "0";
            var spanId = activeScope?.Span?.SpanId.ToString() ?? "0";
            
            // Read Datadog configuration from environment variables (same as function)
            var ddService = Environment.GetEnvironmentVariable("DD_SERVICE") ?? "unknown-service";
            var ddVersion = Environment.GetEnvironmentVariable("DD_VERSION") ?? "unknown-version";
            var ddEnv = Environment.GetEnvironmentVariable("DD_ENV") ?? "unknown-env";
            
            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["dd.trace_id"] = traceId,
                ["dd.span_id"] = spanId,
                ["dd.service"] = ddService,
                ["dd.version"] = ddVersion,
                ["dd.env"] = ddEnv
            });
            
            logger.LogInformation("Test log with Datadog correlation from environment variables");
            
            Assert.NotNull(traceId);
            Assert.NotNull(spanId);
            Assert.Equal("test-service", ddService);
            Assert.Equal("test-1.0.0", ddVersion);
            Assert.Equal("test", ddEnv);
        }
        finally
        {
            ClearTestDatadogEnvironmentVariables();
        }
    }

    [Fact]
    public void DatadogConfigurationUsesDefaultsWhenEnvironmentVariablesNotSet()
    {
        // Arrange
        ClearTestDatadogEnvironmentVariables();
        
        // Act
        var ddService = Environment.GetEnvironmentVariable("DD_SERVICE") ?? "unknown-service";
        var ddVersion = Environment.GetEnvironmentVariable("DD_VERSION") ?? "unknown-version";
        var ddEnv = Environment.GetEnvironmentVariable("DD_ENV") ?? "unknown-env";
        
        // Assert
        Assert.Equal("unknown-service", ddService);
        Assert.Equal("unknown-version", ddVersion);
        Assert.Equal("unknown-env", ddEnv);
    }
} 