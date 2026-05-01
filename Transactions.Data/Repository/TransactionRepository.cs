using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Transactions.Domain.Models;
using Transactions.Domain.Exceptions;

namespace Transactions.Data.Repository;

public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext ctx) : base(ctx)
    {
    }

    public async Task<bool> ExistsByExternalIdAsync(string externalId)
    {
        return await _ctx.Transactions.AnyAsync(t => t.ExternalTransactionId == externalId);
    }

    public override async Task SaveChangesAsync()
    {
        try
        {
            await base.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("23505") == true)
        {
            throw new DuplicateTransactionException("Transaction already processed.");
        }
    }
}
