using Microsoft.Extensions.Logging;
using Xunit;
using Serilog;
using Serilog.Formatting.Compact;

namespace AzureAppServiceSample.Tests;

public class HttpTriggerFunctionTests
{
    private static ILoggerFactory CreateSerilogLoggerFactory()
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                new CompactJsonFormatter(), 
                "logs/test-pagemodel.log",
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
    public void HttpTriggerFunction_Constructor_ShouldNotThrow()
    {
        // Arrange
        var loggerFactory = CreateSerilogLoggerFactory();
        
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
        var loggerFactory = CreateSerilogLoggerFactory();
        
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

    [Fact]
    public void HttpTriggerFunction_Logger_SupportsScopes()
    {
        // Arrange
        var loggerFactory = CreateSerilogLoggerFactory();
        var function = new HttpTriggerFunction(loggerFactory);
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();
        
        // Act & Assert
        using var scope = logger.BeginScope("Test scope");
        logger.LogInformation("Test message within scope");
        
        // No exceptions should be thrown
        Assert.NotNull(function);
        
        // Cleanup
        loggerFactory.Dispose();
    }

    [Fact]
    public void HttpTriggerFunction_Logger_SupportsStructuredLogging()
    {
        // Arrange
        var loggerFactory = CreateSerilogLoggerFactory();
        var function = new HttpTriggerFunction(loggerFactory);
        var logger = loggerFactory.CreateLogger<HttpTriggerFunction>();
        
        // Act & Assert
        logger.LogInformation("Test message with {Property1} and {Property2}", "value1", 42);
        
        // No exceptions should be thrown
        Assert.NotNull(function);
        
        // Cleanup
        loggerFactory.Dispose();
    }
} 