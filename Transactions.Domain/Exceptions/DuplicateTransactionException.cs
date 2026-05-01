using System;

namespace Transactions.Domain.Exceptions;

public class DuplicateTransactionException : Exception
{
    public DuplicateTransactionException(string message) : base(message)
    {
    }
}
