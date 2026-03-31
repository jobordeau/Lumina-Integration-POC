terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0" # On utilise la version 3.x, reconnue pour sa stabilité
    }
  }
}

provider "azurerm" {
  features {} 
}