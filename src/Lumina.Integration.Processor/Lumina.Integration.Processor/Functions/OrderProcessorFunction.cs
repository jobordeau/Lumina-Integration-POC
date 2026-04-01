using System;
using System.Text.Json;
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
            _logger.LogInformation("Nouveau message reçu depuis le Service Bus. Tentative de traitement du format canonique.");

            try
            {
                var order = JsonSerializer.Deserialize<Order>(messageBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (order == null || string.IsNullOrEmpty(order.OrderId))
                {
                    _logger.LogWarning("Le message reçu n'est pas au format canonique attendu. Message ignoré ou défectueux.");
                    return;
                }

                _logger.LogInformation("Message canonique valide ! Envoi de la commande {OrderId} au Core. Montant : {TotalAmount}", order.OrderId, order.TotalAmount);

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
                _logger.LogError(ex, "Erreur fatale lors du traitement du message. Il sera renvoyé dans le bus (puis DLQ si échec répété).");
                throw;
            }
        }
    }
}