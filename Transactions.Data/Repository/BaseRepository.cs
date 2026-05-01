using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Transactions.Data.Repository;

public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _ctx;
    private readonly DbSet<T> _entitySet;

    protected BaseRepository(AppDbContext ctx)
    {
        _ctx = ctx;
        _entitySet = ctx.Set<T>();
    }
    
    public async Task AddAsync(T entity)
    {
        if (entity != null)
        {
            await _entitySet.AddAsync(entity);
        }
    }

    public virtual async Task SaveChangesAsync()
    {
        await _ctx.SaveChangesAsync();
    }
}