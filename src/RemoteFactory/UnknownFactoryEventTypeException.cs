using System;
using System.Collections.Generic;
using System.Linq;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Thrown when the client receives a relayed factory event whose <c>TypeFullName</c>
/// cannot be resolved to a loaded <see cref="FactoryEventBase"/> descendant.
/// Indicates a server/client version mismatch or a missing domain assembly on the client.
///
/// Caught inside the relay dispatch isolation block and logged; never propagates to
/// the factory caller.
/// </summary>
public sealed class UnknownFactoryEventTypeException : Exception
{
    public UnknownFactoryEventTypeException()
        : base("Unknown factory event type.")
    {
        UnresolvedTypeFullName = string.Empty;
        BatchTypeFullNames = Array.Empty<string>();
    }

    public UnknownFactoryEventTypeException(string message)
        : base(message)
    {
        UnresolvedTypeFullName = string.Empty;
        BatchTypeFullNames = Array.Empty<string>();
    }

    public UnknownFactoryEventTypeException(string message, Exception innerException)
        : base(message, innerException)
    {
        UnresolvedTypeFullName = string.Empty;
        BatchTypeFullNames = Array.Empty<string>();
    }

    public UnknownFactoryEventTypeException(string typeFullName, IEnumerable<string> batchTypeFullNames)
        : base(BuildMessage(typeFullName, batchTypeFullNames))
    {
        UnresolvedTypeFullName = typeFullName;
        BatchTypeFullNames = batchTypeFullNames.ToArray();
    }

    /// <summary>The wire TypeFullName that could not be resolved.</summary>
    public string UnresolvedTypeFullName { get; }

    /// <summary>All TypeFullName values present in the batch, for diagnostic context.</summary>
    public IReadOnlyList<string> BatchTypeFullNames { get; }

    private static string BuildMessage(string typeFullName, IEnumerable<string> batch)
    {
        var batchList = string.Join(", ", batch);
        return $"Unknown factory event type '{typeFullName}'. No loaded assembly contains a FactoryEventBase descendant with this FullName. Batch contained: [{batchList}].";
    }
}
