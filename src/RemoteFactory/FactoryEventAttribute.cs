using System;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Marks a class as a factory event. Applied to <see cref="FactoryEventBase"/> with
/// <c>Inherited = true</c>; every descendant inherits it automatically.
///
/// <see cref="FactoryEventTypeRegistry"/> scans loaded assemblies for types carrying
/// this attribute (via <c>GetCustomAttribute&lt;FactoryEventAttribute&gt;(inherit: true)</c>)
/// to resolve wire-format <c>TypeFullName</c> values back to CLR types during relay
/// deserialization.
///
/// Consumers do not apply this attribute directly — inheriting <see cref="FactoryEventBase"/>
/// is sufficient.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class FactoryEventAttribute : Attribute
{
}
