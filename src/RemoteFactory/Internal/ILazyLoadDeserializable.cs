namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Non-generic internal interface for applying deserialized state to a LazyLoad instance
/// without using reflection. Used by LazyLoadJsonConverter during deserialization
/// to merge server-side state into constructor-created instances, preserving loader delegates.
/// </summary>
internal interface ILazyLoadDeserializable
{
    bool IsLoaded { get; }
    object? BoxedValue { get; }
    void ApplyDeserializedState(object? value, bool isLoaded);
}
