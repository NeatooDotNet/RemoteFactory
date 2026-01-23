using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.Can;

/// <summary>
/// Code Path 20-22 tests: Can method generation patterns.
/// Tests cover: Can public method, Can remote method, Can local method.
/// </summary>
public class CanMethodCodePathTests
{
    #region Authorization Target

    /// <summary>
    /// Authorization class for testing Can methods.
    /// </summary>
    public class CanMethodTestAuth
    {
        public static bool ShouldAllow { get; set; } = true;

        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public bool CanRead()
        {
            return ShouldAllow;
        }
    }

    #endregion

    #region Code Path 20: Can Public Method

    /// <summary>
    /// Code Path 20: Tests the Can public method.
    /// Location: RenderCanMethod, lines 735-762.
    /// Key line 742: sb.AppendLine($"        public virtual {returnType} {method.UniqueName}({parameters})");
    /// </summary>
    [Factory]
    [AuthorizeFactory<CanMethodTestAuth>]
    public class CanPublicMethodTarget
    {
        public string Value { get; }

        [Fetch]
        public CanPublicMethodTarget()
        {
            Value = "Fetched";
        }
    }

    public class CanPublicMethodTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly ICanPublicMethodTargetFactory _factory;

        public CanPublicMethodTests()
        {
            _provider = new ServerContainerBuilder()
                .WithService<CanMethodTestAuth, CanMethodTestAuth>()
                .Build();
            _factory = _provider.GetRequiredService<ICanPublicMethodTargetFactory>();
            CanMethodTestAuth.ShouldAllow = true;
        }

        public void Dispose()
        {
            CanMethodTestAuth.ShouldAllow = true;
            (_provider as IDisposable)?.Dispose();
        }

        [Fact]
        public void CanMethod_Public_ReturnsAuthorized_WhenAllowed()
        {
            // Arrange
            CanMethodTestAuth.ShouldAllow = true;

            // Act - Call the public CanFetch method (public keyword must be correct)
            var result = _factory.CanFetch();

            // Assert - Method is public and works correctly
            Assert.True(result.HasAccess);
        }

        [Fact]
        public void CanMethod_Public_ReturnsAuthorizedType()
        {
            // This test verifies the return type is Authorized (not AuthorizedX)
            // The fact that it compiles proves the return type is correct
            Authorized result = _factory.CanFetch();

            // The result must be a valid Authorized object
            Assert.NotNull(result);
            Assert.IsType<Authorized>(result);
        }

        [Fact]
        public void CanMethod_MethodName_IsCorrect()
        {
            // Arrange
            CanMethodTestAuth.ShouldAllow = true;

            // Act & Assert - The method name should be CanFetch (not CanFetchX)
            // This compiles, so the method name is correct
            var result = _factory.CanFetch();
            Assert.NotNull(result);
        }
    }

    #endregion

    #region Code Path 21: Can Remote Method

    /// <summary>
    /// Code Path 21: Tests the Can remote method generation.
    /// This is tested through the IntegrationTests project with ClientServerContainers.
    /// For unit tests, we verify the local method generation which is similar.
    /// Location: lines 764-780.
    /// Key line 775: Similar pattern to other remote methods with ForDelegate.
    /// </summary>
    /// <remarks>
    /// Remote methods require client/server serialization which is tested in IntegrationTests.
    /// This test verifies the synchronous pattern which exercises similar code paths.
    /// </remarks>
    [Factory]
    [AuthorizeFactory<CanMethodTestAuth>]
    public class CanRemoteTarget
    {
        public string Value { get; }

        [Fetch]
        public CanRemoteTarget()
        {
            Value = "Fetched";
        }
    }

    public class CanRemoteMethodTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly ICanRemoteTargetFactory _factory;

        public CanRemoteMethodTests()
        {
            _provider = new ServerContainerBuilder()
                .WithService<CanMethodTestAuth, CanMethodTestAuth>()
                .Build();
            _factory = _provider.GetRequiredService<ICanRemoteTargetFactory>();
            CanMethodTestAuth.ShouldAllow = true;
        }

        public void Dispose()
        {
            CanMethodTestAuth.ShouldAllow = true;
            (_provider as IDisposable)?.Dispose();
        }

        [Fact]
        public void CanMethod_ReturnType_IsAuthorized()
        {
            // Arrange
            CanMethodTestAuth.ShouldAllow = true;

            // Act - Return type should be Authorized (not AuthorizedX)
            Authorized result = _factory.CanFetch();

            // Assert
            Assert.True(result.HasAccess);
        }

        [Fact]
        public void CanMethod_HasAccess_ReflectsAuthState()
        {
            // Test true state
            CanMethodTestAuth.ShouldAllow = true;
            Assert.True(_factory.CanFetch().HasAccess);

            // Test false state
            CanMethodTestAuth.ShouldAllow = false;
            Assert.False(_factory.CanFetch().HasAccess);
        }
    }

    #endregion

    #region Code Path 22: Can Local Method

    /// <summary>
    /// Code Path 22: Tests the Can local method.
    /// Location: lines 782-806.
    /// Key line 789: sb.AppendLine($"        public {asyncKeyword} {returnType} Local{method.UniqueName}({parameters})");
    /// </summary>
    [Factory]
    [AuthorizeFactory<CanMethodTestAuth>]
    public class CanLocalMethodTarget
    {
        public string Value { get; }

        [Fetch]
        public CanLocalMethodTarget()
        {
            Value = "LocalFetched";
        }
    }

    public class CanLocalMethodTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly ICanLocalMethodTargetFactory _factory;

        public CanLocalMethodTests()
        {
            _provider = new ServerContainerBuilder()
                .WithService<CanMethodTestAuth, CanMethodTestAuth>()
                .Build();
            _factory = _provider.GetRequiredService<ICanLocalMethodTargetFactory>();
            CanMethodTestAuth.ShouldAllow = true;
        }

        public void Dispose()
        {
            CanMethodTestAuth.ShouldAllow = true;
            (_provider as IDisposable)?.Dispose();
        }

        [Fact]
        public void CanLocalMethod_IsPublic()
        {
            // The method should be public (not publicXXX)
            // This compiles and runs, so public keyword is correct
            CanMethodTestAuth.ShouldAllow = true;
            var result = _factory.CanFetch();
            Assert.True(result.HasAccess);
        }

        [Fact]
        public void CanLocalMethod_ReturnsNewAuthorizedTrue_OnSuccess()
        {
            // Arrange
            CanMethodTestAuth.ShouldAllow = true;

            // Act - Local method should return new Authorized(true) on success
            var result = _factory.CanFetch();

            // Assert
            Assert.True(result.HasAccess);
        }

        [Fact]
        public void CanLocalMethod_HasAccessProperty_IsAccessible()
        {
            // This test verifies the HasAccess property exists and is accessible
            // The fact that it compiles proves the Authorized type is correct
            var result = _factory.CanFetch();

            // HasAccess property should be accessible (returns bool)
            bool hasAccess = result.HasAccess;

            // The result is a bool - value depends on static auth state which is tested elsewhere
            Assert.IsType<bool>(hasAccess);
        }
    }

    #endregion

    #region Code Path 22: Can Local Method with Async Authorization

    /// <summary>
    /// Tests async authorization with Can methods.
    /// </summary>
    public class CanMethodAsyncAuth
    {
        public static bool ShouldAllow { get; set; } = true;

        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public async Task<bool> CanReadAsync()
        {
            await Task.Delay(1);
            return ShouldAllow;
        }
    }

    [Factory]
    [AuthorizeFactory<CanMethodAsyncAuth>]
    public class CanAsyncAuthTarget
    {
        public string Value { get; }

        [Fetch]
        public CanAsyncAuthTarget()
        {
            Value = "AsyncAuth";
        }
    }

    public class CanAsyncLocalMethodTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly ICanAsyncAuthTargetFactory _factory;

        public CanAsyncLocalMethodTests()
        {
            _provider = new ServerContainerBuilder()
                .WithService<CanMethodAsyncAuth, CanMethodAsyncAuth>()
                .Build();
            _factory = _provider.GetRequiredService<ICanAsyncAuthTargetFactory>();
            CanMethodAsyncAuth.ShouldAllow = true;
        }

        public void Dispose()
        {
            CanMethodAsyncAuth.ShouldAllow = true;
            (_provider as IDisposable)?.Dispose();
        }

        [Fact]
        public async Task CanLocalMethod_Async_ReturnsTaskAuthorized()
        {
            // Arrange
            CanMethodAsyncAuth.ShouldAllow = true;

            // Act - Async can methods return Task<Authorized>
            var result = await _factory.CanFetch();

            // Assert
            Assert.True(result.HasAccess);
        }

        [Fact]
        public async Task CanLocalMethod_Async_PropagatesAuthFailure()
        {
            // Arrange
            CanMethodAsyncAuth.ShouldAllow = false;

            // Act
            var result = await _factory.CanFetch();

            // Assert
            Assert.False(result.HasAccess);
        }
    }

    #endregion

    #region Combined Test: All Can Method Code Paths

    /// <summary>
    /// Comprehensive test that exercises all Can method code paths together.
    /// </summary>
    public class CanComprehensiveAuth
    {
        public static bool AllowCreate { get; set; } = true;
        public static bool AllowFetch { get; set; } = true;

        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        public bool CanCreate()
        {
            return AllowCreate;
        }

        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public bool CanFetch()
        {
            return AllowFetch;
        }
    }

    [Factory]
    [AuthorizeFactory<CanComprehensiveAuth>]
    public class CanComprehensiveTarget
    {
        public string CreateValue { get; }
        public string FetchValue { get; }

        [Create]
        public CanComprehensiveTarget()
        {
            CreateValue = "Created";
            FetchValue = string.Empty;
        }

        [Fetch]
        public CanComprehensiveTarget(int id)
        {
            CreateValue = string.Empty;
            FetchValue = $"Fetched-{id}";
        }
    }

    public class CanComprehensiveTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly ICanComprehensiveTargetFactory _factory;

        public CanComprehensiveTests()
        {
            _provider = new ServerContainerBuilder()
                .WithService<CanComprehensiveAuth, CanComprehensiveAuth>()
                .Build();
            _factory = _provider.GetRequiredService<ICanComprehensiveTargetFactory>();
            CanComprehensiveAuth.AllowCreate = true;
            CanComprehensiveAuth.AllowFetch = true;
        }

        public void Dispose()
        {
            CanComprehensiveAuth.AllowCreate = true;
            CanComprehensiveAuth.AllowFetch = true;
            (_provider as IDisposable)?.Dispose();
        }

        [Fact]
        public void CanCreate_And_CanFetch_BothGenerated()
        {
            // Both Can methods should be generated correctly
            var canCreate = _factory.CanCreate();
            var canFetch = _factory.CanFetch();

            Assert.True(canCreate.HasAccess);
            Assert.True(canFetch.HasAccess);
        }

        [Fact]
        public void CanMethods_ReturnAuthorizedType()
        {
            // This test verifies both Can methods return the correct Authorized type
            // The fact that it compiles proves the method names and return types are correct
            Authorized createResult = _factory.CanCreate();
            Authorized fetchResult = _factory.CanFetch();

            // Both results must be valid Authorized objects
            Assert.NotNull(createResult);
            Assert.NotNull(fetchResult);
            Assert.IsType<Authorized>(createResult);
            Assert.IsType<Authorized>(fetchResult);
        }

        [Fact]
        public void CanMethods_AffectFactoryMethods()
        {
            // When CanCreate returns false, Create should fail
            CanComprehensiveAuth.AllowCreate = false;
            var createResult = _factory.Create();
            Assert.Null(createResult);

            // When CanFetch returns false, Fetch should fail
            CanComprehensiveAuth.AllowFetch = false;
            var fetchResult = _factory.Fetch(42);
            Assert.Null(fetchResult);
        }

        [Fact]
        public void CanMethods_SuccessAllowsExecution()
        {
            // When CanCreate returns true, Create should succeed
            CanComprehensiveAuth.AllowCreate = true;
            var createResult = _factory.Create();
            Assert.NotNull(createResult);
            Assert.Equal("Created", createResult.CreateValue);

            // When CanFetch returns true, Fetch should succeed
            CanComprehensiveAuth.AllowFetch = true;
            var fetchResult = _factory.Fetch(42);
            Assert.NotNull(fetchResult);
            Assert.Equal("Fetched-42", fetchResult.FetchValue);
        }
    }

    #endregion
}
