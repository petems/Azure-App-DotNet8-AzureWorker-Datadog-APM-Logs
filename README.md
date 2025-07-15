# Azure Functions Worker with Datadog APM & Serilog

Azure Functions Worker (.NET 8) deployed to Azure App Service with integrated Datadog APM tracing and Serilog logging.

## Architecture

- **Runtime**: .NET 8 Azure Functions Worker (isolated process)
- **Deployment**: Azure App Service on Linux
- **Logging**: Serilog with structured JSON output
- **APM**: Datadog tracing (auto-injected in container)
- **Infrastructure**: Terraform for reproducible deployments

## Features

- HTTP-triggered Azure Function
- Clean logging pipeline (no double logging)
- Datadog APM integration ready
- Structured JSON logging to files
- Container deployment support
- Infrastructure as Code with Terraform

## Local Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Docker](https://www.docker.com/products/docker-desktop) (optional, for container testing)

### Running Locally

1. **Clone and setup**:
```bash
git clone <your-repo>
cd Azure-App-DotNet8-AzureWorker-Datadog-APM-Logs
```

2. **Install dependencies**:
```bash
dotnet restore
```

3. **Start the Functions host**:
```bash
func host start
```

The function will be available at:
- **HTTP Trigger**: `http://localhost:7071/api/HttpTrigger`

4. **Test the endpoint**:
```bash
curl http://localhost:7071/api/HttpTrigger
# Response: Welcome to Azure Functions Worker with Serilog logging!
```

### Local Logs

**Console output** (Serilog formatted):
```
[19:13:42 INF] === HTTP TRIGGER STARTED === Method: GET, URL: http://localhost:7071/api/HttpTrigger
[19:13:42 WRN] This is a WARNING level message to test logging levels
[19:13:42 ERR] This is an ERROR level message to test logging levels
[19:13:42 INF] === HTTP TRIGGER COMPLETED === Response: 200 OK
```

**File logs**: Check `logs/serilog.log` for structured JSON logs

## Container Deployment

### Build and Test Locally

**Linux Container**:
```bash
docker build -f Dockerfile.linux -t azure-functions-worker .
docker run -p 80:80 azure-functions-worker
```

**Windows Container**:
```bash
docker build -f Dockerfile.windows -t azure-functions-worker .
docker run -p 80:80 azure-functions-worker
```

## Azure Deployment

### Using Terraform

1. **Configure variables** in `main.tf`:
```hcl
variable "resource_group_name" {
  default = "your-resource-group"
}
```

2. **Deploy infrastructure**:
```bash
terraform init
terraform plan
terraform apply
```

3. **Get deployment URL**:
```bash
terraform output webapp_default_hostname
```

### Datadog Integration

The Terraform output provides the Datadog CI command:
```bash
terraform output datadog_ci_command
```

Run this command to instrument the deployed app with Datadog APM.

## Testing

Run the test suite:
```bash
dotnet test
```

Tests cover:
- Function instantiation
- Logger factory integration
- Basic function behavior

## Configuration

### Application Settings

**Local** (`local.settings.json`):
```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

**Production**: Configured via Terraform in App Service settings.

### Logging Configuration

- **Serilog**: Console + JSON file output
- **Datadog**: Auto-injected APM in container environment
- **No Application Insights**: Removed to prevent logging conflicts

## Project Structure

```
├── HttpTriggerFunction.cs    # Main HTTP trigger function
├── Program.cs                # Host configuration with Serilog
├── host.json                 # Functions runtime settings
├── local.settings.json       # Local development settings
├── main.tf                   # Terraform infrastructure
├── Dockerfile.linux          # Linux container definition
├── Dockerfile.windows        # Windows container definition
└── AzureAppServiceSample.Tests/ # Unit tests
```

## Troubleshooting

### Double Logging Issues
This project resolves double logging by:
- Clearing default logging providers
- Using only Serilog for all logging
- Removing Application Insights conflicts

See `DOUBLE_LOGGING_ANALYSIS.md` for detailed resolution.

### Local Development Issues
- Ensure Azure Functions Core Tools v4 is installed
- Check that port 7071 is available
- Verify .NET 8 SDK is installed

## Contributing

Contributions welcome. Please ensure tests pass and follow the established logging patterns. 