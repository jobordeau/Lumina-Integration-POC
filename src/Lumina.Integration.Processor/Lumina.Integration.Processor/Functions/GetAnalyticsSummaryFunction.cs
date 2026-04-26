using System.Net;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Lumina.Integration.Processor.Functions
{
    /// <summary>
    /// Renvoie le résumé analytique calculé par le notebook Fabric.
    /// Lit analytics-summary/latest.json et le retourne tel quel.
    /// </summary>
    public class GetAnalyticsSummaryFunction
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public GetAnalyticsSummaryFunction(
            ILoggerFactory loggerFactory,
            BlobServiceClient blobServiceClient)
        {
            _logger = loggerFactory.CreateLogger<GetAnalyticsSummaryFunction>();
            _blobServiceClient = blobServiceClient;
        }

        [Function(nameof(GetAnalyticsSummaryFunction))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
                Route = "analytics/summary")] HttpRequestData req)
        {
            _logger.LogInformation("Analytics summary requested.");

            try
            {
                var container = _blobServiceClient.GetBlobContainerClient("analytics-summary");
                var blob = container.GetBlobClient("latest.json");

                if (!await blob.ExistsAsync())
                {
                    return await JsonResponse(req, HttpStatusCode.NotFound, new
                    {
                        error = "not_yet_computed",
                        message = "Le notebook Fabric n'a pas encore généré de résumé. " +
                                  "Veuillez lancer une exécution manuelle ou attendre le prochain run planifié."
                    });
                }

                var content = await blob.DownloadContentAsync();
                var properties = await blob.GetPropertiesAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                // Cache 60s côté navigateur — on actualise le notebook toutes les 30 min
                response.Headers.Add("Cache-Control", "public, max-age=60");
                response.Headers.Add("X-Snapshot-LastModified",
                    properties.Value.LastModified.ToString("o"));

                await response.WriteStringAsync(content.Value.Content.ToString());
                return response;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Erreur lookup analytics-summary");
                return await JsonResponse(req, HttpStatusCode.InternalServerError, new
                {
                    error = "storage_error",
                    message = ex.Message,
                });
            }
        }

        private static async Task<HttpResponseData> JsonResponse(
            HttpRequestData req, HttpStatusCode code, object payload)
        {
            var response = req.CreateResponse(code);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(payload));
            return response;
        }
    }
}