using System.Text.Json;
using System.Text.Json.Serialization;
using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Interfaces;

// Supporting classes for async lifecycle hooks - full implementations for compilation
[Factory]
public partial class EmployeeAsyncStart : IFactorySaveMeta, IFactoryOnStartAsync
{
    private readonly IEmployeeRepository? _repository;
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    public EmployeeAsyncStart(IEmployeeRepository repository)
    {
        _repository = repository;
        Id = Guid.NewGuid();
    }

    [Create]
    public EmployeeAsyncStart() => Id = Guid.NewGuid();

    public async Task FactoryStartAsync(FactoryOperation factoryOperation)
    {
        if (factoryOperation == FactoryOperation.Insert && _repository != null)
        {
            var employees = await _repository.GetByDepartmentIdAsync(DepartmentId, default);
            if (employees.Count >= 100)
                throw new InvalidOperationException("Department has reached maximum capacity");
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

[Factory]
public partial class EmployeeAsyncComplete : IFactorySaveMeta, IFactoryOnCompleteAsync
{
    private readonly IEmailService? _emailService;
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    public EmployeeAsyncComplete(IEmailService emailService)
    {
        _emailService = emailService;
        Id = Guid.NewGuid();
    }

    [Create]
    public EmployeeAsyncComplete() => Id = Guid.NewGuid();

    public async Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        if (factoryOperation == FactoryOperation.Insert && _emailService != null)
            await _emailService.SendAsync(Email, "Welcome!", $"Welcome, {FirstName}!", default);
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

[Factory]
public partial class EmployeeAsyncCancelled : IFactorySaveMeta, IFactoryOnCancelledAsync
{
    private readonly IAuditLogService? _auditLog;
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    public EmployeeAsyncCancelled(IAuditLogService auditLog)
    {
        _auditLog = auditLog;
        Id = Guid.NewGuid();
    }

    [Create]
    public EmployeeAsyncCancelled() => Id = Guid.NewGuid();

    public async Task FactoryCancelledAsync(FactoryOperation factoryOperation)
    {
        if (_auditLog != null)
            await _auditLog.LogAsync("Cancelled", Id, "Employee",
                $"Operation {factoryOperation} was cancelled", default);
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

#region interfaces-factorysavemeta
// IFactorySaveMeta: Provides IsNew/IsDeleted for Save routing
[Factory]
public partial class EmployeeSaveDemo : IFactorySaveMeta
{
    public bool IsNew { get; private set; } = true;   // true = Insert, false = Update
    public bool IsDeleted { get; set; }               // true = Delete

    // Routing: IsNew=true -> Insert, IsNew=false -> Update, IsDeleted=true -> Delete

    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeSaveDemo()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;  // Fetched = existing
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
        IsNew = false;
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
// IOrdinalSerializable: Compact array-based JSON serialization
public class MoneyValueObject : IOrdinalSerializable
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyValueObject(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // Properties in alphabetical order: Amount, Currency
    public object?[] ToOrdinalArray() => [Amount, Currency];
    // JSON: [100.50, "USD"] instead of {"Amount":100.50,"Currency":"USD"}
}
#endregion

#region interfaces-ordinalconverterprovider
// IOrdinalConverterProvider<TSelf>: Custom converter for ordinal serialization
public class MoneyWithConverter : IOrdinalSerializable, IOrdinalConverterProvider<MoneyWithConverter>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyWithConverter(decimal amount, string currency)
        => (Amount, Currency) = (amount, currency);

    public object?[] ToOrdinalArray() => [Amount, Currency];

    // Static factory provides the converter
    public static JsonConverter<MoneyWithConverter> CreateOrdinalConverter()
        => new MoneyOrdinalConverter();
}

// Converter implementation (outside snippet for brevity)
file sealed class MoneyOrdinalConverter : JsonConverter<MoneyWithConverter>
{
    public override MoneyWithConverter Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
        reader.Read(); var amount = reader.GetDecimal();
        reader.Read(); var currency = reader.GetString() ?? "USD";
        reader.Read();
        return new MoneyWithConverter(amount, currency);
    }

    public override void Write(
        Utf8JsonWriter writer, MoneyWithConverter value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Amount);
        writer.WriteStringValue(value.Currency);
        writer.WriteEndArray();
    }
}
#endregion

#region interfaces-eventtracker
// IEventTracker: Wait for pending fire-and-forget events during shutdown
[Factory]
public static partial class EventTrackerDemo
{
    [Execute]
    private static async Task<int> _WaitForEvents([Service] IEventTracker eventTracker, CancellationToken ct)
    {
        var pending = eventTracker.PendingCount;
        if (pending > 0)
            await eventTracker.WaitAllAsync(ct);  // Graceful shutdown
        return pending;
    }
}
#endregion

#region interfaces-aspauthorize
// IAspAuthorize: Custom authorization with audit logging
public class AuditingAspAuthorize : IAspAuthorize
{
    private readonly IAspAuthorize _inner;
    private readonly IAuditLogService _auditLog;

    public AuditingAspAuthorize(IAspAuthorize inner, IAuditLogService auditLog)
    {
        _inner = inner;
        _auditLog = auditLog;
    }

    public async Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false)
    {
        var policies = string.Join(", ", authorizeData.Select(a => a.Policy ?? a.Roles ?? "Default"));
        await _auditLog.LogAsync("AuthCheck", Guid.Empty, "Auth", $"Policies: {policies}", default);

        var result = await _inner.Authorize(authorizeData, forbid);

        await _auditLog.LogAsync(string.IsNullOrEmpty(result) ? "AuthSuccess" : "AuthFailed",
            Guid.Empty, "Auth", result ?? "OK", default);
        return result;
    }
}
#endregion

// Supporting class for IFactorySave demonstration
[Factory]
public partial class EmployeeFactorySaveDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeFactorySaveDemo() => Id = Guid.NewGuid();

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
