using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Lumina.Functions
{
    public class FailedOrderFunction
    {
        private readonly ILogger<FailedOrderFunction> _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public FailedOrderFunction(ILogger<FailedOrderFunction> logger, BlobServiceClient blobServiceClient)
        {
            _logger = logger;
            _blobServiceClient = blobServiceClient;
        }

        [Function(nameof(FailedOrderFunction))]
        public async Task Run(
            [ServiceBusTrigger(
                "sbt-lumina-orders",
                "sbs-process-order/$DeadLetterQueue",
                Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
        {
            _logger.LogWarning($"[DLQ] Message mort détecté. MessageId: {message.MessageId}");

            try
            {
                string messageBody = message.Body.ToString();

                string fileName = $"failed-order-{message.MessageId ?? Guid.NewGuid().ToString()}.json";

                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient("failed-orders");

                await containerClient.CreateIfNotExistsAsync();

                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                using (var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(messageBody)))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                _logger.LogInformation($"[DLQ] Opération réussie. Message sauvegardé sous : failed-orders/{fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DLQ] Échec critique lors de la sauvegarde du message : {ex.Message}");

                throw;
            }
        }
    }
}