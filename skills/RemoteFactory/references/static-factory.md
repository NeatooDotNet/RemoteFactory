# Static Factory Pattern

Use for stateless commands. No instance state — pure functions with side effects via services.

## Execute Commands (Request-Response)

Use `[Execute]` for operations that return a result. The client awaits the response. `[Execute]` obeys `[Remote]` like every other operation: with it the call crosses to the server; without it the delegate runs on whichever tier resolves it, with that tier's services — the shape for client-side computation that must not round-trip.

> **Note:** `[Execute]` also works on non-static `[Factory]` classes when the operation returns the containing type. See `references/class-factory.md` for that pattern.

<!-- snippet: skill-static-execute-commands -->
<a id='snippet-skill-static-execute-commands'></a>
```cs
[Factory]
public static partial class SkillEmployeeCommands
{
    [Remote, Execute]
    private static async Task<bool> _SendNotification(
        string recipient,
        string message,
        [Service] IEmailService service)
    {
        await service.SendAsync(recipient, "Notification", message);
        return true;
    }

    [Remote, Execute]
    private static async Task<SkillEmployeeSummary> _GetEmployeeSummary(
        Guid employeeId,
        [Service] IEmployeeRepository repo)
    {
        var employee = await repo.GetByIdAsync(employeeId);
        if (employee == null)
            return new SkillEmployeeSummary { Id = employeeId, Found = false };

        return new SkillEmployeeSummary
        {
            Id = employeeId,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Position = employee.Position,
            Found = true
        };
    }
}

public class SkillEmployeeSummary
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool Found { get; set; }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/StaticFactorySamples.cs#L6-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-static-execute-commands' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generates**:
- `SkillEmployeeCommands.SendNotification(recipient, message)` delegate
- `SkillEmployeeCommands.GetEmployeeSummary(employeeId)` delegate

**Usage**:
```csharp
var success = await SkillEmployeeCommands.SendNotification("admin@example.com", "Hello!");
var summary = await SkillEmployeeCommands.GetEmployeeSummary(employeeId);
```

> **Looking for domain events?** See `references/factory-events.md` for `IFactoryEvents.Raise`, the `[FactoryEventHandler<T>]` class attribute, `RaiseOptions.ServerOnly`, and `IFactoryEventRelay`. That is RemoteFactory's only event-shaped abstraction; the former `[Event]` method attribute was removed in v1.5.0.

---

## Critical Rules

### Methods must be `private static` with underscore prefix

```csharp
// WRONG - conflicts with generated code
[Remote, Execute]
public static Task<bool> SendNotification(...) { }

// RIGHT - private with underscore
[Remote, Execute]
private static Task<bool> _SendNotification(...) { }
```

The generator creates the public method. Your code provides the private implementation.

`private static` also matters for trimming: with `[Remote]`, the generated local registration is guarded by `NeatooRuntime.IsServerRuntime`, so on a Blazor WASM client published with the feature switch set to `false`, the method body, its `[Service]` dependencies, and their transitive references are removed. Without `[Remote]` there is no guard — the body ships to the client and runs there, which is the point of a bare `[Execute]`. Static factories are exempt from the NF0105 `[Remote] public` check; `[Remote]` alone decides placement. See `references/trimming.md`, which documents what each factory shape does and does not remove.

### Bare [Execute] runs where it is called

Leave `[Remote]` off when the command must run on the tier that calls it — a client-side engine, a scoring rule, anything that must not round-trip. The generator emits one unguarded local delegate in every mode and no remote delegate; `[Service]` parameters resolve from that tier's container, so the dependency must be registered there (a bare command that takes a server-only repository compiles, then fails on the client at call time).

<!-- snippet: attributes-execute-local -->
<a id='snippet-attributes-execute-local'></a>
```cs
[Factory]
public static partial class TallyCommand
{
    [Execute]  // No [Remote] - runs on whichever tier resolves the delegate, with that tier's services
    private static Task<decimal> _Total(decimal baseSalary, decimal bonus, [Service] ISalaryCalculator calculator)
        => Task.FromResult(calculator.Calculate(baseSalary, bonus));
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L99-L107' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-execute-local' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### [Execute] must return `Task<T>`, not `Task`

```csharp
// WRONG - no return value
[Remote, Execute]
private static Task _DoSomething(...) { }

// RIGHT - returns a value
[Remote, Execute]
private static Task<bool> _DoSomething(...) { return Task.FromResult(true); }
```

The client needs something to await and confirm the operation completed.

### Static classes must be `partial`

```csharp
// WRONG - missing partial
[Factory]
public static class Commands { }

// RIGHT
[Factory]
public static partial class Commands { }
```

---

## When to Use Static Factory

- **Stateless commands** — No instance state needed
- **Request-response operations** — Clean function-style API
- **Cross-cutting operations** — Notifications, auditing, logging (invoked via `[Execute]` request-response)

For fire-and-forget work (email, webhooks, queue publishes), call `Task.Run` directly inside the factory method with a fresh scope from `IServiceScopeFactory.CreateScope()`. For transactional domain events, use `IFactoryEvents.Raise` + `[FactoryEventHandler<T>]` — see `references/factory-events.md`.
