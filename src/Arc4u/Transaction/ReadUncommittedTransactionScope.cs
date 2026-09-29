using System.Transactions;

namespace Arc4u.Transaction;

/// <summary>
/// A transaction scope with the <see cref="IsolationLevel.ReadUncommitted"/> isolation level.
/// </summary>
public class ReadUncommittedTransactionScope : BaseTransactionScope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadUncommittedTransactionScope"/> class.
    /// </summary>
    public ReadUncommittedTransactionScope() : base(IsolationLevel.ReadUncommitted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadUncommittedTransactionScope"/> class with a scope option.
    /// </summary>
    /// <param name="transactionScopeOption">The requested scope option (see <see cref="BaseTransactionScope(TransactionScopeOption, IsolationLevel)"/>).</param>
    public ReadUncommittedTransactionScope(TransactionScopeOption transactionScopeOption) : base(transactionScopeOption, IsolationLevel.ReadUncommitted)
    {
    }
}
