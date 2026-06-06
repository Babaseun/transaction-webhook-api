using Microsoft.EntityFrameworkCore;
using Transactions.Domain.Models;

namespace Transactions.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var transaction = modelBuilder.Entity<Transaction>();

        transaction.HasIndex(t => t.ExternalTransactionId)
            .IsUnique();

        transaction.Property(t => t.Amount).HasPrecision(18, 2);
    }
}