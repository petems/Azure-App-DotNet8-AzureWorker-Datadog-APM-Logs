using Microsoft.Extensions.Logging;
using Xunit;

namespace AzureAppServiceSample.Tests;

public class IntegrationTests
{
    [Fact]
    public void HttpTriggerFunction_Integration_CanBeCreated()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        // Act
        var function = new HttpTriggerFunction(loggerFactory);
        
        // Assert
        Assert.NotNull(function);
        
        // Cleanup
        loggerFactory.Dispose();
    }

    [Fact]
    public void HttpTriggerFunction_WithDifferentLogLevels()
    {
        // Arrange & Act
        var debugLoggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var infoLoggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        var debugFunction = new HttpTriggerFunction(debugLoggerFactory);
        var infoFunction = new HttpTriggerFunction(infoLoggerFactory);
        
        // Assert
        Assert.NotNull(debugFunction);
        Assert.NotNull(infoFunction);
        
        // Cleanup
        debugLoggerFactory.Dispose();
        infoLoggerFactory.Dispose();
    }

    [Fact]
    public void HttpTriggerFunction_LoggerFactory_CreatesLogger()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        // Act
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();
        
        // Assert
        Assert.NotNull(logger);
        
        // Cleanup
        loggerFactory.Dispose();
    }
} 