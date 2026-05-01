using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Transactions.Domain.Models;
using Transactions.Domain.Services;

namespace Transactions.API.Controllers;

[ApiController]
[Route("webhooks/[controller]")]
public class TransactionsController(
    ITransactionService transactionService,
    IValidator<TransactionPayload> validator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostTransaction([FromBody] TransactionPayload payload)
    {
        var validation = await validator.ValidateAsync(payload);
        if (!validation.IsValid)
            return BadRequest(validation.ToDictionary());

        var result = await transactionService.ProcessTransactionAsync(payload);

        if (result.IsDuplicate)
            return Ok(new { Message = "Transaction already processed." });

        var transaction = result.Transaction!;

        return Created($"/transactions/{transaction.Id}", new
        {
            transaction.ExternalTransactionId,
            transaction.Amount,
            transaction.CalculatedFee,
            transaction.NetAmount
        });
    }
}
