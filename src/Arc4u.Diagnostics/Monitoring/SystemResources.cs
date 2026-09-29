using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arc4u.Diagnostics.Monitoring;

/// <summary>
/// Monitor and log the information about the Process Cpu and Memory usage!
/// </summary>
public sealed class SystemResources : IHostedService, IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="SystemResources"/> class.</summary>
    /// <param name="logger">The logger used to write the monitoring entries.</param>
    /// <param name="internalPeriodInSeconds">The interval, in seconds, between two measures. Must be greater than 0.</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="internalPeriodInSeconds"/> is 0.</exception>
    public SystemResources(ILogger<SystemResources> logger, uint internalPeriodInSeconds = 10)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (0 == internalPeriodInSeconds)
        {
            throw new ArgumentException("The interval must be bigger than 0!");
        }

        interval = internalPeriodInSeconds;
        _lastTimeStamp = _process.StartTime.ToUniversalTime();
        _logger = logger;
    }

    private readonly uint interval;
    private readonly ILogger<SystemResources> _logger;

    /// <summary>Starts the timer that collects and logs the CPU and memory usage, after <see cref="StartMonitoringDelayInSeconds"/> seconds and then every interval.</summary>
    /// <param name="cancellationToken">Not used.</param>
    /// <returns>A completed task.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Scheduler = new Timer(CollectData, null, TimeSpan.FromSeconds(StartMonitoringDelayInSeconds), TimeSpan.FromSeconds(interval));

        return Task.CompletedTask;
    }

    private Process _process = Process.GetCurrentProcess();
    private DateTime _lastTimeStamp;
    private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
    private TimeSpan _lastUserProcessorTime = TimeSpan.Zero;
    private TimeSpan _lastPrivilegedProcessorTime = TimeSpan.Zero;

    private readonly CpuData _cpuData = new CpuData();

    private void CollectData(object? state)
    {
        _process = Process.GetCurrentProcess();

        var totalCpuTimeUsed = _process.TotalProcessorTime.TotalMilliseconds - _lastTotalProcessorTime.TotalMilliseconds;
        var privilegedCpuTimeUsed = _process.PrivilegedProcessorTime.TotalMilliseconds - _lastPrivilegedProcessorTime.TotalMilliseconds;
        var userCpuTimeUsed = _process.UserProcessorTime.TotalMilliseconds - _lastUserProcessorTime.TotalMilliseconds;

        _lastTotalProcessorTime = _process.TotalProcessorTime;
        _lastPrivilegedProcessorTime = _process.PrivilegedProcessorTime;
        _lastUserProcessorTime = _process.UserProcessorTime;

        var cpuTimeElapsed = (DateTime.UtcNow - _lastTimeStamp).TotalMilliseconds * Environment.ProcessorCount;
        _lastTimeStamp = DateTime.UtcNow;

        _cpuData.TotalCpuUsed = totalCpuTimeUsed * 100 / cpuTimeElapsed;
        _cpuData.PrivilegedCpuUsed = privilegedCpuTimeUsed * 100 / cpuTimeElapsed;
        _cpuData.UserCpuUsed = userCpuTimeUsed * 100 / cpuTimeElapsed;

        _logger.Monitoring()
               .Add("TotalCpuUsed", _cpuData.TotalCpuUsed)
               .Add("PrivilegedCpuUsed", _cpuData.PrivilegedCpuUsed)
               .Add("UserCpuUsed", _cpuData.UserCpuUsed)
               .Add("WorkingSet", _process.WorkingSet64)
               .Add("NonPagedSystemMemory", _process.NonpagedSystemMemorySize64)
               .Add("PagedMemory", _process.PagedMemorySize64)
               .Add("PagedSystemMemory", _process.PagedSystemMemorySize64)
               .Add("PrivateMemory", _process.PrivateMemorySize64)
               .Add("VirtualMemoryMemory", _process.VirtualMemorySize64)
               .LogMonitoring();
    }

    /// <summary>Stops the timer.</summary>
    /// <param name="cancellationToken">Not used.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Scheduler?.Change(Timeout.Infinite, Timeout.Infinite);

        return Task.CompletedTask;

    }

    private Timer? Scheduler { get; set; }

    /// <summary>
    /// Define the delay the system will wait before starting the monitoring.
    /// Default is 10.
    /// </summary>
    public uint StartMonitoringDelayInSeconds { get; set; } = 10;

    /// <summary>Releases the timer.</summary>
    public void Dispose()
    {
        Scheduler?.Dispose();
    }
}
