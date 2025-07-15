using Microsoft.Extensions.Logging;
using Xunit;

namespace AzureAppServiceSample.Tests;

public class ProductionIntegrationTests
{
    [Fact]
    public void Production_HttpTriggerFunction_CanBeCreated()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
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
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Production-like setting
        });
        
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
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        
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
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
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
} 