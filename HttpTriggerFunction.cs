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
            // Get current Datadog trace and span IDs for manual injection
            var activeScope = Tracer.Instance.ActiveScope;
            var traceId = activeScope?.Span?.TraceId.ToString() ?? "0";
            var spanId = activeScope?.Span?.SpanId.ToString() ?? "0";
            
            // Read Datadog configuration from environment variables (same as Datadog agent)
            var ddService = Environment.GetEnvironmentVariable("DD_SERVICE") ?? "unknown-service";
            var ddVersion = Environment.GetEnvironmentVariable("DD_VERSION") ?? "unknown-version";
            var ddEnv = Environment.GetEnvironmentVariable("DD_ENV") ?? "unknown-env";
            
            // Use scoped logging to include Datadog trace correlation
            // Application Insights is only being used for it's logging capabilities
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["dd.trace_id"] = traceId,
                ["dd.span_id"] = spanId,
                ["dd.service"] = ddService,
                ["dd.version"] = ddVersion,
                ["dd.env"] = ddEnv
            });

            // Application Insights Logs + Datadog trace correlation additions
            _logger.LogInformation("=== HTTP TRIGGER STARTED === Method: {Method}, URL: {URL}", req.Method, req.Url);
            _logger.LogInformation("Datadog Trace ID: {TraceId}, Span ID: {SpanId}", traceId, spanId);
            _logger.LogInformation("Datadog Config - Service: {Service}, Version: {Version}, Env: {Environment}", 
                ddService, ddVersion, ddEnv);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions with Application Insights logging and Datadog tracing!");

            _logger.LogInformation("=== HTTP TRIGGER COMPLETED === Response: 200 OK");

            return response;
        }
    }
} 