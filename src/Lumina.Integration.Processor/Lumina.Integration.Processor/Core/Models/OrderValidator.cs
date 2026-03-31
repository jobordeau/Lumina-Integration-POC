using FluentValidation;

namespace Lumina.Integration.Processor.Core.Models
{
    public class OrderValidator : AbstractValidator<Order>
    {
        public OrderValidator()
        {
            RuleFor(order => order.OrderId)
                .NotEmpty().WithMessage("L'identifiant de la commande est obligatoire.");

            RuleFor(order => order.CustomerId)
                .NotEmpty().WithMessage("Le numéro client est obligatoire.");

            RuleFor(order => order.TotalAmount)
                .GreaterThan(0).WithMessage("Le montant de la commande doit être positif.");
        }
    }
}