using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Lumina.Integration.Processor.Core.Interfaces;
using Lumina.Integration.Processor.Core.Services;
using Lumina.Integration.Processor.Infrastructure.Adapters;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddScoped<IOrderProcessingService, OrderProcessingService>();
builder.Services.AddScoped<IOrderRepository, DataLakeOrderRepository>();

builder.Build().Run();