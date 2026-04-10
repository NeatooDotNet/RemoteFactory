using System.ComponentModel;
using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.LazyLoad;

/// <summary>
/// Core behavior tests for <see cref="LazyLoad{T}"/>.
/// Covers parameterless ctor, loader ctor, LoadAsync, SetValue,
/// thread safety, error handling, and INPC events.
/// </summary>
public class LazyLoadCoreTests
{
    /// <summary>
    /// TS-LL-001 (BR-LL-001): Parameterless constructor produces correct defaults.
    /// </summary>
    [Fact]
    public void ParameterlessConstructor_AllDefaultValues()
    {
        var ll = new LazyLoad<string>();

        Assert.Null(ll.Value);
        Assert.False(ll.IsLoaded);
        Assert.False(ll.IsLoading);
        Assert.False(ll.HasLoadError);
    }

    /// <summary>
    /// TS-LL-002 (BR-LL-002): Loader constructor does not invoke loader on Value read.
    /// </summary>
    [Fact]
    public void LoaderConstructor_ValueIsNull_WithoutLoad()
    {
        bool loaderInvoked = false;
        var ll = new LazyLoad<string>(() =>
        {
            loaderInvoked = true;
            return Task.FromResult<string?>("hello");
        });

        // Passive read -- should NOT trigger the loader
        var value = ll.Value;

        Assert.Null(value);
        Assert.False(loaderInvoked);
        Assert.False(ll.IsLoaded);
    }

    /// <summary>
    /// TS-LL-003 (BR-LL-003): LoadAsync invokes loader and sets Value/IsLoaded.
    /// </summary>
    [Fact]
    public async Task LoadAsync_SetsValueAndIsLoaded()
    {
        var ll = new LazyLoad<string>(() => Task.FromResult<string?>("loaded"));

        var result = await ll.LoadAsync();

        Assert.Equal("loaded", result);
        Assert.Equal("loaded", ll.Value);
        Assert.True(ll.IsLoaded);
        Assert.False(ll.IsLoading);
    }

    /// <summary>
    /// TS-LL-004 (BR-LL-004): Concurrent LoadAsync calls share a single load task.
    /// </summary>
    [Fact]
    public async Task ConcurrentLoadAsync_SingleInvocation()
    {
        int invocationCount = 0;
        var tcs = new TaskCompletionSource<string?>();

        var ll = new LazyLoad<string>(() =>
        {
            Interlocked.Increment(ref invocationCount);
            return tcs.Task;
        });

        // Launch 5 concurrent LoadAsync calls
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => ll.LoadAsync())
            .ToArray();

        // Complete the loader
        tcs.SetResult("concurrent-result");

        var results = await Task.WhenAll(tasks);

        // All tasks return the same value
        Assert.All(results, r => Assert.Equal("concurrent-result", r));
        // Loader was invoked exactly once
        Assert.Equal(1, invocationCount);
    }

    /// <summary>
    /// TS-LL-005 (BR-LL-005): LoadAsync with no loader throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task LoadAsync_NoLoader_ThrowsInvalidOperationException()
    {
        var ll = new LazyLoad<string>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => ll.LoadAsync());
    }

    /// <summary>
    /// TS-LL-006 (BR-LL-006): Loader error sets HasLoadError and LoadError.
    /// </summary>
    [Fact]
    public async Task LoadAsync_LoaderThrows_SetsHasLoadError()
    {
        var ll = new LazyLoad<string>(() => throw new Exception("fail"));

        await Assert.ThrowsAsync<Exception>(() => ll.LoadAsync());

        Assert.True(ll.HasLoadError);
        Assert.Equal("fail", ll.LoadError);
    }

    /// <summary>
    /// TS-LL-007 (BR-LL-007): SetValue sets value, clears errors, fires INPC.
    /// </summary>
    [Fact]
    public void SetValue_SetsValueAndClearsErrors_FiresPropertyChanged()
    {
        var ll = new LazyLoad<string>(() => Task.FromResult<string?>("unused"));

        var changedProperties = new List<string>();
        ll.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        ll.SetValue("direct");

        Assert.Equal("direct", ll.Value);
        Assert.True(ll.IsLoaded);
        Assert.False(ll.HasLoadError);
        Assert.Contains("Value", changedProperties);
        Assert.Contains("IsLoaded", changedProperties);
        Assert.Contains("HasLoadError", changedProperties);
    }

    /// <summary>
    /// TS-LL-008 (BR-LL-008): Pre-loaded constructor sets Value and IsLoaded.
    /// </summary>
    [Fact]
    public void PreLoadedConstructor_ValueAndIsLoaded()
    {
        var ll = new LazyLoad<string>("preloaded");

        Assert.Equal("preloaded", ll.Value);
        Assert.True(ll.IsLoaded);
    }

    /// <summary>
    /// TS-LL-009 (BR-LL-003, BR-LL-009): PropertyChanged fires on LoadAsync completion.
    /// </summary>
    [Fact]
    public async Task LoadAsync_FiresPropertyChanged()
    {
        var ll = new LazyLoad<string>(() => Task.FromResult<string?>("loaded"));

        var changedProperties = new List<string>();
        ll.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        await ll.LoadAsync();

        Assert.Contains("Value", changedProperties);
        Assert.Contains("IsLoaded", changedProperties);
    }

    /// <summary>
    /// TS-LL-010 (BR-LL-010): Inner value INPC events are forwarded.
    /// </summary>
    [Fact]
    public void InnerValue_PropertyChanged_Forwarded()
    {
        var mock = new MockNotifyPropertyChanged();
        var ll = new LazyLoad<MockNotifyPropertyChanged>(mock);

        var forwardedProperties = new List<string>();
        ll.PropertyChanged += (_, e) => forwardedProperties.Add(e.PropertyName!);

        // Raise PropertyChanged on the inner value
        mock.RaisePropertyChanged("SomeProperty");

        Assert.Contains("SomeProperty", forwardedProperties);
    }

    /// <summary>
    /// Helper class implementing INotifyPropertyChanged for INPC forwarding tests.
    /// </summary>
    public class MockNotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
