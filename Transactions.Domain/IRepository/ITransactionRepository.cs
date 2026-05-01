using Transactions.Domain.Models;

namespace Transactions.Data;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<bool> ExistsByExternalIdAsync(string externalId);
}