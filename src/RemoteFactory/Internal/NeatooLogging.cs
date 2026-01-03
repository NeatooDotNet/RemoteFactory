using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Ambient logger support for static methods that cannot use DI.
/// Provides logger access for components that cannot have loggers injected.
/// </summary>
public static class NeatooLogging
{
    private static ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Sets the logger factory for static method logging.
    /// Called during application startup by the NeatooLoggingStartupFilter or ConfigureLogging.
    /// </summary>
    /// <param name="factory">The logger factory to use. Can be null to disable logging.</param>
    public static void SetLoggerFactory(ILoggerFactory? factory)
    {
        _loggerFactory = factory;
    }

    /// <summary>
    /// Gets a typed logger for static methods.
    /// Returns NullLogger if no factory has been configured.
    /// </summary>
    /// <typeparam name="T">The type to create a logger for.</typeparam>
    /// <returns>An ILogger instance (never null).</returns>
    public static ILogger<T> GetLogger<T>()
    {
        return _loggerFactory?.CreateLogger<T>() ?? NullLogger<T>.Instance;
    }

    /// <summary>
    /// Gets a logger by category name for static methods.
    /// Returns NullLogger if no factory has been configured.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>An ILogger instance (never null).</returns>
    public static ILogger GetLogger(string categoryName)
    {
        return _loggerFactory?.CreateLogger(categoryName) ?? NullLoggerFactory.Instance.CreateLogger(categoryName);
    }

    /// <summary>
    /// Gets a logger for a specific category.
    /// </summary>
    /// <param name="category">The logger category from NeatooLoggerCategories.</param>
    /// <returns>An ILogger instance (never null).</returns>
    public static ILogger GetLogger(NeatooLoggerCategory category)
    {
        return GetLogger(NeatooLoggerCategories.GetCategoryName(category));
    }

    /// <summary>
    /// Configures logging for Neatoo RemoteFactory in non-ASP.NET Core applications.
    /// Call this method during application startup if not using ASP.NET Core.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to use for logging.</param>
    public static void ConfigureLogging(ILoggerFactory loggerFactory)
    {
        SetLoggerFactory(loggerFactory);
    }

    /// <summary>
    /// Indicates whether a logger factory has been configured.
    /// </summary>
    public static bool IsConfigured => _loggerFactory != null;
}
