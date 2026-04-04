using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Lumina.Integration.Processor.Functions
{
    public class FailedOrderFunction
    {
        private readonly ILogger<FailedOrderFunction> _logger;

        public FailedOrderFunction(ILogger<FailedOrderFunction> logger)
        {
            _logger = logger;
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

                string dataLakeConnectionString = Environment.GetEnvironmentVariable("DataLakeConnection");

                if (string.IsNullOrEmpty(dataLakeConnectionString))
                {
                    _logger.LogError("[DLQ] La chaîne de connexion au Data Lake est introuvable.");
                    return;
                }

                BlobServiceClient dataLakeClient = new BlobServiceClient(dataLakeConnectionString);
                BlobContainerClient containerClient = dataLakeClient.GetBlobContainerClient("failed-orders");

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