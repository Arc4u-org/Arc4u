using Serilog;
using Serilog.Core;

namespace Arc4u.Diagnostics.Serilog;

/// <summary>
/// Base class of the Serilog based <see cref="ILogWriter"/> implementations. <see cref="Initialize"/> creates a logger with the minimum level set to Verbose
/// and the log context enricher, then lets the derived class complete the configuration in <see cref="Configure(LoggerConfiguration)"/>.
/// </summary>
public abstract class SerilogWriter : ILogWriter
{
    private bool _isInitialized;
    private bool _disposed;
    private static readonly object _locker = new();
    private Logger? _logger;

    /// <summary>Configures the Serilog sinks, enrichers and filters of the logger.</summary>
    /// <param name="configurator">The logger configuration to complete.</param>
    public abstract void Configure(LoggerConfiguration configurator);

    /// <summary>Gets the Serilog logger created by <see cref="Initialize"/>.</summary>
    /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
    public Logger Logger => _logger ?? throw new InvalidOperationException("Logger is not initialized");

    /// <inheritdoc/>
    public void Initialize()
    {
        lock (_locker)
        {
            if (_isInitialized)
            {
                return;
            }

            var configurator = new LoggerConfiguration()
                                         .MinimumLevel.Verbose()
                                         .Enrich.FromLogContext();

            Configure(configurator);

            _logger = configurator.CreateLogger();

            _isInitialized = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Disposes the Serilog logger.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>; <see langword="false"/> when called from a finalizer, in which case nothing is released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (!disposing)
            {
                return;
            }

            if (_logger is not null)
            {
                ((IDisposable)_logger).Dispose();
            }

            _disposed = true;
        }
    }
}
