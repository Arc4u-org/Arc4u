namespace Arc4u.Diagnostics.Monitoring;

/// <summary>The message of the <see cref="ObsoleteAttribute"/> on the CPU and memory monitoring types.</summary>
internal static class MonitoringObsolete
{
    public const string Message = "Logging the CPU and memory usage is deprecated and will be removed in a future major version. Collect the .NET runtime and process metrics with OpenTelemetry instead (see https://arc4u-org.github.io/Arc4u/guides/diagnostics/index.html#log-cpu-and-memory-usage).";
}
