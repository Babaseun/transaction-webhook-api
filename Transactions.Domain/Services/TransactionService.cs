using System.Threading.Tasks;
using Transactions.Data;
using Transactions.Domain.Models;
using Transactions.Domain.Exceptions;
using Transactions.Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Transactions.Domain.Services;

public record ProcessTransactionResult(bool IsDuplicate, Transaction? Transaction);

public interface ITransactionService
{
    Task<ProcessTransactionResult> ProcessTransactionAsync(TransactionPayload payload);
}

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly ILogger<TransactionService> _logger;
    private readonly TransactionSettings _settings;

    public TransactionService(ITransactionRepository repository, ILogger<TransactionService> logger, IOptions<TransactionSettings> options)
    {
        _repository = repository;
        _logger = logger;
        _settings = options.Value;
    }

    public async Task<ProcessTransactionResult> ProcessTransactionAsync(TransactionPayload payload)
    {
        // 1. Idempotency pre-check
        var exists = await _repository.ExistsByExternalIdAsync(payload.TransactionId);
        if (exists)
        {
            return new ProcessTransactionResult(true, null);
        }

        // 2. Derived computation
        decimal feeRate = _settings.FeeRate;
        decimal calculatedFee = payload.Amount * feeRate;
        decimal netAmount = payload.Amount - calculatedFee;

        var transaction = new Transaction
        {
            ExternalTransactionId = payload.TransactionId,
            Amount = payload.Amount,
            Currency = payload.Currency,
            Merchant = payload.Merchant,
            Timestamp = payload.Timestamp,
            CalculatedFee = calculatedFee,
            NetAmount = netAmount
        };

        await _repository.AddAsync(transaction);
        
        try
        {
            await _repository.SaveChangesAsync();
        }
        catch (DuplicateTransactionException)
        {
            _logger.LogWarning("Concurrent duplicate webhook received for {TransactionId}", payload.TransactionId);
            return new ProcessTransactionResult(true, null);
        }

        return new ProcessTransactionResult(false, transaction);
    }
}