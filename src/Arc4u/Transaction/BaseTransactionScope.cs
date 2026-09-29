using System.Transactions;

namespace Arc4u.Transaction;

/// <summary>
/// Base class of the transaction scopes that flow across asynchronous calls
/// (<see cref="TransactionScopeAsyncFlowOption.Enabled"/>) and run with a given isolation level.
/// </summary>
public abstract class BaseTransactionScope : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseTransactionScope"/> class.
    /// </summary>
    /// <param name="transactionScopeOption">The requested scope option. Note that the underlying <see cref="TransactionScope"/> is always created with <see cref="TransactionScopeOption.Required"/>.</param>
    /// <param name="isolationLevel">The isolation level of the transaction.</param>
    public BaseTransactionScope(TransactionScopeOption transactionScopeOption, IsolationLevel isolationLevel)
    {
        var transactionOption = new TransactionOptions
        {
            IsolationLevel = isolationLevel
        };

        transactionScope = new TransactionScope(TransactionScopeOption.Required, transactionOption, TransactionScopeAsyncFlowOption.Enabled);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseTransactionScope"/> class that requires a transaction (<see cref="TransactionScopeOption.Required"/>).
    /// </summary>
    /// <param name="isolationLevel">The isolation level of the transaction.</param>
    public BaseTransactionScope(IsolationLevel isolationLevel) : this(TransactionScopeOption.Required, isolationLevel)
    {
    }

    /// <summary>
    /// Indicates whether the scope has already been disposed.
    /// </summary>
    protected bool disposed;
    /// <summary>
    /// The underlying <see cref="System.Transactions.TransactionScope"/>.
    /// </summary>
    protected TransactionScope transactionScope;

    /// <summary>
    /// Indicates that all the operations of the scope completed successfully, so that the transaction can be committed when the scope is disposed.
    /// </summary>
    public virtual void Complete()
    {
        transactionScope?.Complete();
    }

    /// <summary>
    /// Disposes the scope. The transaction is rolled back if <see cref="Complete"/> was not called.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the underlying <see cref="System.Transactions.TransactionScope"/>.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>; derived classes with a finalizer pass <see langword="false"/>.</param>
    protected virtual void Dispose(Boolean disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                transactionScope?.Dispose();
                disposed = true;
            }
        }
    }
}
