using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Samples.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for docs/interfaces-reference.md documentation.
/// </summary>
public partial class InterfacesReferenceSamples
{
    #region interfaces-factoryonstart
    [Factory]
    public partial class FactoryOnStartExample : IFactoryOnStart
    {
        public Guid Id { get; private set; }
        public FactoryOperation? StartedOperation { get; private set; }
        public DateTime StartTime { get; private set; }

        [Create]
        public FactoryOnStartExample() { Id = Guid.NewGuid(); }

        // Called BEFORE the factory operation executes
        public void FactoryStart(FactoryOperation factoryOperation)
        {
            StartedOperation = factoryOperation;
            StartTime = DateTime.UtcNow;

            // Pre-operation logic: validation, setup, logging
            if (factoryOperation == FactoryOperation.Delete && Id == Guid.Empty)
            {
                throw new InvalidOperationException("Cannot delete: entity has no Id");
            }
        }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id)
        {
            Id = id;
            return Task.FromResult(true);
        }
    }
    #endregion

    #region interfaces-factoryonstart-async
    [Factory]
    public partial class FactoryOnStartAsyncExample : IFactoryOnStartAsync
    {
        public Guid Id { get; private set; }
        public bool PreConditionsValidated { get; private set; }

        [Create]
        public FactoryOnStartAsyncExample() { Id = Guid.NewGuid(); }

        // Async version for operations requiring async validation
        public async Task FactoryStartAsync(FactoryOperation factoryOperation)
        {
            // Async pre-operation logic
            await Task.Delay(1); // Simulate async validation

            PreConditionsValidated = true;
        }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id)
        {
            Id = id;
            return Task.FromResult(true);
        }
    }
    #endregion

    #region interfaces-factoryoncomplete
    [Factory]
    public partial class FactoryOnCompleteExample : IFactoryOnComplete
    {
        public Guid Id { get; private set; }
        public FactoryOperation? CompletedOperation { get; private set; }
        public DateTime CompleteTime { get; private set; }

        [Create]
        public FactoryOnCompleteExample() { Id = Guid.NewGuid(); }

        // Called AFTER the factory operation completes successfully
        public void FactoryComplete(FactoryOperation factoryOperation)
        {
            CompletedOperation = factoryOperation;
            CompleteTime = DateTime.UtcNow;

            // Post-operation logic: logging, notifications, cleanup
        }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id)
        {
            Id = id;
            return Task.FromResult(true);
        }
    }
    #endregion

    #region interfaces-factoryoncomplete-async
    [Factory]
    public partial class FactoryOnCompleteAsyncExample : IFactoryOnCompleteAsync
    {
        public Guid Id { get; private set; }
        public bool PostProcessingComplete { get; private set; }

        [Create]
        public FactoryOnCompleteAsyncExample() { Id = Guid.NewGuid(); }

        // Async version for post-operation processing
        public async Task FactoryCompleteAsync(FactoryOperation factoryOperation)
        {
            // Async post-operation logic
            await Task.Delay(1); // Simulate async processing

            PostProcessingComplete = true;
        }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id)
        {
            Id = id;
            return Task.FromResult(true);
        }
    }
    #endregion

    #region interfaces-factoryoncancelled
    [Factory]
    public partial class FactoryOnCancelledExample : IFactoryOnCancelled
    {
        public Guid Id { get; private set; }
        public FactoryOperation? CancelledOperation { get; private set; }
        public bool CleanupPerformed { get; private set; }

        [Create]
        public FactoryOnCancelledExample() { Id = Guid.NewGuid(); }

        // Called when an OperationCanceledException occurs
        public void FactoryCancelled(FactoryOperation factoryOperation)
        {
            CancelledOperation = factoryOperation;

            // Cleanup logic for cancelled operations
            CleanupPerformed = true;
        }

        [Remote, Fetch]
        public async Task<bool> Fetch(Guid id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
            Id = id;
            return true;
        }
    }
    #endregion

    #region interfaces-factoryoncancelled-async
    [Factory]
    public partial class FactoryOnCancelledAsyncExample : IFactoryOnCancelledAsync
    {
        public Guid Id { get; private set; }
        public bool AsyncCleanupComplete { get; private set; }

        [Create]
        public FactoryOnCancelledAsyncExample() { Id = Guid.NewGuid(); }

        // Async version for cleanup requiring async operations
        public async Task FactoryCancelledAsync(FactoryOperation factoryOperation)
        {
            // Async cleanup logic
            await Task.Delay(1); // Simulate async cleanup

            AsyncCleanupComplete = true;
        }

        [Remote, Fetch]
        public async Task<bool> Fetch(Guid id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
            Id = id;
            return true;
        }
    }
    #endregion

    #region interfaces-lifecycle-order
    [Factory]
    public partial class LifecycleOrderExample : IFactoryOnStart, IFactoryOnComplete, IFactoryOnCancelled
    {
        public Guid Id { get; private set; }
        public List<string> LifecycleEvents { get; } = new();

        [Create]
        public LifecycleOrderExample() { Id = Guid.NewGuid(); }

        // Execution order:
        // 1. FactoryStart (before operation)
        public void FactoryStart(FactoryOperation factoryOperation)
        {
            LifecycleEvents.Add($"Start: {factoryOperation}");
        }

        // 2. Factory operation executes (Fetch, Insert, etc.)
        [Remote, Fetch]
        public Task<bool> Fetch(Guid id)
        {
            LifecycleEvents.Add("Operation: Fetch");
            Id = id;
            return Task.FromResult(true);
        }

        // 3a. FactoryComplete (on success)
        public void FactoryComplete(FactoryOperation factoryOperation)
        {
            LifecycleEvents.Add($"Complete: {factoryOperation}");
        }

        // 3b. FactoryCancelled (on cancellation - instead of Complete)
        public void FactoryCancelled(FactoryOperation factoryOperation)
        {
            LifecycleEvents.Add($"Cancelled: {factoryOperation}");
        }
    }
    #endregion

    #region interfaces-factorysavemeta
    [Factory]
    public partial class FactorySaveMetaExample : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;

        // IFactorySaveMeta implementation
        // These properties control Save routing
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public FactorySaveMetaExample()
        {
            Id = Guid.NewGuid();
            // IsNew = true by default - Save will call Insert
        }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id)
        {
            Id = id;
            IsNew = false; // After fetch, IsNew = false - Save will call Update
            return Task.FromResult(true);
        }

        [Remote, Insert]
        public Task Insert()
        {
            IsNew = false;
            return Task.CompletedTask;
        }

        [Remote, Update]
        public Task Update()
        {
            return Task.CompletedTask;
        }

        [Remote, Delete]
        public Task Delete()
        {
            return Task.CompletedTask;
        }
    }

    // Save routing based on IFactorySaveMeta:
    // IsNew=true,  IsDeleted=false -> Insert
    // IsNew=false, IsDeleted=false -> Update
    // IsNew=false, IsDeleted=true  -> Delete
    // IsNew=true,  IsDeleted=true  -> No operation (new item deleted)
    #endregion

    #region interfaces-factorysave
    // IFactorySave<T> is implemented by generated factories
    // Usage: inject the factory interface and call Save()

    // Create new entity - IsNew=true by default
    // var entity = factory.Create();
    // entity.Name = "Test";

    // Save routes to Insert (IsNew=true, IsDeleted=false)
    // var saved = await factory.Save(entity);
    // saved.IsNew; // false - Insert completed

    // Modify and Save routes to Update (IsNew=false, IsDeleted=false)
    // saved.Name = "Updated";
    // await factory.Save(saved);

    // Mark deleted and Save routes to Delete (IsDeleted=true)
    // saved.IsDeleted = true;
    // await factory.Save(saved); // returns null - entity deleted
    #endregion

    #region interfaces-aspauthorize
    // Custom IAspAuthorize implementation for simplified authorization scenarios
    // (e.g., testing, non-ASP.NET Core environments, or custom authorization logic)
    public partial class CustomAspAuthorize : IAspAuthorize
    {
        private readonly IUserContext _userContext;

        public CustomAspAuthorize(IUserContext userContext)
        {
            _userContext = userContext;
        }

        public Task<string?> Authorize(
            IEnumerable<AspAuthorizeData> authorizeData,
            bool forbid = false)
        {
            // Check if user is authenticated
            if (!_userContext.IsAuthenticated)
            {
                var message = "User is not authenticated";
                if (forbid)
                {
                    throw new AspForbidException(message);
                }
                return Task.FromResult<string?>(message);
            }

            // Check role requirements from authorization data
            foreach (var data in authorizeData)
            {
                if (!string.IsNullOrEmpty(data.Roles))
                {
                    var requiredRoles = data.Roles.Split(',', StringSplitOptions.TrimEntries);
                    if (!requiredRoles.Any(role => _userContext.IsInRole(role)))
                    {
                        var message = $"User lacks required role(s): {data.Roles}";
                        if (forbid)
                        {
                            throw new AspForbidException(message);
                        }
                        return Task.FromResult<string?>(message);
                    }
                }
            }

            // Return empty string to indicate success
            return Task.FromResult<string?>(string.Empty);
        }
    }
    #endregion

    #region interfaces-ordinalserializable
    // IOrdinalSerializable marks types for array-based JSON serialization
    public partial class OrdinalSerializableExample : IOrdinalSerializable
    {
        public string Alpha { get; set; } = string.Empty;  // Index 0
        public int Beta { get; set; }                       // Index 1
        public DateTime Gamma { get; set; }                 // Index 2

        // Convert to array in alphabetical property order
        public object?[] ToOrdinalArray()
        {
            return [Alpha, Beta, Gamma];
        }
    }

    // JSON output: ["value", 42, "2024-01-15T10:30:00Z"]
    // Instead of: {"Alpha":"value","Beta":42,"Gamma":"2024-01-15T10:30:00Z"}
    #endregion

    #region interfaces-ordinalconverterprovider
    // IOrdinalConverterProvider<TSelf> enables types to provide their own ordinal converter
    // This is used by the source generator to create AOT-compatible serialization

    // Type implements the interface to provide its own converter
    public partial class CustomOrdinalType : IOrdinalConverterProvider<CustomOrdinalType>
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";

        // Static abstract method implementation
        public static JsonConverter<CustomOrdinalType> CreateOrdinalConverter()
        {
            return new CustomOrdinalConverter();
        }
    }

    public partial class CustomOrdinalConverter : JsonConverter<CustomOrdinalType>
    {
        public override CustomOrdinalType? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            reader.Read();
            var amount = reader.GetDecimal();
            reader.Read();
            var currency = reader.GetString() ?? "USD";
            reader.Read();

            return new CustomOrdinalType { Amount = amount, Currency = currency };
        }

        public override void Write(
            Utf8JsonWriter writer,
            CustomOrdinalType value,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Amount);
            writer.WriteStringValue(value.Currency);
            writer.WriteEndArray();
        }
    }
    #endregion

    #region interfaces-eventtracker
    // IEventTracker monitors pending fire-and-forget events
    // Inject via DI: IEventTracker eventTracker

    // Check number of pending events
    // eventTracker.PendingCount; // number of tracked events not yet complete

    // Wait for all pending events to complete (useful for graceful shutdown)
    // await eventTracker.WaitAllAsync();

    // After WaitAllAsync, all tracked events have completed
    // eventTracker.PendingCount; // 0
    #endregion

    // [Fact]
    public void FactoryOnStart_IsCalled()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IFactoryOnStartExampleFactory>();

        var entity = factory.Create();

        Assert.Equal(FactoryOperation.Create, entity.StartedOperation);
        Assert.True(entity.StartTime <= DateTime.UtcNow);
    }

    // [Fact]
    public async Task FactoryOnComplete_IsCalled()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IFactoryOnCompleteExampleFactory>();

        var entity = factory.Create();
        await factory.Fetch(Guid.NewGuid());

        Assert.Equal(FactoryOperation.Fetch, entity.CompletedOperation);
    }

    // [Fact]
    public async Task LifecycleOrder_IsCorrect()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<ILifecycleOrderExampleFactory>();

        var entity = factory.Create();
        entity.LifecycleEvents.Clear(); // Clear Create events

        await factory.Fetch(Guid.NewGuid());

        Assert.Equal(3, entity.LifecycleEvents.Count);
        Assert.Equal("Start: Fetch", entity.LifecycleEvents[0]);
        Assert.Equal("Operation: Fetch", entity.LifecycleEvents[1]);
        Assert.Equal("Complete: Fetch", entity.LifecycleEvents[2]);
    }

    // [Fact]
    public async Task FactorySaveMeta_RoutesCorrectly()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IFactorySaveMetaExampleFactory>();

        // New entity
        var entity = factory.Create();
        Assert.True(entity.IsNew);

        // Save -> Insert
        var saved = await factory.Save(entity);
        Assert.False(saved!.IsNew);

        // Save -> Update
        saved.Name = "Updated";
        var updated = await factory.Save(saved);
        Assert.NotNull(updated);

        // Save -> Delete
        updated.IsDeleted = true;
        var deleted = await factory.Save(updated);
        Assert.Null(deleted);
    }

    // [Fact]
    public void OrdinalSerializable_ToArray()
    {
        var example = new OrdinalSerializableExample
        {
            Alpha = "test",
            Beta = 42,
            Gamma = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        var array = example.ToOrdinalArray();

        Assert.Equal(3, array.Length);
        Assert.Equal("test", array[0]);
        Assert.Equal(42, array[1]);
        Assert.Equal(example.Gamma, array[2]);
    }
}
