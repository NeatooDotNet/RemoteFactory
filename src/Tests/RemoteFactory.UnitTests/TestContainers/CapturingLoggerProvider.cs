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
/// entry-call-scoped tests can wire it into a real DI container;
/// PHASE-007's queued 9002/9004/9006 pins reuse it.
/// </summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public List<LogEntry> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

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

            lock (owner.Entries)
            {
                owner.Entries.Add(new LogEntry(eventId.Id, logLevel, exception, phase, eventType, formatter(state, exception)));
            }
        }
    }
}
