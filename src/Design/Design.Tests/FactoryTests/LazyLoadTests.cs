// =============================================================================
// DESIGN SOURCE OF TRUTH: LazyLoad<T> Property Tests
// =============================================================================
//
// Tests demonstrating the LazyLoad<T> property pattern on [Factory] classes.
// These tests validate that LazyLoad properties work correctly across all
// container modes and that state is preserved through serialization.
//
// =============================================================================

using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for LazyLoad&lt;T&gt; property pattern.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Test both loaded and unloaded states
///
/// LazyLoad&lt;T&gt; has two serializable states:
/// - Unloaded: Value=null, IsLoaded=false (deferred loading)
/// - Loaded: Value=data, IsLoaded=true (pre-loaded or post-LoadAsync)
///
/// Both states must survive serialization round-trips. The loader delegate
/// is NOT serialized (delegates reference server-side services), so only
/// Value and IsLoaded cross the wire.
/// </remarks>
public class LazyLoadTests
{
    /// <summary>
    /// Verifies that a [Factory] class with a LazyLoad property can be created
    /// in local mode with the lazy property in unloaded state.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: LazyLoad starts unloaded after Create
    ///
    /// The Create method sets up the LazyLoad with a loader delegate but
    /// does NOT call LoadAsync(). The property is ready for on-demand loading.
    /// </remarks>
    [Fact]
    public async Task Create_InLocalMode_LazyPropertyIsUnloaded()
    {
        // Arrange
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IProductWithReviewsFactory>();

        // Act
        var product = await factory.Create("Test Product", 19.99m);

        // Assert - LazyLoad is configured but not yet loaded
        Assert.NotNull(product);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(19.99m, product.Price);
        Assert.NotNull(product.Reviews);
        Assert.Null(product.Reviews.Value);
        Assert.False(product.Reviews.IsLoaded);

        local.Dispose();
    }

    /// <summary>
    /// Verifies that LoadAsync() triggers the loader and sets Value.
    /// </summary>
    [Fact]
    public async Task Create_ThenLoadAsync_LoadsValue()
    {
        // Arrange
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IProductWithReviewsFactory>();
        var product = await factory.Create("Test Product", 19.99m);

        // Act - explicitly trigger lazy loading
        var reviews = await product.Reviews.LoadAsync();

        // Assert - Value is now loaded
        Assert.NotNull(reviews);
        Assert.True(product.Reviews.IsLoaded);
        Assert.Contains("Reviews for product", product.Reviews.Value, StringComparison.Ordinal);

        local.Dispose();
    }

    /// <summary>
    /// Verifies that Fetch returns an object with an unloaded LazyLoad property,
    /// and the unloaded state survives serialization across the client/server boundary.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Unloaded LazyLoad survives client-server round-trip
    ///
    /// When the server sends an object with LazyLoad.IsLoaded=false:
    /// 1. Ordinal serializer writes [null, false] for the two LazyLoad slots
    /// 2. Client deserializes, generating code creates new LazyLoad()
    /// 3. Client-side Reviews.Value == null, Reviews.IsLoaded == false
    ///
    /// The loader delegate is NOT preserved across the wire, but the
    /// constructor-initialization pattern re-creates it on the next server call.
    /// </remarks>
    [Fact]
    public async Task Fetch_UnloadedLazyProperty_SurvivesClientServerRoundTrip()
    {
        // Arrange - use client mode to force serialization
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IProductWithReviewsFactory>();

        // Act - Fetch from server with unloaded LazyLoad
        var product = await factory.Fetch(42);

        // Assert - unloaded state preserved through serialization
        Assert.NotNull(product);
        Assert.Equal(42, product.Id);
        Assert.Equal("Product_42", product.Name);
        Assert.Equal(29.99m, product.Price);
        Assert.NotNull(product.Reviews);
        Assert.Null(product.Reviews.Value);
        Assert.False(product.Reviews.IsLoaded);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that a pre-loaded LazyLoad property survives serialization
    /// across the client/server boundary with its value intact.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Loaded LazyLoad preserves Value across the wire
    ///
    /// When the server sends an object with LazyLoad.IsLoaded=true:
    /// 1. Ordinal serializer writes [value, true] for the two LazyLoad slots
    /// 2. Client deserializes, generated code creates new LazyLoad(value)
    /// 3. Client-side Reviews.Value == the server's value, Reviews.IsLoaded == true
    /// </remarks>
    [Fact]
    public async Task FetchWithReviews_LoadedLazyProperty_SurvivesClientServerRoundTrip()
    {
        // Arrange - use client mode to force serialization
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IProductWithReviewsFactory>();

        // Act - Fetch from server with pre-loaded reviews
        var product = await factory.FetchWithReviews(42, true);

        // Assert - loaded state and value preserved through serialization
        Assert.NotNull(product);
        Assert.Equal(42, product.Id);
        Assert.Equal("Product_42", product.Name);
        Assert.NotNull(product.Reviews);
        Assert.True(product.Reviews.IsLoaded);
        Assert.Contains("Reviews for product 42", product.Reviews.Value, StringComparison.Ordinal);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies the FetchWithReviews method with includeReviews=false returns
    /// an unloaded LazyLoad (same as regular Fetch).
    /// </summary>
    [Fact]
    public async Task FetchWithReviews_NotIncluded_LazyPropertyIsUnloaded()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IProductWithReviewsFactory>();

        // Act - Fetch without reviews
        var product = await factory.FetchWithReviews(42, false);

        // Assert - LazyLoad is unloaded
        Assert.NotNull(product);
        Assert.NotNull(product.Reviews);
        Assert.Null(product.Reviews.Value);
        Assert.False(product.Reviews.IsLoaded);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that SetValue() directly sets the LazyLoad value without
    /// requiring a loader delegate.
    /// </summary>
    [Fact]
    public async Task SetValue_DirectlyLoadsLazyProperty()
    {
        // Arrange
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IProductWithReviewsFactory>();
        var product = await factory.Fetch(1);

        // Act - directly set the value (bypasses loader)
        product.Reviews.SetValue("Manually set reviews");

        // Assert
        Assert.True(product.Reviews.IsLoaded);
        Assert.Equal("Manually set reviews", product.Reviews.Value);

        local.Dispose();
    }
}
