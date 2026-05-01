using Microsoft.EntityFrameworkCore;
using Transactions.Domain.Models;

namespace Transactions.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.ExternalTransactionId)
            .IsUnique(); 
    }
}
