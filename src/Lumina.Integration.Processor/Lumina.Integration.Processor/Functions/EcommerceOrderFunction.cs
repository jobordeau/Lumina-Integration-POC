using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Lumina.Integration.Processor.Core.Models;

namespace Lumina.Integration.Processor.Functions
{
    public class EcommerceOrderFunction
    {
        private readonly ILogger _logger;
        private readonly LuminaSettings _settings;
        private readonly ServiceBusSender _sender;

        public EcommerceOrderFunction(
            ILoggerFactory loggerFactory,
            IOptions<LuminaSettings> settings,
            ServiceBusClient sbClient)
        {
            _logger = loggerFactory.CreateLogger<EcommerceOrderFunction>();
            _settings = settings.Value;
            _sender = sbClient.CreateSender(_settings.TopicName);
        }

        [Function(nameof(EcommerceOrderFunction)]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
        {
            _logger.LogInformation("Réception d'une commande E-commerce.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var json = JsonNode.Parse(requestBody);

            var canonicalOrder = new Order
            {
                OrderId = json["orderId"]?.ToString(),
                CustomerId = json["customerDetails"]?["customerId"]?.ToString(),
                OrderDate = DateTime.Parse(json["timestamp"]?.ToString()),
                Status = "Received_From_Web",
                TotalAmount = json["items"]?.AsArray().Sum(x =>
                    (decimal)x["qty"] * (decimal)x["unitPrice"]) ?? 0
            };

            string messagePayload = System.Text.Json.JsonSerializer.Serialize(canonicalOrder);
            await _sender.SendMessageAsync(new ServiceBusMessage(messagePayload));

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteStringAsync($"Commande {canonicalOrder.OrderId} transférée au bus.");
            return response;
        }
    }
}