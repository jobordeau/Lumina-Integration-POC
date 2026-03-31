using Lumina.Integration.Processor.Core.Interfaces;
using Lumina.Integration.Processor.Core.Services;
using Lumina.Integration.Processor.Infrastructure.Adapters;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

builder.Services.AddScoped<IOrderProcessingService, OrderProcessingService>();
builder.Services.AddScoped<IOrderRepository, DataLakeOrderRepository>();

builder.Build().Run();