using System;

namespace Transactions.Domain.Models;

public record TransactionPayload(
    string TransactionId, 
    decimal Amount, 
    string Currency, 
    string Merchant, 
    DateTime Timestamp
);
