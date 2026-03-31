using System;
using System.Threading.Tasks;
using Lumina.Integration.Processor.Core.Interfaces;
using Lumina.Integration.Processor.Core.Models;

namespace Lumina.Integration.Processor.Core.Services
{
    public class OrderProcessingService : IOrderProcessingService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderProcessingService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<bool> ProcessOrderAsync(Order order)
        {
            if (order == null) return false;

            var validator = new OrderValidator();
            var validationResult = validator.Validate(order);

            if (!validationResult.IsValid)
            {
                Console.WriteLine($"[Erreur] La commande {order.OrderId} est invalide :");
                foreach (var failure in validationResult.Errors)
                {
                    Console.WriteLine($"- {failure.ErrorMessage}");
                }
                return false;
            }

            Console.WriteLine($"[Succès] Validation passée pour {order.OrderId} ({order.TotalAmount}€).");

            await _orderRepository.SaveOrderAsync(order);

            return true;
        }
    }
}