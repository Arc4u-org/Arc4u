using System.Transactions;

namespace Arc4u.Transaction;

/// <summary>
/// A transaction scope with the <see cref="IsolationLevel.Serializable"/> isolation level.
/// </summary>
public class SerializeTransactionScope : BaseTransactionScope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SerializeTransactionScope"/> class.
    /// </summary>
    public SerializeTransactionScope() : base(IsolationLevel.Serializable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializeTransactionScope"/> class with a scope option.
    /// </summary>
    /// <param name="transactionScopeOption">The requested scope option (see <see cref="BaseTransactionScope(TransactionScopeOption, IsolationLevel)"/>).</param>
    public SerializeTransactionScope(TransactionScopeOption transactionScopeOption) : base(transactionScopeOption, IsolationLevel.Serializable)
    {
    }
}
