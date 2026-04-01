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