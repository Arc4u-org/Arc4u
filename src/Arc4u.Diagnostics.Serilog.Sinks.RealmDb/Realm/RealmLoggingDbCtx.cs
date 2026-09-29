using Microsoft.Extensions.Logging;
using Realms;

namespace Arc4u.Diagnostics.Serilog.Sinks.RealmDb;

/// <summary>An <see cref="ILogStore"/> that reads the log messages persisted in a Realm database, the most recent first.</summary>
public class RealmLoggingDbCtx : ILogStore
{
    /// <summary>Initializes a new instance of the <see cref="RealmLoggingDbCtx"/> class and opens the Realm database.</summary>
    /// <param name="config">The configuration of the Realm database.</param>
    /// <param name="loggerFactory">The logger factory (not used).</param>
    public RealmLoggingDbCtx(RealmConfiguration config, ILoggerFactory loggerFactory)
    {
        _realm = Realm.GetInstance(config);
    }

    /// <inheritdoc/>
    public void RemoveAll()
    {
        _realm.Write(_realm.RemoveAll<LogDBMessage>);
    }

    /// <summary>
    /// Gets a page of the messages ordered by descending timestamp. <paramref name="skip"/> entries are skipped first;
    /// then at most <paramref name="take"/> entries are examined and, when a criteria is given, only those whose text contains it (case-insensitive) are returned.
    /// </summary>
    /// <param name="criteria">Text that the message must contain. When empty, no filter is applied.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to examine.</param>
    /// <returns>The matching log messages.</returns>
    public List<LogMessage> GetLogs(string criteria, int skip, int take)
    {
        var hasCriteria = !string.IsNullOrWhiteSpace(criteria);
        var searchText = hasCriteria ? criteria.ToLowerInvariant() : "";

        var queryable = _realm.All<LogDBMessage>().OrderByDescending(msg => msg.Timestamp);

        var enumerator = queryable.GetEnumerator();

        var i = 0;
        while (i < skip && enumerator.MoveNext())
        {
            i++;
        }

        var result = new List<LogDBMessage>(take);
        i = 0;

        while (i < take && enumerator.MoveNext())
        {
            if (hasCriteria && enumerator.Current.Message.ToLowerInvariant().Contains(searchText))
            {
                result.Add(enumerator.Current);
            }

            if (!hasCriteria)
            {
                result.Add(enumerator.Current);
            }

            i++;
        }

        // return the list<LogMessage> based on the list<LogDBMessage>.
        var mapped = result.Select(db => new LogMessage
        {
            Message = db.Message,
            MessageCategory = ((MessageCategory)db.MessageCategory).ToString(),
            MessageType = ((LogLevel)db.MessageType).ToString(),
            Timestamp = db.Timestamp,
            ActivityId = db.ActivityId,
            Application = db.Application,
            Identity = db.Identity,
            ClassType = db.ClassType,
            MethodName = db.MethodName,
            ProcessId = db.ProcessId,
            ThreadId = db.ThreadId,
            Stacktrace = db.Stacktrace,
            Properties = db.Properties
        }).ToList();

        return mapped;
    }

    private readonly Realm _realm;
    private readonly ILoggerFactory _loggerFactory;
    /// <summary>Gets the opened Realm database.</summary>
    public Realm Realm { get { return _realm; } }
}
