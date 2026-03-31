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
        private readonly string _connectionString;
        private const string ContainerName = "gold-orders";

        public DataLakeOrderRepository()
        {
            _connectionString = Environment.GetEnvironmentVariable("DataLakeConnection");
        }

        public async Task SaveOrderAsync(Order order)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

            string fileName = $"{order.OrderId}.json";
            var blobClient = containerClient.GetBlobClient(fileName);

            string jsonContent = JsonSerializer.Serialize(order, new JsonSerializerOptions { WriteIndented = true });

            await blobClient.UploadAsync(BinaryData.FromString(jsonContent), overwrite: true);

            Console.WriteLine($"[Infrastructure] Le fichier {fileName} a été écrit dans le Data Lake avec succès.");
        }
    }
}