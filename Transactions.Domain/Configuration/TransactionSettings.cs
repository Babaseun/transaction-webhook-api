namespace Transactions.Domain.Configuration;

public class TransactionSettings
{
    public const string SectionName = "TransactionSettings";

    public decimal FeeRate { get; set; } = 0.015m; // Default to 1.5% if not provided
}
