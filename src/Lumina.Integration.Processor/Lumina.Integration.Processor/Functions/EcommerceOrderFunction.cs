using System.Linq;
using System.Net;
using System.Text.Json;
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

        [Function(nameof(EcommerceOrderFunction))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
        {
            _logger.LogInformation("Réception d'une commande E-commerce.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

 
            Order canonicalOrder;
            try
            {
                var json = JsonNode.Parse(requestBody);
                if (json is null)
                {
                    return await PlainTextResponse(req, HttpStatusCode.BadRequest, "Body JSON vide ou invalide.");
                }

                canonicalOrder = new Order
                {
                    OrderId = json["orderId"]?.ToString(),
                    CustomerId = json["customerDetails"]?["customerId"]?.ToString(),
                    OrderDate = DateTime.TryParse(json["timestamp"]?.ToString(), out var dt) ? dt : DateTime.UtcNow,
                    Status = "Received_From_Web",
                    TotalAmount = json["items"]?.AsArray().Sum(x =>
                        (decimal?)x?["qty"] * (decimal?)x?["unitPrice"]) ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec du parsing du body e-commerce.");
                return await PlainTextResponse(req, HttpStatusCode.BadRequest, "Format JSON e-commerce invalide.");
            }

            var validator = new OrderValidator();
            var validationResult = validator.Validate(canonicalOrder);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "FluentValidation a détecté {Count} erreurs · OrderId={OrderId}",
                    validationResult.Errors.Count,
                    canonicalOrder.OrderId ?? "(null)");

                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
                var payload = JsonSerializer.Serialize(new
                {
                    isValid = false,
                    errors = validationResult.Errors.Select(e => new
                    {
                        propertyName = e.PropertyName,
                        errorMessage = e.ErrorMessage,
                        attemptedValue = e.AttemptedValue?.ToString() ?? ""
                    })
                });
                await errorResponse.WriteStringAsync(payload);
                return errorResponse;
            }

            string messagePayload = JsonSerializer.Serialize(canonicalOrder);
            var sbMessage = new ServiceBusMessage(messagePayload)
            {
                MessageId = canonicalOrder.OrderId
            };
            await _sender.SendMessageAsync(sbMessage);

            _logger.LogInformation(
                "Commande {OrderId} publiée sur {Topic} · MessageId={MessageId}",
                canonicalOrder.OrderId, _settings.TopicName, sbMessage.MessageId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(new
            {
                orderId = canonicalOrder.OrderId,
                status = "accepted",
                message = $"Commande {canonicalOrder.OrderId} transférée au bus."
            }));
            return response;
        }

        private static async Task<HttpResponseData> PlainTextResponse(
            HttpRequestData req, HttpStatusCode code, string message)
        {
            var response = req.CreateResponse(code);
            await response.WriteStringAsync(message);
            return response;
        }
    }
}