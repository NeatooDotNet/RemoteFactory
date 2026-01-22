using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RemoteFactory.IntegrationTests.Logging;

/// <summary>
/// A test logger provider that captures log messages for verification in integration tests.
/// </summary>
public sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, TestLogger> _loggers = new();
    private readonly ConcurrentBag<LogMessage> _messages = new();
    private readonly LogLevel _minimumLevel;

    public TestLoggerProvider(LogLevel minimumLevel = LogLevel.Trace)
    {
        _minimumLevel = minimumLevel;
    }

    /// <summary>
    /// Gets all captured log messages.
    /// </summary>
    public IReadOnlyList<LogMessage> Messages => _messages.ToList();

    /// <summary>
    /// Gets log messages for a specific category.
    /// </summary>
    public IReadOnlyList<LogMessage> GetMessages(string categoryName)
    {
        return _messages.Where(m => m.CategoryName == categoryName).ToList();
    }

    /// <summary>
    /// Gets log messages that match a predicate.
    /// </summary>
    public IReadOnlyList<LogMessage> GetMessages(Func<LogMessage, bool> predicate)
    {
        return _messages.Where(predicate).ToList();
    }

    /// <summary>
    /// Clears all captured messages.
    /// </summary>
    public void Clear()
    {
        _messages.Clear();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new TestLogger(name, this, _minimumLevel));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }

    internal void AddMessage(LogMessage message)
    {
        _messages.Add(message);
    }
}

/// <summary>
/// A test logger that captures log messages.
/// </summary>
internal sealed class TestLogger : ILogger
{
    private readonly string _categoryName;
    private readonly TestLoggerProvider _provider;
    private readonly LogLevel _minimumLevel;

    public TestLogger(string categoryName, TestLoggerProvider provider, LogLevel minimumLevel)
    {
        _categoryName = categoryName;
        _provider = provider;
        _minimumLevel = minimumLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minimumLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = new LogMessage(
            _categoryName,
            logLevel,
            eventId,
            formatter(state, exception),
            exception);

        _provider.AddMessage(message);
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

/// <summary>
/// Represents a captured log message.
/// </summary>
public sealed record LogMessage(
    string CategoryName,
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception);

/// <summary>
/// Extension methods for TestLoggerProvider.
/// </summary>
public static class TestLoggerProviderExtensions
{
    /// <summary>
    /// Asserts that at least one message matches the predicate.
    /// </summary>
    public static void AssertLogged(
        this TestLoggerProvider provider,
        Func<LogMessage, bool> predicate,
        string? message = null)
    {
        var matches = provider.GetMessages(predicate);
        if (matches.Count == 0)
        {
            throw new Xunit.Sdk.XunitException(
                message ?? $"Expected at least one log message matching predicate. Found {provider.Messages.Count} total messages.");
        }
    }

    /// <summary>
    /// Asserts that at least one message with the given event ID was logged.
    /// </summary>
    public static void AssertLoggedEventId(
        this TestLoggerProvider provider,
        int eventId,
        string? message = null)
    {
        provider.AssertLogged(m => m.EventId.Id == eventId, message ?? $"Expected log message with event ID {eventId}");
    }

    /// <summary>
    /// Asserts that at least one message contains the given text.
    /// </summary>
    public static void AssertLoggedContains(
        this TestLoggerProvider provider,
        string text,
        string? message = null)
    {
        provider.AssertLogged(m => m.Message.Contains(text), message ?? $"Expected log message containing '{text}'");
    }

    /// <summary>
    /// Asserts that no messages match the predicate.
    /// </summary>
    public static void AssertNotLogged(
        this TestLoggerProvider provider,
        Func<LogMessage, bool> predicate,
        string? message = null)
    {
        var matches = provider.GetMessages(predicate);
        if (matches.Count > 0)
        {
            throw new Xunit.Sdk.XunitException(
                message ?? $"Expected no log messages matching predicate, but found {matches.Count}.");
        }
    }
}
