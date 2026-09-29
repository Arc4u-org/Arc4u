namespace Arc4u.Diagnostics.Monitoring;

/// <summary>Holds the CPU usage of the current process, expressed as a percentage of the total CPU capacity.</summary>
public class CpuData
{
    /// <summary>Gets or sets the total CPU used (percentage).</summary>
    public double TotalCpuUsed { get; set; }
    /// <summary>Gets or sets the CPU used in privileged (kernel) mode (percentage).</summary>
    public double PrivilegedCpuUsed { get; set; }
    /// <summary>Gets or sets the CPU used in user mode (percentage).</summary>
    public double UserCpuUsed { get; set; }
}
