using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AzureAppServiceSample
{
    public class HttpTriggerFunction
    {
        private readonly ILogger _logger;

        public HttpTriggerFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpTriggerFunction>();
        }

        [Function("HttpTrigger")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            // This log message will demonstrate the double logging issue
            _logger.LogInformation("=== HTTP TRIGGER STARTED === Method: {Method}, URL: {URL}", req.Method, req.Url);
            _logger.LogWarning("This is a WARNING level message to test logging levels");
            _logger.LogError("This is an ERROR level message to test logging levels");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions Worker with Serilog logging!");

            _logger.LogInformation("=== HTTP TRIGGER COMPLETED === Response: 200 OK");

            return response;
        }
    }
} 