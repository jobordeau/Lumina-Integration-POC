using System.Net;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Lumina.Integration.Processor.Functions
{
    public class GetOrderStatusFunction
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public GetOrderStatusFunction(
            ILoggerFactory loggerFactory,
            BlobServiceClient blobServiceClient)
        {
            _logger = loggerFactory.CreateLogger<GetOrderStatusFunction>();
            _blobServiceClient = blobServiceClient;
        }

        [Function(nameof(GetOrderStatusFunction))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
                Route = "orders/{orderId}/status")] HttpRequestData req,
            string orderId)
        {
            _logger.LogInformation("Lookup status · OrderId={OrderId}", orderId);

            var alert = await TryReadAlertAsync(orderId);

            try
            {
                var goldContainer = _blobServiceClient.GetBlobContainerClient("gold-orders");
                var goldBlob = goldContainer.GetBlobClient($"{orderId}.json");
                if (await goldBlob.ExistsAsync())
                {
                    var content = await goldBlob.DownloadContentAsync();
                    var bodyJson = content.Value.Content.ToString();
                    return await JsonResponse(req, HttpStatusCode.OK, new
                    {
                        orderId,
                        status = "completed",
                        location = $"gold-orders/{orderId}.json",
                        body = JsonDocument.Parse(bodyJson).RootElement,
                        alert
                    });
                }
            }
            catch (RequestFailedException ex)
            {
                _logger.LogWarning(ex, "Erreur lookup gold-orders");
            }

            try
            {
                var failedContainer = _blobServiceClient.GetBlobContainerClient("failed-orders");
                var failedBlob = failedContainer.GetBlobClient($"failed-order-{orderId}.json");
                if (await failedBlob.ExistsAsync())
                {
                    var content = await failedBlob.DownloadContentAsync();
                    var bodyJson = content.Value.Content.ToString();
                    return await JsonResponse(req, HttpStatusCode.OK, new
                    {
                        orderId,
                        status = "dead-lettered",
                        location = $"failed-orders/failed-order-{orderId}.json",
                        body = JsonDocument.Parse(bodyJson).RootElement,
                        alert
                    });
                }
            }
            catch (RequestFailedException ex)
            {
                _logger.LogWarning(ex, "Erreur lookup failed-orders");
            }

            return await JsonResponse(req, HttpStatusCode.OK, new
            {
                orderId,
                status = "pending",
                message = "Commande non encore persistée. Probablement encore en transit dans Service Bus.",
                alert
            });
        }

        private async Task<object?> TryReadAlertAsync(string orderId)
        {
            try
            {
                var alertsContainer = _blobServiceClient.GetBlobContainerClient("alerts-sent");
                var alertBlob = alertsContainer.GetBlobClient($"{orderId}.json");
                if (await alertBlob.ExistsAsync())
                {
                    var content = await alertBlob.DownloadContentAsync();
                    var alertJson = content.Value.Content.ToString();
                    return new
                    {
                        sent = true,
                        location = $"alerts-sent/{orderId}.json",
                        details = JsonDocument.Parse(alertJson).RootElement
                    };
                }
            }
            catch (RequestFailedException ex)
            {
                _logger.LogWarning(ex, "Erreur lookup alerts-sent · {OrderId}", orderId);
            }

            return new { sent = false };
        }

        private static async Task<HttpResponseData> JsonResponse(
            HttpRequestData req, HttpStatusCode code, object payload)
        {
            var response = req.CreateResponse(code);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = false
            }));
            return response;
        }
    }
}