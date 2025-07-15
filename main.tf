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
  repo_url           = "https://github.com/petems/Azure-App-Service-Terraform-Datadog-APM-Logs-Sandbox"
  branch             = "master"
  use_manual_integration = true
  use_mercurial      = false
}

# Output the default hostname of the webapp
output "webapp_default_hostname" {
  description = "Default hostname of the webapp"
  value       = "https://${azurerm_linux_web_app.webapp.default_hostname}"
}

output "datadog_ci_command" {
  description = "Command to run Datadog CI"
  value       = "datadog-ci aas instrument -s ${data.azurerm_subscription.current.subscription_id} -g ${data.azurerm_resource_group.rg.name} -n ${resource.azurerm_linux_web_app.webapp.name} --service=dotnetcore-hello-world --env=lab"
}