using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.TestContainers;

/// <summary>
/// One captured log call: the event id, level, exception, the two structured values
/// the phase-dispatch log methods carry (<c>Phase</c> and <c>EventType</c>), and the
/// formatted message (added PHASE-006 for count-bearing pins like 9006's discarded
/// count, which lives only in the message).
/// </summary>
internal sealed record LogEntry(int EventId, LogLevel Level, Exception? Exception, DispatchPhase? Phase, string? EventType, string Message);

/// <summary>
/// Minimal capturing logger provider for unit-tier log-emission pins (9xxx phased
/// dispatch). Extracted from <c>FactoryEventPhaseSchedulerTests</c> (PHASE-004) so
/// entry-call-scoped tests can wire it into a real DI container; PHASE-007's
/// 9002/9004/9006/9009 pins reuse it through the <see cref="Entries"/> snapshot.
/// </summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<LogEntry> _entries = [];

    /// <summary>
    /// A snapshot of what has been captured so far, safe to enumerate while logging
    /// continues. Writes take the same lock, so a test asserting over this list cannot
    /// tear against a handler still running on another thread (drains, the relay's
    /// fire-and-forget, and entry-call sweeps all log from wherever they happen to be).
    /// Returning a copy also means an assertion helper cannot mutate the capture.
    /// </summary>
    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_entries)
            {
                return [.. _entries];
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    private void Add(LogEntry entry)
    {
        lock (_entries)
        {
            _entries.Add(entry);
        }
    }

    public void Dispose() { }

    private sealed class CapturingLogger(CapturingLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            DispatchPhase? phase = null;
            string? eventType = null;
            if (state is IReadOnlyList<KeyValuePair<string, object?>> values)
            {
                foreach (var pair in values)
                {
                    if (pair.Key == "Phase" && pair.Value is DispatchPhase p)
                    {
                        phase = p;
                    }

                    if (pair.Key == "EventType" && pair.Value is string et)
                    {
                        eventType = et;
                    }
                }
            }

            owner.Add(new LogEntry(eventId.Id, logLevel, exception, phase, eventType, formatter(state, exception)));
        }
    }
}
