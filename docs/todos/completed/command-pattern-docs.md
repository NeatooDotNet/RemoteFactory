# Command Pattern Documentation Improvements

**Created:** 2024-12-27
**Status:** Completed
**Priority:** Medium
**Completed:** 2024-12-27

---

## Problem Statement

Users who want to implement a simple **command/query pattern** (send a request value object, get a response value object) have difficulty finding how to do this in RemoteFactory. The pattern IS supported via `[Execute]` on static classes, but:

1. Documentation is buried in "Advanced Topics"
2. Examples start with complex scenarios instead of simple ones
3. Terminology doesn't match what users search for ("command", "query", "request-response")
4. The simple `Criteria → Result` pattern is hidden halfway through the doc

## User's Mental Model

Users think:
> "I have a simple request object, I want to send it to the server and get a response object back. This isn't a full domain object with CRUD - just a simple call."

```csharp
// What users want to do:
public class GetUserQuery
{
    public int UserId { get; set; }
}

public class UserResult
{
    public string Name { get; set; }
    public string Email { get; set; }
}

// "How do I call the server with GetUserQuery and get back UserResult?"
```

## Current Documentation Location

- **Primary:** `docs/advanced/static-execute.md`
- **Brief mention:** `docs/concepts/factory-operations.md` (lines 359-392)
- **Index link:** Under "Advanced Topics" only

## Proposed Changes

### 1. Add "Commands & Queries" Section to factory-operations.md

**Location:** After "Execute Operations" section (around line 392)

**Content to add:**

```markdown
## Commands & Queries Pattern

For simple request-response operations that don't involve domain object graphs, use `[Execute]` with static classes. This is ideal for:

- **Queries**: Fetch data without loading a full domain model
- **Commands**: Perform actions that return simple results
- **Lookups**: Get dropdown options, validate codes, check availability

### Simple Example

```csharp
// Request (criteria)
public class GetUserQuery
{
    public int UserId { get; set; }
}

// Response (result)
public class UserResult
{
    public string Name { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
}

// The command handler
[Factory]
public static partial class UserQueries
{
    [Remote]
    [Execute]
    public static async Task<UserResult?> GetUser(
        GetUserQuery query,
        [Service] IUserContext ctx)
    {
        var user = await ctx.Users.FindAsync(query.UserId);
        if (user == null) return null;

        return new UserResult
        {
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive
        };
    }
}
```

**Generated factory:**

```csharp
public interface IUserQueriesFactory
{
    Task<UserResult?> GetUser(GetUserQuery query);
}
```

**Usage:**

```csharp
@inject IUserQueriesFactory UserQueries

var result = await UserQueries.GetUser(new GetUserQuery { UserId = 123 });
if (result != null)
{
    Console.WriteLine($"Found: {result.Name}");
}
```

See [Static Execute](../advanced/static-execute.md) for more patterns including authorization, multiple methods, and complex parameters.
```

---

### 2. Update Index with Better Discoverability

**File:** `docs/index.md`

**Change:** Add to "Core Concepts" section:

```markdown
## Core Concepts

- **[Architecture Overview](concepts/architecture-overview.md)**: Understand how RemoteFactory works
- **[Factory Operations](concepts/factory-operations.md)**: Create, Fetch, Insert, Update, Delete, and Execute
- **[Commands & Queries](concepts/factory-operations.md#commands--queries-pattern)**: Simple request-response patterns
- **[Three-Tier Execution](concepts/three-tier-execution.md)**: Server, Remote, and Logical modes
- **[Service Injection](concepts/service-injection.md)**: Using `[Service]` for dependency injection
```

---

### 3. Reorder static-execute.md - Simple Example First

**File:** `docs/advanced/static-execute.md`

**Current order:**
1. When to Use Static Execute
2. Basic Syntax (SalesReport - complex example)
3. Multiple Execute Methods
4. With Authorization
5. Return Types
6. Complex Parameters (SearchCriteria - finally the simple pattern!)
7. ...

**Proposed order:**
1. When to Use Static Execute
2. **Simple Command/Query Pattern** (NEW - GetUser example)
3. Basic Syntax (rename to "Report Generation Example")
4. Complex Parameters (move up)
5. Multiple Execute Methods
6. With Authorization
7. Return Types
8. ...

---

### 4. Update Title and Add Terminology

**File:** `docs/advanced/static-execute.md`

**Current:**
```yaml
title: "Static Execute"
description: "Using [Execute] with static classes for remote procedure calls"
```

**Proposed:**
```yaml
title: "Commands, Queries & Static Execute"
description: "Simple request-response patterns using [Execute] for commands, queries, and remote procedure calls"
```

**Add to top of document after intro:**

```markdown
## Terminology

| Term | Description | Example |
|------|-------------|---------|
| **Command** | An action that may change state | `DeactivateUser(userId)` |
| **Query** | A request for data that doesn't change state | `GetUserById(userId)` |
| **Request/Criteria** | The input value object | `SearchCriteria`, `GetUserQuery` |
| **Response/Result** | The output value object | `SearchResults`, `UserResult` |

All of these patterns use `[Execute]` on static partial classes.
```

---

## Implementation Checklist

- [x] Add "Commands & Queries" section to `factory-operations.md`
- [x] Add "Commands & Queries" link to `index.md` Core Concepts
- [x] Reorder `static-execute.md` to show simple pattern first
- [x] Update title/description of `static-execute.md`
- [x] Add terminology table to `static-execute.md`
- [x] Update this document with completion status

---

## Success Criteria

A user searching for any of these terms should find the pattern within 2 clicks:
- "command pattern"
- "query pattern"
- "request response"
- "simple server call"
- "criteria result"
- "RPC"

The simplest possible example (single request → single response) should appear before complex examples.
