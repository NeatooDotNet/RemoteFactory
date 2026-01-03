namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Logger category enumeration for Neatoo RemoteFactory.
/// Used to organize and filter log messages.
/// </summary>
public enum NeatooLoggerCategory
{
    /// <summary>
    /// General library logs.
    /// </summary>
    General,

    /// <summary>
    /// Serialization operations (JSON, ordinal conversion).
    /// </summary>
    Serialization,

    /// <summary>
    /// Factory operations (Create, Fetch, Insert, Update, Delete).
    /// </summary>
    Factory,

    /// <summary>
    /// Remote call operations (client-side HTTP calls).
    /// </summary>
    Remote,

    /// <summary>
    /// Authorization checks.
    /// </summary>
    Authorization,

    /// <summary>
    /// Server-side request handling.
    /// </summary>
    Server
}

/// <summary>
/// Logger category name constants for Neatoo RemoteFactory.
/// Use these for filtering in appsettings.json or logging configuration.
/// </summary>
public static class NeatooLoggerCategories
{
    /// <summary>
    /// Base category for all Neatoo RemoteFactory logs.
    /// </summary>
    public const string Base = "Neatoo.RemoteFactory";

    /// <summary>
    /// Category for serialization operations.
    /// </summary>
    public const string Serialization = "Neatoo.RemoteFactory.Serialization";

    /// <summary>
    /// Category for factory operations.
    /// </summary>
    public const string Factory = "Neatoo.RemoteFactory.Factory";

    /// <summary>
    /// Category for remote call operations.
    /// </summary>
    public const string Remote = "Neatoo.RemoteFactory.Remote";

    /// <summary>
    /// Category for authorization checks.
    /// </summary>
    public const string Authorization = "Neatoo.RemoteFactory.Authorization";

    /// <summary>
    /// Category for server-side request handling.
    /// </summary>
    public const string Server = "Neatoo.RemoteFactory.Server";

    /// <summary>
    /// Gets the category name for a NeatooLoggerCategory.
    /// </summary>
    /// <param name="category">The category to get the name for.</param>
    /// <returns>The category name string.</returns>
    public static string GetCategoryName(NeatooLoggerCategory category)
    {
        return category switch
        {
            NeatooLoggerCategory.Serialization => Serialization,
            NeatooLoggerCategory.Factory => Factory,
            NeatooLoggerCategory.Remote => Remote,
            NeatooLoggerCategory.Authorization => Authorization,
            NeatooLoggerCategory.Server => Server,
            _ => Base
        };
    }
}
