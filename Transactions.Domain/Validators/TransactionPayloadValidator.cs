using FluentValidation;
using Transactions.Domain.Models;

namespace Transactions.Domain.Validators;

public class TransactionPayloadValidator : AbstractValidator<TransactionPayload>
{
    public TransactionPayloadValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty().WithMessage("TransactionId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(x => x.Merchant)
            .NotEmpty().WithMessage("Merchant name is required.");

        RuleFor(x => x.Timestamp)
            .NotEmpty().WithMessage("Timestamp is required.");
    }
}
