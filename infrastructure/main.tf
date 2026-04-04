data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "rg_lumina" {
  name     = "rg-lumina-poc-dev"
  location = "France Central"

  tags = {
    Projet        = "Lumina-Concept"
    Environnement = "Dev"
    Proprietaire  = "Jovann"
  }
}

resource "azurerm_storage_account" "st_lumina" {
  name                     = "stluminapocdevjobordeau" 
  resource_group_name      = azurerm_resource_group.rg_lumina.name
  location                 = azurerm_resource_group.rg_lumina.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = azurerm_resource_group.rg_lumina.tags
}

resource "azurerm_storage_container" "blob_lumina" {
  name                  = "entrepota102"
  storage_account_name  = azurerm_storage_account.st_lumina.name
  container_access_type = "private" 
}

resource "azurerm_servicebus_namespace" "sb_lumina" {
  name                = "sbluminapocdevjobordeau"
  location            = azurerm_resource_group.rg_lumina.location
  resource_group_name = azurerm_resource_group.rg_lumina.name
  sku                 = "Standard"

  tags = azurerm_resource_group.rg_lumina.tags
}

resource "azurerm_servicebus_topic" "sbt_orders" {
  name         = "sbt-lumina-orders"
  namespace_id = azurerm_servicebus_namespace.sb_lumina.id
}

resource "azurerm_servicebus_subscription" "sbs_process_order" {
  name               = "sbs-process-order"
  topic_id           = azurerm_servicebus_topic.sbt_orders.id
  max_delivery_count = 3
}

resource "azurerm_storage_account" "st_fn_lumina" {
  name                     = "stfnluminadevjobordeau"
  resource_group_name      = azurerm_resource_group.rg_lumina.name
  location                 = azurerm_resource_group.rg_lumina.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_service_plan" "asp_lumina" {
  name                = "asp-lumina-dev"
  resource_group_name = azurerm_resource_group.rg_lumina.name
  location            = azurerm_resource_group.rg_lumina.location
  os_type             = "Linux"
  sku_name            = "Y1" 
}

resource "azurerm_linux_function_app" "fn_lumina" {
  name                = "fn-lumina-processor-dev-jobordeau"
  resource_group_name = azurerm_resource_group.rg_lumina.name
  location            = azurerm_resource_group.rg_lumina.location

  identity {
    type = "SystemAssigned"
  }

  storage_account_name       = azurerm_storage_account.st_fn_lumina.name
  storage_account_access_key = azurerm_storage_account.st_fn_lumina.primary_access_key
  service_plan_id            = azurerm_service_plan.asp_lumina.id

  site_config {
    application_stack {
      dotnet_version = "8.0" 
      use_dotnet_isolated_runtime = true
    }
  }

  lifecycle {
    ignore_changes = [
      app_settings
    ]
  }
}

resource "azurerm_storage_account" "adls_lumina" {
  name                     = "adlsluminadevjobordeau" 
  resource_group_name      = azurerm_resource_group.rg_lumina.name
  location                 = azurerm_resource_group.rg_lumina.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  is_hns_enabled           = true 
}

resource "azurerm_storage_data_lake_gen2_filesystem" "fs_orders" {
  name               = "gold-orders"
  storage_account_id = azurerm_storage_account.adls_lumina.id
}

resource "azurerm_api_management" "apim_lumina" {
  name                = "apim-lumina-dev-jobordeau"
  location            = azurerm_resource_group.rg_lumina.location
  resource_group_name = azurerm_resource_group.rg_lumina.name
  publisher_name      = "Lumina POC"
  publisher_email     = "jbordeau2@myges.fr"
  sku_name = "Consumption_0" 
}

resource "azurerm_api_management_api" "api_orders" {
  name                = "lumina-orders-api"
  resource_group_name = azurerm_resource_group.rg_lumina.name
  api_management_name = azurerm_api_management.apim_lumina.name
  revision            = "1"
  display_name        = "Lumina E-Commerce API"
  path                = "ecommerce"
  protocols           = ["https"]
}

resource "azurerm_api_management_api_operation" "op_post_order" {
  operation_id        = "post-ecommerce-order"
  api_name            = azurerm_api_management_api.api_orders.name
  api_management_name = azurerm_api_management.apim_lumina.name
  resource_group_name = azurerm_resource_group.rg_lumina.name
  display_name        = "Créer une commande"
  method              = "POST"
  url_template        = "/orders"
  description         = "Reçoit un payload JSON e-commerce et le transforme au format canonique"
}

resource "azurerm_api_management_api_policy" "policy_orders" {
  api_name            = azurerm_api_management_api.api_orders.name
  api_management_name = azurerm_api_management.apim_lumina.name
  resource_group_name = azurerm_resource_group.rg_lumina.name

  xml_content = <<XML
<policies>
    <inbound>
        <base />
        <rate-limit calls="10" renewal-period="60" />
        
        <set-header name="X-Source-System" exists-action="override">
            <value>APIM-LUMINA</value>
        </set-header>
        
        <set-backend-service base-url="https://${azurerm_linux_function_app.fn_lumina.default_hostname}/api" />
    </inbound>
    <backend>
        <forward-request timeout="20" />
    </backend>
    <outbound>
        <base />
    </outbound>
    <on-error>
        <base />
    </on-error>
</policies>
XML
}

resource "azurerm_logic_app_workflow" "logicapp_lumina" {
  name                = "la-lumina-workflow-dev-jobordeau"
  location            = azurerm_resource_group.rg_lumina.location
  resource_group_name = azurerm_resource_group.rg_lumina.name

  lifecycle {
    ignore_changes = [
      parameters,
      workflow_parameters
    ]
  }
}

resource "azurerm_storage_data_lake_gen2_filesystem" "fs_failed_orders" {
  name               = "failed-orders"
  storage_account_id = azurerm_storage_account.adls_lumina.id
}

resource "azurerm_key_vault" "kv_lumina" {
  name                        = "kv-lumina-dev-jobordeau" # Doit être unique et faire max 24 caractères
  location                    = azurerm_resource_group.rg_lumina.location
  resource_group_name         = azurerm_resource_group.rg_lumina.name
  enabled_for_disk_encryption = true
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false
  sku_name                    = "standard"

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id
    secret_permissions = ["Get", "List", "Set", "Delete", "Recover", "Backup", "Restore", "Purge"]
  }

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = azurerm_linux_function_app.fn_lumina.identity[0].principal_id
    secret_permissions = ["Get", "List"]
  }
}

resource "azurerm_role_assignment" "role_adls_function" {
  scope                = azurerm_storage_account.adls_lumina.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_linux_function_app.fn_lumina.identity[0].principal_id
}

resource "azurerm_role_assignment" "role_sb_function" {
  scope                = azurerm_servicebus_namespace.sb_lumina.id
  role_definition_name = "Azure Service Bus Data Owner"
  principal_id         = azurerm_linux_function_app.fn_lumina.identity[0].principal_id
}
