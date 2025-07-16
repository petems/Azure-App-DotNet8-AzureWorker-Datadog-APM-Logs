using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Datadog.Trace; // Added for trace correlation
using System.Collections.Generic; // Added for Dictionary

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
            // Get current trace and span IDs for manual injection
            var activeScope = Tracer.Instance.ActiveScope;
            var traceId = activeScope?.Span?.TraceId.ToString() ?? "0";
            var spanId = activeScope?.Span?.SpanId.ToString() ?? "0";
            
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["dd.trace_id"] = traceId,
                ["dd.span_id"] = spanId,
                ["dd.service"] = "azure-functions-sample",
                ["dd.version"] = "1.0.0",
                ["dd.env"] = "development"
            });

            // All log messages within this scope will include Datadog trace correlation
            _logger.LogInformation("=== HTTP TRIGGER STARTED === Method: {Method}, URL: {URL}", req.Method, req.Url);
            _logger.LogWarning("This is a WARNING level message to test logging levels");
            _logger.LogError("This is an ERROR level message to test logging levels");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions Worker with Serilog logging and Datadog trace correlation!");

            _logger.LogInformation("=== HTTP TRIGGER COMPLETED === Response: 200 OK");

            return response;
        }
    }
} 