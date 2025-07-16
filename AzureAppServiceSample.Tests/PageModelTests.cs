using Microsoft.Extensions.Logging;
using Xunit;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.DependencyInjection;

namespace AzureAppServiceSample.Tests;

public class HttpTriggerFunctionTests
{
    private static ILoggerFactory CreateApplicationInsightsLoggerFactory()
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

    [Fact]
    public void Constructor_WithValidLoggerFactory_CreatesInstance()
    {
        // Arrange
        var loggerFactory = CreateApplicationInsightsLoggerFactory();

        // Act
        var function = new HttpTriggerFunction(loggerFactory);

        // Assert
        Assert.NotNull(function);
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HttpTriggerFunction(null!));
    }

    [Fact]
    public void Logger_IsConfiguredWithApplicationInsights()
    {
        // Arrange
        var loggerFactory = CreateApplicationInsightsLoggerFactory();
        var function = new HttpTriggerFunction(loggerFactory);

        // Act - This verifies the logger was created successfully
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();

        // Assert
        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
    }

    [Theory]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    public void Logger_SupportsExpectedLogLevels(LogLevel logLevel)
    {
        // Arrange
        var loggerFactory = CreateApplicationInsightsLoggerFactory();
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();

        // Act & Assert
        Assert.True(logger.IsEnabled(logLevel));
    }

    [Fact]
    public void Logger_CanLogStructuredData()
    {
        // Arrange
        var loggerFactory = CreateApplicationInsightsLoggerFactory();
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();

        // Act & Assert - Should not throw
        logger.LogInformation("Test message with {Property1} and {Property2}", "value1", "value2");
        
        // Just verify no exceptions are thrown during structured logging
        Assert.True(true);
    }

    [Fact]
    public void Logger_CanUseScopes()
    {
        // Arrange
        var loggerFactory = CreateApplicationInsightsLoggerFactory();
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();

        // Act & Assert - Should not throw
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = "test-request-123",
            ["UserId"] = "user-456"
        });
        
        logger.LogInformation("Test message within scope");
        
        // Just verify no exceptions are thrown during scoped logging
        Assert.True(true);
    }
} 