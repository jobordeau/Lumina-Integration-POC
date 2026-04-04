using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Lumina.Integration.Processor.Core.Interfaces;
using Lumina.Integration.Processor.Core.Models;

namespace Lumina.Integration.Processor.Infrastructure.Adapters
{
    public class DataLakeOrderRepository : IOrderRepository
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string ContainerName = "gold-orders";

        public DataLakeOrderRepository(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public async Task SaveOrderAsync(Order order)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);

            string fileName = $"{order.OrderId}.json";
            var blobClient = containerClient.GetBlobClient(fileName);

            string jsonContent = JsonSerializer.Serialize(order, new JsonSerializerOptions { WriteIndented = true });

            await blobClient.UploadAsync(BinaryData.FromString(jsonContent), overwrite: true);

            Console.WriteLine($"[Infrastructure] Le fichier {fileName} a été écrit dans le Data Lake en mode Passwordless !");
        }
    }
}