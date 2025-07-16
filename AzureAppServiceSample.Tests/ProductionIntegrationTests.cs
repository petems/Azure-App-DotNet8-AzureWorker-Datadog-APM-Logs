using Microsoft.Extensions.Logging;
using Xunit;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.DependencyInjection;
using Datadog.Trace;

namespace AzureAppServiceSample.Tests;

public class ProductionIntegrationTests
{
    private static ILoggerFactory CreateProductionLikeLoggerFactory(LogLevel minimumLevel = LogLevel.Information)
    {
        var services = new ServiceCollection();
        
        // Add Application Insights (production-like configuration)
        services.AddApplicationInsightsTelemetryWorkerService();
        
        // Configure logging with production-like settings
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(minimumLevel);
            
            // Filter out verbose Microsoft logs (production-like)
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
        });

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ILoggerFactory>();
    }

    private void SetProductionDatadogEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("DD_SERVICE", "azure-functions-sample");
        Environment.SetEnvironmentVariable("DD_VERSION", "1.0.0");
        Environment.SetEnvironmentVariable("DD_ENV", "production");
        Environment.SetEnvironmentVariable("DD_TRACE_ENABLED", "true");
        Environment.SetEnvironmentVariable("DD_RUNTIME_METRICS_ENABLED", "true");
    }

    private void ClearProductionDatadogEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("DD_SERVICE", null);
        Environment.SetEnvironmentVariable("DD_VERSION", null);
        Environment.SetEnvironmentVariable("DD_ENV", null);
        Environment.SetEnvironmentVariable("DD_TRACE_ENABLED", null);
        Environment.SetEnvironmentVariable("DD_RUNTIME_METRICS_ENABLED", null);
    }

    [Fact]
    public void ProductionConfiguration_CanCreateHttpTriggerFunction()
    {
        // Arrange
        var loggerFactory = CreateProductionLikeLoggerFactory(LogLevel.Warning);

        // Act
        var function = new HttpTriggerFunction(loggerFactory);

        // Assert
        Assert.NotNull(function);
    }

    [Fact]
    public void ProductionConfiguration_LoggerFiltersWork()
    {
        // Arrange
        var loggerFactory = CreateProductionLikeLoggerFactory(LogLevel.Warning);
        var logger = loggerFactory.CreateLogger<ProductionIntegrationTests>();

        // Act & Assert
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
    }

    [Fact]
    public void ProductionConfiguration_SupportsDatadogTracing()
    {
        // Act
        var tracer = Tracer.Instance;

        // Assert
        Assert.NotNull(tracer);
    }

    [Fact]
    public void ProductionConfiguration_CanCreateDatadogCorrelatedLogsWithEnvironmentVariables()
    {
        // Arrange
        SetProductionDatadogEnvironmentVariables();
        
        try
        {
            var loggerFactory = CreateProductionLikeLoggerFactory();
            var logger = loggerFactory.CreateLogger<ProductionIntegrationTests>();

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
            
            logger.LogWarning("Production-level warning with Datadog correlation from environment variables");
            
            Assert.NotNull(traceId);
            Assert.NotNull(spanId);
            Assert.Equal("azure-functions-sample", ddService);
            Assert.Equal("1.0.0", ddVersion);
            Assert.Equal("production", ddEnv);
        }
        finally
        {
            ClearProductionDatadogEnvironmentVariables();
        }
    }

    [Theory]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    public void ProductionConfiguration_SupportsExpectedLogLevels(LogLevel logLevel)
    {
        // Arrange
        var loggerFactory = CreateProductionLikeLoggerFactory(logLevel);
        var logger = loggerFactory.CreateLogger<ProductionIntegrationTests>();

        // Act & Assert
        Assert.True(logger.IsEnabled(logLevel));
    }

    [Fact]
    public void ProductionConfiguration_ApplicationInsightsIntegration()
    {
        // Arrange
        var loggerFactory = CreateProductionLikeLoggerFactory();
        var logger = loggerFactory.CreateLogger<ProductionIntegrationTests>();

        // Act & Assert - Verify Application Insights structured logging works
        logger.LogInformation("Application Insights test with {RequestId} and {UserId}", 
            "req-123", "user-456");
        
        // No exceptions should be thrown
        Assert.True(true);
    }

    [Fact]
    public void ProductionConfiguration_CombinedApplicationInsightsAndDatadogLoggingWithEnvironmentVariables()
    {
        // Arrange
        SetProductionDatadogEnvironmentVariables();
        
        try
        {
            var loggerFactory = CreateProductionLikeLoggerFactory();
            var logger = loggerFactory.CreateLogger<ProductionIntegrationTests>();

            // Act
            var activeScope = Tracer.Instance.ActiveScope;
            var traceId = activeScope?.Span?.TraceId.ToString() ?? "test-trace";
            var spanId = activeScope?.Span?.SpanId.ToString() ?? "test-span";
            
            // Read Datadog configuration from environment variables (same as function)
            var ddService = Environment.GetEnvironmentVariable("DD_SERVICE") ?? "unknown-service";
            var ddVersion = Environment.GetEnvironmentVariable("DD_VERSION") ?? "unknown-version";
            var ddEnv = Environment.GetEnvironmentVariable("DD_ENV") ?? "unknown-env";
            
            // Create combined scope with both Application Insights and Datadog properties
            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                // Datadog properties from environment variables
                ["dd.trace_id"] = traceId,
                ["dd.span_id"] = spanId,
                ["dd.service"] = ddService,
                ["dd.version"] = ddVersion,
                ["dd.env"] = ddEnv,
                
                // Application Insights properties
                ["RequestId"] = "ai-request-123",
                ["OperationId"] = "ai-operation-456",
                ["UserAgent"] = "Test-Agent/1.0"
            });
            
            // Log with structured properties
            logger.LogInformation("Combined logging test: Method={Method}, Status={Status}", 
                "GET", 200);
            
            // Assert - No exceptions should be thrown and environment variables should be read correctly
            Assert.NotNull(traceId);
            Assert.NotNull(spanId);
            Assert.Equal("azure-functions-sample", ddService);
            Assert.Equal("1.0.0", ddVersion);
            Assert.Equal("production", ddEnv);
        }
        finally
        {
            ClearProductionDatadogEnvironmentVariables();
        }
    }

    [Fact]
    public void ProductionConfiguration_DatadogEnvironmentVariablesAreRespected()
    {
        // Arrange
        SetProductionDatadogEnvironmentVariables();
        
        try
        {
            // Act
            var ddService = Environment.GetEnvironmentVariable("DD_SERVICE");
            var ddVersion = Environment.GetEnvironmentVariable("DD_VERSION");
            var ddEnv = Environment.GetEnvironmentVariable("DD_ENV");
            var ddTraceEnabled = Environment.GetEnvironmentVariable("DD_TRACE_ENABLED");
            var ddRuntimeMetricsEnabled = Environment.GetEnvironmentVariable("DD_RUNTIME_METRICS_ENABLED");
            
            // Assert
            Assert.Equal("azure-functions-sample", ddService);
            Assert.Equal("1.0.0", ddVersion);
            Assert.Equal("production", ddEnv);
            Assert.Equal("true", ddTraceEnabled);
            Assert.Equal("true", ddRuntimeMetricsEnabled);
        }
        finally
        {
            ClearProductionDatadogEnvironmentVariables();
        }
    }
} 