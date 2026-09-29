
namespace Arc4u.Diagnostics;
/// <summary>Allows a logger to receive the name of the member that is emitting a log entry.</summary>
public interface ILoggerCallerMember
{
    /// <summary>Sets the name of the calling member that is written in the <see cref="LoggingConstants.MethodName"/> property of the next log entries.</summary>
    /// <param name="caller">The name of the calling method or property.</param>
    void CallerMemberName(string caller);
}
