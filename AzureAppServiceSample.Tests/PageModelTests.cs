using Microsoft.Extensions.Logging;
using Xunit;

namespace AzureAppServiceSample.Tests;

public class HttpTriggerFunctionTests
{
    [Fact]
    public void HttpTriggerFunction_Constructor_ShouldNotThrow()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        // Act & Assert
        var function = new HttpTriggerFunction(loggerFactory);
        Assert.NotNull(function);
        
        // Cleanup
        loggerFactory.Dispose();
    }

    [Fact]
    public void HttpTriggerFunction_Constructor_WithNullLoggerFactory_ShouldThrow()
    {
        // Arrange
        ILoggerFactory? nullLoggerFactory = null;
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HttpTriggerFunction(nullLoggerFactory!));
    }

    [Fact]
    public void HttpTriggerFunction_CanBeInstantiatedMultipleTimes()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        // Act
        var function1 = new HttpTriggerFunction(loggerFactory);
        var function2 = new HttpTriggerFunction(loggerFactory);
        
        // Assert
        Assert.NotNull(function1);
        Assert.NotNull(function2);
        Assert.NotSame(function1, function2);
        
        // Cleanup
        loggerFactory.Dispose();
    }
} 