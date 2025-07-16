# Configure the Azure provider
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.36.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.5.0"
    }
  }
  required_version = ">= 1.0.0"
}

data "azurerm_subscription" "current" {
}

# Variable for the resource group name
variable "resource_group_name" {
  description = "Name of the existing Azure resource group"
  type        = string
  default     = "petems-azureapp-dotnet-sandbox"
}

# Variable for Datadog API key
variable "dd_api_key" {
  description = "Datadog API key for APM and logging"
  type        = string
  sensitive   = true
}

provider "azurerm" {
  features {}
}

# Generate a random integer to create a globally unique name
resource "random_integer" "ri" {
  min = 10000
  max = 99999
}

# Use the existing default eastus resource group (using the Datadog sandbox repo)
data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

# Create the Linux App Service Plan
resource "azurerm_service_plan" "appserviceplan" {
  name                = "webapp-asp-${random_integer.ri.result}"
  location            = data.azurerm_resource_group.rg.location
  resource_group_name = data.azurerm_resource_group.rg.name
  os_type             = "Linux"
  sku_name            = "B1"
}

# Create the web app, pass in the App Service Plan ID
resource "azurerm_linux_web_app" "webapp" {
  name                  = "webapp-${random_integer.ri.result}"
  location              = data.azurerm_resource_group.rg.location
  resource_group_name   = data.azurerm_resource_group.rg.name
  service_plan_id       = azurerm_service_plan.appserviceplan.id
  depends_on            = [azurerm_service_plan.appserviceplan]
  https_only            = true
  
  site_config { 
    minimum_tls_version = "1.2"
    application_stack {
      dotnet_version = "8.0"
    }
    
    # Proposed sidecar configuration (GitHub issue #25167 - not yet implemented)
    # linux_fx_version = "SITECONTAINERS|mcr.microsoft.com/appsvc/dotnetcore:8.0|datadog/serverless-init:latest"
    
    # When GitHub issue #25167 is implemented, uncomment this:
    # sitecontainers {
    #   image = "mcr.microsoft.com/appsvc/dotnetcore:8.0"
    #   target_port = 80
    #   is_main = true
    # }
    # 
    # sitecontainers {
    #   image = "datadog/serverless-init:latest"
    #   target_port = 8126
    #   is_main = false
    # }
  }

  # Datadog environment variables
  # Note: Datadog sidecar container (datadog/serverless-init:latest) must be configured manually
  # via Azure Portal > Deployment > Deployment Center > Containers > Add > Custom container
  # Image: datadog/serverless-init:latest, Port: 8126
  app_settings = {
    # General Datadog settings
    "DD_API_KEY"                           = var.dd_api_key
    "DD_SITE"                             = "datadoghq.com"
    "DD_SERVICE"                          = "dotnetcore-hello-world"
    "DD_ENV"                              = "lab"
    "DD_SERVERLESS_LOG_PATH"              = "/home/LogFiles/*.log"
    "WEBSITES_ENABLE_APP_SERVICE_STORAGE" = "true"
    
    # .NET specific Datadog settings
    "DD_DOTNET_TRACER_HOME"     = "/home/site/wwwroot/datadog"
    "DD_TRACE_LOG_DIRECTORY"    = "/home/LogFiles/dotnet"
    "CORECLR_ENABLE_PROFILING"  = "1"
    "CORECLR_PROFILER"          = "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"
    "CORECLR_PROFILER_PATH"     = "/home/site/wwwroot/datadog/linux-x64/Datadog.Trace.ClrProfiler.Native.so"
    "DD_PROFILING_ENABLED"      = "true"
  }

  logs {
    detailed_error_messages = true
    failed_request_tracing  = true
    
    application_logs {
      file_system_level = "Verbose"
    }
    
    http_logs {
      file_system {
        retention_in_days = 7
        retention_in_mb   = 100
      }
    }
  }
}

#  Deploy code from a public GitHub repo
resource "azurerm_app_service_source_control" "sourcecontrol" {
  app_id             = azurerm_linux_web_app.webapp.id
  repo_url           = "https://github.com/petems/Azure-App-DotNet8-AzureWorker-Datadog-APM-Logs"
  branch             = "master"
  use_manual_integration = true
  use_mercurial      = false
}

# Output the default hostname of the webapp
output "webapp_default_hostname" {
  description = "Default hostname of the webapp"
  value       = "https://${azurerm_linux_web_app.webapp.default_hostname}"
}

# Datadog sidecar container via ARM template (workaround until GitHub issue #25167 is resolved)
resource "azurerm_resource_group_template_deployment" "datadog_sidecar" {
  name                = "datadog-sidecar-deployment"
  resource_group_name = data.azurerm_resource_group.rg.name
  deployment_mode     = "Incremental"
  depends_on          = [azurerm_linux_web_app.webapp]

  template_content = jsonencode({
    "$schema"      = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"
    contentVersion = "1.0.0.0"
    parameters = {
      webAppName = {
        type = "string"
      }
    }
    resources = [
      {
        type       = "Microsoft.Web/sites/sitecontainers"
        apiVersion = "2024-04-01"
        name       = "[concat(parameters('webAppName'), '/datadog-sidecar')]"
        properties = {
          image                = "datadog/serverless-init:latest"
          isMain               = false
          authType            = "Anonymous"
          volumeMounts        = []
          environmentVariables = []
        }
      }
    ]
  })

  parameters_content = jsonencode({
    webAppName = {
      value = azurerm_linux_web_app.webapp.name
    }
  })
}

output "datadog_ci_command" {
  description = "Command to run Datadog CI"
  value       = "datadog-ci aas instrument -s ${data.azurerm_subscription.current.subscription_id} -g ${data.azurerm_resource_group.rg.name} -n ${resource.azurerm_linux_web_app.webapp.name} --service=dotnetcore-hello-world --env=lab"
}
