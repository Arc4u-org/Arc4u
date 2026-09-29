using System.Transactions;

namespace Arc4u.Transaction;

/// <summary>
/// A transaction scope with the <see cref="IsolationLevel.ReadCommitted"/> isolation level.
/// </summary>
public class ReadCommittedTransactionScope : BaseTransactionScope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadCommittedTransactionScope"/> class.
    /// </summary>
    public ReadCommittedTransactionScope() : base(IsolationLevel.ReadCommitted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadCommittedTransactionScope"/> class with a scope option.
    /// </summary>
    /// <param name="transactionScopeOption">The requested scope option (see <see cref="BaseTransactionScope(TransactionScopeOption, IsolationLevel)"/>).</param>
    public ReadCommittedTransactionScope(TransactionScopeOption transactionScopeOption) : base(transactionScopeOption, IsolationLevel.ReadCommitted)
    {
    }
}
