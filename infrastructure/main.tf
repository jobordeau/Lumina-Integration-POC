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