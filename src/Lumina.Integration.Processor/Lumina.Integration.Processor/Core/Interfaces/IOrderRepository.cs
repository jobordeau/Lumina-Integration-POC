using System.Threading.Tasks;
using Lumina.Integration.Processor.Core.Models;

namespace Lumina.Integration.Processor.Core.Interfaces
{
    public interface IOrderRepository
    {
        Task SaveOrderAsync(Order order);
    }
}