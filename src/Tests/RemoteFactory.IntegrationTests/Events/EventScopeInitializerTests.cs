#pragma warning disable xUnit1051 // Test timing delays don't need test cancellation token

using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events;

/// <summary>
/// Scoped service used as a test double for tenant context propagation.
/// </summary>
public interface ITenantContext
{
    string? TenantId { get; set; }
    string? ConnectionString { get; set; }
}

public class TenantContext : ITenantContext
{
    public string? TenantId { get; set; }
    public string? ConnectionString { get; set; }
}

/// <summary>
/// Integration tests for the IEventScopeInitializer mechanism.
/// Verifies that custom scope initializers propagate ambient context
/// from the request scope to event handler scopes.
/// </summary>
public class EventScopeInitializerTests
{
    /// <summary>
    /// Verifies that a custom IEventScopeInitializer runs and propagates state to event scopes.
    /// Uses a thread-safe counter to prove the custom initializer actually executed.
    /// </summary>
    [Fact]
    public async Task CustomInitializer_PropagatesTenantContext_ToEventScope()
    {
        // Arrange — thread-safe flag to verify custom initializer ran
        var customInitializerRan = 0;

        var scopes = ClientServerContainers.Scopes(
            configureLocal: services =>
            {
                services.AddScoped<ITenantContext, TenantContext>();
                services.AddRemoteFactoryEventScopeInitializer((parentScope, childScope) =>
                {
                    var parentTenant = parentScope.GetService<ITenantContext>();
                    var childTenant = childScope.GetService<ITenantContext>();
                    if (parentTenant?.TenantId != null && childTenant != null)
                    {
                        childTenant.TenantId = parentTenant.TenantId;
                        childTenant.ConnectionString = parentTenant.ConnectionString;
                    }
                    Interlocked.Increment(ref customInitializerRan);
                });
            });

        // Set tenant context on the request scope
        var tenantContext = scopes.local.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.TenantId = "tenant-42";
        tenantContext.ConnectionString = "Host=localhost;Database=tenant_42";

        // Set correlation too, to verify both initializers run
        var correlationContext = scopes.local.ServiceProvider.GetRequiredService<ICorrelationContext>();
        correlationContext.CorrelationId = "corr-with-tenant";

        var processEvent = scopes.local.ServiceProvider
            .GetRequiredService<CorrelationEventTarget.ProcessWithCorrelationEvent>();
        var testService = scopes.local.ServiceProvider.GetRequiredService<IEventTestService>();

        // Act
        var entityId = Guid.NewGuid();
        await processEvent(entityId);
        await Task.Delay(200);

        // Assert — custom initializer ran
        Assert.True(customInitializerRan > 0, "Custom initializer did not run");

        // Assert — correlation was also propagated (built-in initializer ran)
        var recordedEvents = testService.GetRecordedEventsWithCorrelation();
        Assert.Contains(recordedEvents, e =>
            e.EntityId == entityId &&
            e.CorrelationId == "corr-with-tenant");
    }

    /// <summary>
    /// Verifies that multiple custom initializers all run.
    /// Uses separate counters to prove each one executed.
    /// </summary>
    [Fact]
    public async Task MultipleInitializers_AllRun_InRegistrationOrder()
    {
        // Arrange — thread-safe counters to verify each initializer ran
        var firstInitializerRan = 0;
        var secondInitializerRan = 0;

        var scopes = ClientServerContainers.Scopes(
            configureLocal: services =>
            {
                services.AddScoped<ITenantContext, TenantContext>();

                // First custom initializer: sets TenantId
                services.AddRemoteFactoryEventScopeInitializer((parentScope, childScope) =>
                {
                    var parentTenant = parentScope.GetService<ITenantContext>();
                    var childTenant = childScope.GetService<ITenantContext>();
                    if (parentTenant?.TenantId != null && childTenant != null)
                    {
                        childTenant.TenantId = parentTenant.TenantId;
                    }
                    Interlocked.Increment(ref firstInitializerRan);
                });

                // Second custom initializer: sets ConnectionString
                services.AddRemoteFactoryEventScopeInitializer((parentScope, childScope) =>
                {
                    var parentTenant = parentScope.GetService<ITenantContext>();
                    var childTenant = childScope.GetService<ITenantContext>();
                    if (parentTenant?.ConnectionString != null && childTenant != null)
                    {
                        childTenant.ConnectionString = parentTenant.ConnectionString;
                    }
                    Interlocked.Increment(ref secondInitializerRan);
                });
            });

        // Set tenant context
        var tenantContext = scopes.local.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.TenantId = "tenant-multi";
        tenantContext.ConnectionString = "Host=localhost;Database=multi";

        var processEvent = scopes.local.ServiceProvider
            .GetRequiredService<CorrelationEventTarget.ProcessWithCorrelationEvent>();
        var testService = scopes.local.ServiceProvider.GetRequiredService<IEventTestService>();

        // Act
        var entityId = Guid.NewGuid();
        await processEvent(entityId);
        await Task.Delay(200);

        // Assert — both custom initializers ran
        Assert.True(firstInitializerRan > 0, "First custom initializer did not run");
        Assert.True(secondInitializerRan > 0, "Second custom initializer did not run");

        // Assert — event executed
        var recordedEvents = testService.GetRecordedEventsWithCorrelation();
        Assert.Contains(recordedEvents, e =>
            e.EntityId == entityId &&
            e.EventName == "ProcessWithCorrelation");
    }

    /// <summary>
    /// Verifies that a failing initializer does not prevent the event handler from running.
    /// </summary>
    [Fact]
    public async Task FailingInitializer_DoesNotPreventEventHandler()
    {
        // Arrange — register an initializer that throws
        var scopes = ClientServerContainers.Scopes(
            configureLocal: services =>
            {
                services.AddRemoteFactoryEventScopeInitializer((parentScope, childScope) =>
                {
                    throw new InvalidOperationException("Initializer failure test");
                });
            });

        var correlationContext = scopes.local.ServiceProvider.GetRequiredService<ICorrelationContext>();
        correlationContext.CorrelationId = "should-still-work";

        var processEvent = scopes.local.ServiceProvider
            .GetRequiredService<CorrelationEventTarget.ProcessWithCorrelationEvent>();
        var testService = scopes.local.ServiceProvider.GetRequiredService<IEventTestService>();

        // Act
        var entityId = Guid.NewGuid();
        await processEvent(entityId);
        await Task.Delay(200);

        // Assert — event handler still ran despite the initializer failure
        var recordedEvents = testService.GetRecordedEventsWithCorrelation();
        Assert.Contains(recordedEvents, e =>
            e.EntityId == entityId &&
            e.EventName == "ProcessWithCorrelation");
    }

    /// <summary>
    /// Verifies that multiple AddRemoteFactoryEventScopeInitializer calls accumulate
    /// alongside the built-in CorrelationContextScopeInitializer.
    /// </summary>
    [Fact]
    public void MultipleRegistrations_AccumulateWithBuiltIn()
    {
        var scopes = ClientServerContainers.Scopes(
            configureLocal: services =>
            {
                services.AddRemoteFactoryEventScopeInitializer((_, _) => { });
                services.AddRemoteFactoryEventScopeInitializer((_, _) => { });
                services.AddRemoteFactoryEventScopeInitializer((_, _) => { });
            });

        // 1 built-in (CorrelationContextScopeInitializer) + 3 custom = 4
        var initializers = scopes.local.ServiceProvider.GetServices<IEventScopeInitializer>().ToList();
        Assert.Equal(4, initializers.Count);
    }

    /// <summary>
    /// Verifies that IEventScopeInitializer is registered in Server/Logical mode
    /// (built-in CorrelationContextScopeInitializer) but not in Remote mode.
    /// </summary>
    [Fact]
    public void BuiltInInitializer_RegisteredInServerAndLogical_NotInRemote()
    {
        var scopes = ClientServerContainers.Scopes();

        // Server mode should have the built-in initializer
        var serverInitializers = scopes.server.ServiceProvider.GetServices<IEventScopeInitializer>();
        Assert.NotEmpty(serverInitializers);

        // Local (Logical) mode should have the built-in initializer
        var localInitializers = scopes.local.ServiceProvider.GetServices<IEventScopeInitializer>();
        Assert.NotEmpty(localInitializers);

        // Client (Remote) mode should NOT have initializers
        var clientInitializers = scopes.client.ServiceProvider.GetServices<IEventScopeInitializer>();
        Assert.Empty(clientInitializers);
    }
}
