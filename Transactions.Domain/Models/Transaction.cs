using System;

namespace Transactions.Domain.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal CalculatedFee { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
