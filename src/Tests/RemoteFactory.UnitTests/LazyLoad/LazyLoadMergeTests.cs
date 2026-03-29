using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.LazyLoad;

/// <summary>
/// Tests for <see cref="ILazyLoadDeserializable.ApplyDeserializedState"/>
/// merge pattern, verifying that deserialized state can be merged into
/// constructor-created instances while preserving the loader delegate.
/// </summary>
public class LazyLoadMergeTests
{
    /// <summary>
    /// TS-LL-018 (BR-LL-022): ApplyDeserializedState with loaded=true merges value,
    /// preserves loader. Subsequent LoadAsync returns the merged value (already loaded).
    /// </summary>
    [Fact]
    public async Task ApplyDeserializedState_Loaded_PreservesLoader()
    {
        bool loaderInvoked = false;
        var ll = new LazyLoad<string>(() =>
        {
            loaderInvoked = true;
            return Task.FromResult<string?>("from-loader");
        });

        // Cast to internal interface and apply loaded state
        var deserializable = (ILazyLoadDeserializable)ll;
        deserializable.ApplyDeserializedState("merged", true);

        // Value and IsLoaded should reflect the merged state
        Assert.Equal("merged", ll.Value);
        Assert.True(ll.IsLoaded);

        // LoadAsync should return immediately (already loaded), without invoking the loader
        var result = await ll.LoadAsync();
        Assert.Equal("merged", result);
        Assert.False(loaderInvoked);
    }

    /// <summary>
    /// TS-LL-019 (BR-LL-023): ApplyDeserializedState with loaded=false leaves
    /// instance unchanged. Loader delegate is preserved and can be invoked.
    /// </summary>
    [Fact]
    public async Task ApplyDeserializedState_Unloaded_PreservesLoader()
    {
        var ll = new LazyLoad<string>(() => Task.FromResult<string?>("loaded"));

        // Cast to internal interface and apply unloaded state
        var deserializable = (ILazyLoadDeserializable)ll;
        deserializable.ApplyDeserializedState(null, false);

        // Instance should be unchanged -- still unloaded
        Assert.Null(ll.Value);
        Assert.False(ll.IsLoaded);

        // Loader should still work
        var result = await ll.LoadAsync();
        Assert.Equal("loaded", result);
        Assert.True(ll.IsLoaded);
    }
}
