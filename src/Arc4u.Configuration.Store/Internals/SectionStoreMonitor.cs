using Arc4u.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arc4u.Configuration.Store.Internals;

/// <remarks>
/// Logs through the plain <see cref="ILogger"/> API on purpose (no <c>Technical()</c>): the service must work whether or not
/// <c>AddILogger()</c> was called. When it was, the Arc4u logger writes these entries in the Technical category by default.
/// </remarks>
sealed class SectionStoreMonitor : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SectionStoreMonitor> _logger;
    private readonly TimeSpan _pollingInterval;

    public SectionStoreMonitor(TimeSpan pollingInterval, IServiceScopeFactory serviceScopeFactory, ILogger<SectionStoreMonitor> logger)
    {
        _pollingInterval = pollingInterval;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogServiceStarting(nameof(SectionStoreMonitor));
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogServiceStarted(nameof(SectionStoreMonitor));
        try
        {
            do
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var sectionStoreService = scope.ServiceProvider.GetRequiredService<ISectionStoreService>();
                    sectionStoreService.ReloadIfNeeded();
                }
                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    _logger.LogException(e);
                }
                await Task.Delay(_pollingInterval, stoppingToken).ConfigureAwait(false);
            }
            while (!stoppingToken.IsCancellationRequested);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // do nothing, the service is stopping
        }
        catch (Exception e)
        {
            _logger.LogException(e);
        }
        _logger.LogServiceStopped(nameof(SectionStoreMonitor));
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogServiceStopping(nameof(SectionStoreMonitor));
        return base.StopAsync(cancellationToken);
    }
}
