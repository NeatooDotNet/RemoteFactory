using System.Text.Json;
using System.Text.Json.Serialization;
using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Interfaces;

#region interfaces-factoryonstart-async
/// <summary>
/// Demonstrates IFactoryOnStartAsync for async pre-operation work.
/// </summary>
[Factory]
public partial class EmployeeAsyncStart : IFactorySaveMeta, IFactoryOnStartAsync
{
    private readonly IEmployeeRepository? _repository;

    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Services accessed via constructor injection on the domain class.
    /// </summary>
    public EmployeeAsyncStart(IEmployeeRepository repository)
    {
        _repository = repository;
        Id = Guid.NewGuid();
    }

    [Create]
    public EmployeeAsyncStart()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Async pre-operation validation using constructor-injected repository.
    /// </summary>
    public async Task FactoryStartAsync(FactoryOperation factoryOperation)
    {
        if (factoryOperation == FactoryOperation.Insert && _repository != null)
        {
            // Validate department exists before insert
            var employees = await _repository.GetByDepartmentIdAsync(DepartmentId, default);
            if (employees.Count >= 100)
            {
                throw new InvalidOperationException(
                    "Department has reached maximum capacity of 100 employees");
            }
        }
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = DepartmentId, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion

#region interfaces-factoryoncomplete-async
/// <summary>
/// Demonstrates IFactoryOnCompleteAsync for async post-operation work.
/// </summary>
[Factory]
public partial class EmployeeAsyncComplete : IFactorySaveMeta, IFactoryOnCompleteAsync
{
    private readonly IEmailService? _emailService;

    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Services accessed via constructor injection on the domain class.
    /// </summary>
    public EmployeeAsyncComplete(IEmailService emailService)
    {
        _emailService = emailService;
        Id = Guid.NewGuid();
    }

    [Create]
    public EmployeeAsyncComplete()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Async post-operation notification using constructor-injected service.
    /// </summary>
    public async Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        if (factoryOperation == FactoryOperation.Insert && _emailService != null)
        {
            await _emailService.SendAsync(
                Email,
                "Welcome!",
                $"Welcome to the team, {FirstName}!",
                default);
        }
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = Email, DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion

#region interfaces-factoryoncancelled-async
/// <summary>
/// Demonstrates IFactoryOnCancelledAsync for async cancellation cleanup.
/// </summary>
[Factory]
public partial class EmployeeAsyncCancelled : IFactorySaveMeta, IFactoryOnCancelledAsync
{
    private readonly IAuditLogService? _auditLog;

    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Services accessed via constructor injection on the domain class.
    /// </summary>
    public EmployeeAsyncCancelled(IAuditLogService auditLog)
    {
        _auditLog = auditLog;
        Id = Guid.NewGuid();
    }

    [Create]
    public EmployeeAsyncCancelled()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Async cancellation handling with constructor-injected audit service.
    /// </summary>
    public async Task FactoryCancelledAsync(FactoryOperation factoryOperation)
    {
        if (_auditLog != null)
        {
            await _auditLog.LogAsync(
                "Cancelled",
                Id,
                "Employee",
                $"Operation {factoryOperation} was cancelled",
                default);
        }
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
#endregion

#region interfaces-factorysavemeta
/// <summary>
/// Demonstrates IFactorySaveMeta for save state tracking.
/// </summary>
[Factory]
public partial class EmployeeSaveDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    /// <summary>
    /// True for new entities not yet persisted.
    /// Set to true in constructor, false after Fetch or successful Insert.
    /// </summary>
    public bool IsNew { get; private set; } = true;

    /// <summary>
    /// True for entities marked for deletion.
    /// Set by application code before calling Save().
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Create sets IsNew = true for new entities.
    /// </summary>
    [Create]
    public EmployeeSaveDemo()
    {
        Id = Guid.NewGuid();
        IsNew = true;  // New entity
    }

    /// <summary>
    /// Fetch sets IsNew = false for existing entities.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;  // Existing entity
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;  // No longer new after insert
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
#endregion

#region interfaces-ordinalserializable
/// <summary>
/// Money value object implementing IOrdinalSerializable.
/// Useful for value objects and third-party types that cannot use [Factory].
/// </summary>
public class MoneyValueObject : IOrdinalSerializable
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyValueObject(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Returns properties in alphabetical order for ordinal serialization.
    /// Order: Amount, Currency (alphabetical)
    /// </summary>
    public object?[] ToOrdinalArray()
    {
        // Alphabetical order: Amount, Currency
        return [Amount, Currency];
    }
}
#endregion

#region interfaces-ordinalconverterprovider
/// <summary>
/// Money value object implementing IOrdinalConverterProvider for custom converter.
/// </summary>
public class MoneyWithConverter : IOrdinalSerializable, IOrdinalConverterProvider<MoneyWithConverter>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyWithConverter(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public object?[] ToOrdinalArray()
    {
        return [Amount, Currency];
    }

    /// <summary>
    /// Static factory method provides custom converter.
    /// Required for types implementing IOrdinalConverterProvider.
    /// </summary>
    public static JsonConverter<MoneyWithConverter> CreateOrdinalConverter()
    {
        return new MoneyOrdinalConverter();
    }

    /// <summary>
    /// Custom ordinal converter for Money.
    /// </summary>
    private sealed class MoneyOrdinalConverter : JsonConverter<MoneyWithConverter>
    {
        public override MoneyWithConverter Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            // Expect array: [amount, currency]
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected array for Money");

            reader.Read();
            var amount = reader.GetDecimal();

            reader.Read();
            var currency = reader.GetString() ?? "USD";

            reader.Read(); // EndArray

            return new MoneyWithConverter(amount, currency);
        }

        public override void Write(
            Utf8JsonWriter writer,
            MoneyWithConverter value,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Amount);
            writer.WriteStringValue(value.Currency);
            writer.WriteEndArray();
        }
    }
}
#endregion

#region interfaces-eventtracker
/// <summary>
/// Demonstrates IEventTracker usage for graceful shutdown.
/// </summary>
[Factory]
public static partial class EventTrackerDemo
{
    /// <summary>
    /// Uses IEventTracker to wait for all pending events.
    /// Returns the number of events that were pending.
    /// </summary>
    [Execute]
    private static async Task<int> _WaitForEvents(
        [Service] IEventTracker eventTracker,
        CancellationToken ct)
    {
        // Check how many events are pending
        var pendingCount = eventTracker.PendingCount;

        if (pendingCount > 0)
        {
            // Wait for all pending events to complete
            // Used during graceful shutdown
            await eventTracker.WaitAllAsync(ct);
        }

        return pendingCount;
    }
}
#endregion

#region interfaces-aspauthorize
/// <summary>
/// Custom IAspAuthorize implementation for logging and custom policy evaluation.
/// IAspAuthorize is commonly implemented for custom authorization requirements.
/// </summary>
public class AuditingAspAuthorize : IAspAuthorize
{
    private readonly IAspAuthorize _inner;
    private readonly IAuditLogService _auditLog;

    public AuditingAspAuthorize(
        IAspAuthorize inner,
        IAuditLogService auditLog)
    {
        _inner = inner;
        _auditLog = auditLog;
    }

    /// <summary>
    /// Custom implementation that logs authorization attempts.
    /// </summary>
    public async Task<string?> Authorize(
        IEnumerable<AspAuthorizeData> authorizeData,
        bool forbid = false)
    {
        // Log authorization attempt
        var policies = string.Join(", ",
            authorizeData.Select(a => a.Policy ?? a.Roles ?? "Default"));

        await _auditLog.LogAsync(
            "AuthorizationCheck",
            Guid.Empty,
            "Authorization",
            $"Checking policies: {policies}",
            default);

        // Delegate to inner implementation
        var result = await _inner.Authorize(authorizeData, forbid);

        // Log result
        var success = string.IsNullOrEmpty(result);
        await _auditLog.LogAsync(
            success ? "AuthorizationSuccess" : "AuthorizationFailed",
            Guid.Empty,
            "Authorization",
            success ? "Authorized" : $"Denied: {result}",
            default);

        return result;
    }
}
#endregion

#region interfaces-factorysave
/// <summary>
/// Demonstrates using the generated IFactorySave interface.
/// </summary>
[Factory]
public partial class EmployeeFactorySaveDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeFactorySaveDemo() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}

// Usage example (would be in a consumer/test project):
// var factory = serviceProvider.GetRequiredService<IEmployeeFactorySaveDemoFactory>();
// var employee = factory.Create();
// employee.FirstName = "John";
// var saved = await factory.Save(employee);  // IFactorySave<T>.Save()
// Assert.False(saved?.IsNew);
#endregion
