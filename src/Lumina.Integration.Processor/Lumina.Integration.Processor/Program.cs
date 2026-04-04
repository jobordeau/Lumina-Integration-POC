using Lumina.Integration.Processor.Core.Interfaces;
using Lumina.Integration.Processor.Core.Models;
using Lumina.Integration.Processor.Core.Services;
using Lumina.Integration.Processor.Infrastructure.Adapters;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.Configure<LuminaSettings>(context.Configuration.GetSection("Lumina"));

        services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddServiceBusClient(context.Configuration["Lumina:ServiceBusConnectionString"]);
            clientBuilder.AddBlobServiceClient(context.Configuration["AzureWebJobsStorage"]);
        });

        services.AddScoped<IOrderProcessingService, OrderProcessingService>();
        services.AddScoped<IOrderRepository, DataLakeOrderRepository>();
    })
    .Build();

await host.RunAsync();
