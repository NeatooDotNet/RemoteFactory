# Decision Guide

Quick answers to common "when should I use...?" questions.

---

## When Do I Need [Remote]?

`[Remote]` marks entry points from the client to the server. Once execution crosses to the server, it stays there — subsequent calls don't need `[Remote]`.

```
Is this method called directly from client code (UI, Blazor component)?
├── YES → Add [Remote]
└── NO (called from other server-side code)
    └── No [Remote] needed
```

**Examples**:
- `Fetch()` called by Blazor component → `[Remote, Fetch]`
- `LoadChildren()` called by `Fetch()` on the server → just `[Fetch]` (no `[Remote]`)
- `Insert()` called by `Save()` routed from client → `[Remote, Insert]`

---

## Constructor vs Method Injection?

Constructor injection puts services on both client and server. Method injection puts them only where the method executes — typically the server. This is how you control which services are available on each side.

```
Does the client need this service?
├── YES (validation, logging, client-side logic)
│   └── Constructor injection: [Service] on constructor parameter
└── NO (database, secrets, server-only)
    └── Method injection: [Service] on method parameter
```

**Rule of thumb**: Method injection is the common case. Use constructor injection only when clients need the service too.

See [Service Injection](service-injection.md) for details.

---

## Which Serialization Format?

Ordinal is the default. It's compact and fast. Switch to Named if you need to debug payloads — it produces standard JSON with property names.

```
Use Ordinal (default)
└── Switch to Named only if you need to debug payloads
```

See [Serialization](serialization.md) for a practical dev/production configuration pattern.

---

## Do I Need IFactorySaveMeta?

`IFactorySaveMeta` enables the `Save()` routing pattern — one call that routes to Insert, Update, or Delete based on entity state. If your entity only needs `Fetch()`, skip it.

```
Does your entity support Insert, Update, or Delete via Save()?
├── YES → Implement IFactorySaveMeta
│         (adds IsNew, IsDeleted properties)
└── NO (read-only, or explicit Insert/Update/Delete calls)
    └── Don't implement it
```

See [Save Operation](save-operation.md) for details.

---

## When to Use [Execute]?

`[Execute]` is for operations that don't follow the entity lifecycle (Create → Fetch → Save). Two common scenarios: one-shot command delegates, and static methods where you need to make a decision before instantiating an entity — like choosing Create vs Fetch based on existing data.

```
Does this operation follow the entity lifecycle (Create/Fetch/Save)?
├── YES → Use [Create], [Fetch], [Insert], [Update], [Delete]
└── NO
    ├── One-shot command → Static class with [Execute]
    └── Pre-instantiation decision → Static method with [Execute] on the entity
```

**Examples**:
- Generate monthly report → Static command with `[Execute]`
- Check if employee exists, then Create or Fetch → Static method on Employee with `[Execute]`

See [Factory Operations](factory-operations.md) for details.

---

## When to Use [Event]?

Events are fire-and-forget. The caller continues immediately. Use them for side effects that shouldn't block the main operation — notifications, audit logging, external system updates.

```
Should the caller wait for this operation to complete?
├── YES → Use regular method (or [Execute])
└── NO (notifications, logging, side effects)
    └── Use [Event]
```

**Requirements**:
- `CancellationToken` must be the last parameter
- Returns `void` or `Task`

---

## AuthorizeFactory vs [AspAuthorize]?

Use whichever fits your situation. `AuthorizeFactory` gives you a unified authorization layer that works on both client and server with full DI support. `[AspAuthorize]` applies standard ASP.NET Core policies — use it if that already fits your enterprise's broader auth model. You can mix both on the same entity.

```
What fits your auth model?
├── Domain-specific rules with DI → [AuthorizeFactory<T>]
├── ASP.NET Core policies/roles already in use → [AspAuthorize]
└── Both → Combine them (both checks must pass)
```

See [Authorization](authorization.md) for details.

---

## Next Steps

- [Attributes Reference](attributes-reference.md) — Complete attribute documentation
- [Client-Server Architecture](client-server-architecture.md) — Understanding `[Remote]`
- [Service Injection](service-injection.md) — DI patterns
- [Factory Modes](factory-modes.md) — Runtime modes (Server, Remote, Logical)
- [IL Trimming](trimming.md) — Remove server-only code from published Blazor WASM output
