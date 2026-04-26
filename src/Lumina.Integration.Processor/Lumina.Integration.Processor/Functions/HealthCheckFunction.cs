using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Lumina.Integration.Processor.Functions
{
    public class HealthCheckFunction
    {
        private readonly ILogger<HealthCheckFunction> _logger;

        public HealthCheckFunction(ILogger<HealthCheckFunction> logger)
        {
            _logger = logger;
        }

        [Function(nameof(HealthCheckFunction))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
                Route = "health")] HttpRequestData req)
        {
            _logger.LogInformation("Health check called.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.Headers.Add("Cache-Control", "no-cache, no-store");

            await response.WriteStringAsync(JsonSerializer.Serialize(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow.ToString("o"),
                version = "1.0.0"
            }));

            return response;
        }
    }
}