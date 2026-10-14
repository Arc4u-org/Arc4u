using Microsoft.Extensions.Logging;
using Realms;

namespace Arc4u.Diagnostics.Serilog.Sinks.RealmDb;

/// <summary>An <see cref="ILogStore"/> that reads the log messages persisted in a Realm database, the most recent first.</summary>
/// <remarks>
/// A Realm instance can only be used on the thread that opened it, so each call opens its own instance and closes it before returning.
/// The store can therefore be registered as a singleton and called from any thread, including under a synchronization context that is not bound to one thread (Blazor Server, xUnit).
/// </remarks>
public class RealmLoggingDbCtx : ILogStore
{
    /// <summary>Initializes a new instance of the <see cref="RealmLoggingDbCtx"/> class.</summary>
    /// <param name="config">The configuration of the Realm database.</param>
    public RealmLoggingDbCtx(RealmConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        _config = config;
    }

    private readonly RealmConfiguration _config;

    // Realm binds an instance to the current synchronization context and expects that context to always run on the opening thread.
    // Contexts such as the Blazor Server renderer or xUnit move between threads, so the instance is opened, used and closed without one:
    // the store reads and writes synchronously and does not need notifications.
    private T UseRealm<T>(Func<Realm, T> operation)
    {
        var current = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            using var realm = Realm.GetInstance(_config);
            return operation(realm);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(current);
        }
    }

    /// <inheritdoc/>
    public void RemoveAll()
    {
        UseRealm(realm =>
        {
            realm.Write(realm.RemoveAll<LogDBMessage>);
            return true;
        });
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

        return UseRealm(realm => GetLogs(realm, hasCriteria, searchText, skip, take));
    }

    private static List<LogMessage> GetLogs(Realm realm, bool hasCriteria, string searchText, int skip, int take)
    {
        var queryable = realm.All<LogDBMessage>().OrderByDescending(msg => msg.Timestamp);

        using var enumerator = queryable.GetEnumerator();

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
        // The mapping must happen before the Realm is closed: the LogDBMessage objects are live views on the database.
        var mapped = result.Select(db => new LogMessage
        {
            Message = db.Message,
            MessageCategory = MessageCategoryRegistry.GetName((MessageCategory)db.MessageCategory),
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
}
