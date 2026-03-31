using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Lumina.Integration.Processor.Core.Interfaces;
using Lumina.Integration.Processor.Core.Models;

namespace Lumina.Integration.Processor.Functions
{
    public class OrderProcessorFunction
    {
        private readonly ILogger<OrderProcessorFunction> _logger;
        private readonly IOrderProcessingService _orderService;

        public OrderProcessorFunction(ILogger<OrderProcessorFunction> logger, IOrderProcessingService orderService)
        {
            _logger = logger;
            _orderService = orderService;
        }

        [Function(nameof(OrderProcessorFunction))]
        public async Task Run([ServiceBusTrigger("sbt-lumina-orders", "sbs-process-order", Connection = "ServiceBusConnection")] string messageBody)
        {
            _logger.LogInformation("Nouveau message reçu depuis le Service Bus.");

            try
            {
                var jsonNode = JsonNode.Parse(messageBody);

                var commandeNode = jsonNode["Commande"];
                var clientNode = commandeNode["Client"];
                var ligneNode = commandeNode["LignesDeCommande"]["Ligne"];

                string strQuantite = ligneNode["Quantite"].ToString();
                string strPrixUnitaire = ligneNode["PrixUnitaire"].ToString();

                decimal quantite = decimal.Parse(strQuantite, System.Globalization.CultureInfo.InvariantCulture);
                decimal prixUnitaire = decimal.Parse(strPrixUnitaire, System.Globalization.CultureInfo.InvariantCulture);

                decimal totalCalcule = quantite * prixUnitaire;


                var order = new Order
                {
                    OrderId = (string)commandeNode["Identifiant"],
                    CustomerId = (string)clientNode["NumeroClient"],
                    OrderDate = (DateTime)commandeNode["DateCreation"],
                    TotalAmount = totalCalcule,
                    Status = "New"
                };

                _logger.LogInformation("Traduction réussie ! Envoi de la commande {OrderId} au Core. Montant calculé : {TotalAmount}", order.OrderId, order.TotalAmount);

                bool isValid = await _orderService.ProcessOrderAsync(order);

                if (isValid)
                {
                    _logger.LogInformation("Succès : La commande {OrderId} a été traitée et validée.", order.OrderId);
                }
                else
                {
                    _logger.LogWarning("Rejet : La commande {OrderId} n'a pas passé les règles métier.", order.OrderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur fatale lors du traitement du message.");
                throw;
            }
        }
    }
}