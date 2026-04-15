# Static Factory Pattern

Use for stateless commands. No instance state — pure functions with side effects via services.

## Execute Commands (Request-Response)

Use `[Execute]` for operations that return a result. The client awaits the response.

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
