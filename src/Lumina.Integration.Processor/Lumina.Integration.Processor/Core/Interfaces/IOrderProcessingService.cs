using System.Threading.Tasks;
using Lumina.Integration.Processor.Core.Models;

namespace Lumina.Integration.Processor.Core.Interfaces
{
    public interface IOrderProcessingService
    {
        Task<bool> ProcessOrderAsync(Order order);
    }
}
