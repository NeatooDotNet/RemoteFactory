using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Runtime registry that resolves a wire-format <c>TypeFullName</c> back to the concrete
/// <see cref="FactoryEventBase"/> descendant. Populated by scanning
/// <c>AppDomain.CurrentDomain.GetAssemblies()</c> for types carrying
/// <see cref="FactoryEventAttribute"/> via inheritance.
///
/// The scan is lazy (first-use) and cached. On a <c>TypeFullName</c> miss the registry
/// rescans once (new assemblies may have loaded). Thread-safe.
/// </summary>
internal static class FactoryEventTypeRegistry
{
    private static readonly object _lock = new();
    private static Dictionary<string, Type>? _cache;
    private static int _lastScannedAssemblyCount;

    /// <summary>
    /// Resolves a <c>TypeFullName</c> to a <see cref="FactoryEventBase"/> descendant type.
    /// Returns <c>null</c> when no type is found after a fresh rescan.
    /// </summary>
    public static Type? Resolve(string typeFullName)
    {
        var cache = EnsureCache();
        if (cache.TryGetValue(typeFullName, out var type))
        {
            return type;
        }

        // Miss — rescan once to pick up dynamically-loaded assemblies.
        cache = Rescan();
        return cache.TryGetValue(typeFullName, out var rescanned) ? rescanned : null;
    }

    /// <summary>
    /// For tests: resets the cache so the next Resolve triggers a fresh scan.
    /// </summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            _cache = null;
            _lastScannedAssemblyCount = 0;
        }
    }

    /// <summary>
    /// For tests: returns a snapshot of the currently-cached TypeFullName → Type map.
    /// Triggers a scan if none has happened yet.
    /// </summary>
    internal static IReadOnlyDictionary<string, Type> Snapshot() => EnsureCache();

    private static Dictionary<string, Type> EnsureCache()
    {
        var existing = Volatile.Read(ref _cache);
        if (existing != null)
        {
            return existing;
        }

        lock (_lock)
        {
            if (_cache == null)
            {
                _cache = Scan();
                _lastScannedAssemblyCount = AppDomain.CurrentDomain.GetAssemblies().Length;
            }
            return _cache;
        }
    }

    private static Dictionary<string, Type> Rescan()
    {
        lock (_lock)
        {
            var currentCount = AppDomain.CurrentDomain.GetAssemblies().Length;
            // If nothing new loaded since the last scan, the current cache is authoritative.
            if (_cache != null && currentCount == _lastScannedAssemblyCount)
            {
                return _cache;
            }

            _cache = Scan();
            _lastScannedAssemblyCount = currentCount;
            return _cache;
        }
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "FactoryEventBase carries [DynamicallyAccessedMembers] with Inherited = true, preserving all descendants through trimming.")]
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Assembly.GetTypes() can throw a variety of load/reflection exceptions on malformed or partially-loaded assemblies; the registry must keep scanning the rest of the AppDomain rather than aborting the whole scan.")]
    private static Dictionary<string, Type> Scan()
    {
        var map = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
            }
            catch
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type == null || type.IsAbstract || !type.IsClass)
                {
                    continue;
                }
                if (type.FullName is not { } fullName)
                {
                    continue;
                }
                // Inherited-attribute lookup finds FactoryEventAttribute on FactoryEventBase
                // ancestors without requiring consumers to apply it per-type.
                var attr = type.GetCustomAttribute<FactoryEventAttribute>(inherit: true);
                if (attr == null)
                {
                    continue;
                }
                // First writer wins — later duplicate FullNames (e.g., the same type from
                // two load contexts, or two different assemblies declaring the same FullName)
                // are recorded for collision logging. TypeFullName on the wire is authoritative.
                if (map.TryGetValue(fullName, out var existing))
                {
                    if (!ReferenceEquals(existing, type))
                    {
                        NeatooLogging
                            .GetLogger(NeatooLoggerCategory.Factory)
                            .FactoryEventTypeRegistryCollision(
                                fullName,
                                existing.Assembly.GetName().Name ?? "<unknown>",
                                type.Assembly.GetName().Name ?? "<unknown>");
                    }
                    continue;
                }
                map.Add(fullName, type);
            }
        }
        return map;
    }
}
