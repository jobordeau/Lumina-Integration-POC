using Lumina.Integration.Processor.Core.Interfaces;
using Lumina.Integration.Processor.Core.Services;
using Lumina.Integration.Processor.Infrastructure.Adapters;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddScoped<IOrderProcessingService, OrderProcessingService>();
        services.AddScoped<IOrderRepository, DataLakeOrderRepository>();
    })
    .Build();

await host.RunAsync();
