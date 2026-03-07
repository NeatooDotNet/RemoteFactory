# Internal Factory Visibility for Client/Server Separation and IL Trimming

**Status:** Complete
**Priority:** High
**Created:** 2026-03-06
**Last Updated:** 2026-03-06

---

## Problem

The generator emits `IsServerRuntime` guards on ALL `Local*` methods, including client-safe methods like `Can*` and `Create`. When `IsServerRuntime` is set to `false` on the client (required for IL trimming), these methods throw instead of running locally. The generator has no signal to distinguish:

- Methods the client should call directly (Can*, Create)
- Methods that route to the server ([Remote] Fetch, Save)
- Methods only used within aggregates on the server (child factory operations)

Additionally, child entity factories (e.g., `IOrderLineFactory`) are exposed as public interfaces even though they're only called server-side within aggregate operations. This leaks internal implementation details to the client.

---

## Solution

Use the developer's **public vs internal** visibility on factory methods as the signal. Three categories emerge:

| Pattern | Meaning | Generator behavior |
|---|---|---|
| `[Remote] public` | Client calls it, routes to server | Remote/Local delegate fork (existing behavior) |
| `public` (no Remote) | Client calls it, runs locally | No `IsServerRuntime` guard, no server trip |
| `internal` (no Remote) | Server-only, within aggregate | Gets `IsServerRuntime` guard, trimmable |
| `[Remote] internal` | Contradiction | Emit diagnostic error |

### Factory interface visibility rules

1. **All methods internal** → entire factory interface is `internal` (child entity factories — client can't even inject them)
2. **All methods public** → factory interface is `public` (typical aggregate root)
3. **Mix of public and internal** → factory interface is `public`, but internal methods are excluded from the public interface (only accessible server-side through the concrete factory class)

### What this solves

- **Can\* and Create** are public, no guard → work on the client without a server trip
- **Child factories** are all-internal → invisible to client, naturally trimmed
- **IL trimming** — internal methods get the `IsServerRuntime` guard, trimmer removes them. Public methods without `[Remote]` have no guard and survive trimming.
- **Auth service resolution** — if the developer registers auth services on the client via `RegisterMatchingName`, public Can* methods work locally. If not, they get a clear DI exception — that's a developer configuration choice, not a framework error.

### Related context

This builds on the IL trimming work in the `explore-trimming-remote-only` todo. The `NeatooRuntime.IsServerRuntime` feature switch and `[FeatureSwitchDefinition]` infrastructure is already implemented.

---

## Plans

- [Internal Factory Visibility — Implementation Plan](../plans/completed/internal-factory-visibility.md) — Status: Complete

---

## Tasks

- [x] Architect designs the implementation (generator changes, model changes, interface generation rules)
- [x] Developer reviews plan
- [x] Implementation: generator changes for public/internal method detection
- [x] Implementation: factory interface visibility rules
- [x] Implementation: guard emission rules (public → no guard, internal → guard)
- [x] Implementation: diagnostic for `[Remote] internal` contradiction
- [x] Update Design project with public/internal examples
- [x] Tests: verify generated output for all three categories
- [x] Tests: verify trimming behavior with internal methods
- [x] Architect verification
- [x] Update docs and skill references

---

## Progress Log

### 2026-03-06
- Identified the problem: `IsServerRuntime` guard on sync methods (Can*, Create) breaks Blazor WASM client
- Explored async remote fallback — works for async methods but Can*/Create are sync
- Discovered that making Can* async adds unnecessary server round-trips when auth is registered locally
- Key insight: public/internal visibility on factory methods is the missing signal
- Confirmed auth services ARE registered on the client in the Person example via `RegisterMatchingName`
- Confirmed the reference-app client was missing `RegisterMatchingName` (added as optional with comment)
- Architect analyzed the public/internal approach — raised concerns about conflating C# access modifiers with client/server architecture, but the user's refined design addresses this
- Created branch `internalFactories`

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass
- [x] Design project builds successfully
- [x] Design project tests pass
- [ ] Person example publishes and runs with trimming enabled (deferred — requires Person example update)
- [x] Can* methods work on client without server round-trip
- [x] Child factory interfaces are internal (not injectable from client)

**Verification results:**
- Build: 0 errors, 0 warnings
- Tests: All passing (49 new tests + all existing tests)

---

## Results / Conclusions

The generator now uses the developer's `public` vs `internal` visibility on factory methods as the signal for guard emission, factory interface visibility, and IL trimming eligibility.

**What was implemented:**
- Generator detects method-level `DeclaredAccessibility` (class accessibility is independent and ignored)
- `public` methods: no `IsServerRuntime` guard → work on client without server trip
- `internal` methods: get `IsServerRuntime` guard → trimmable, server-only
- `[Remote] internal`: diagnostic error NF0105
- Factory interface visibility: all-internal → `internal interface`; any public → `public interface` (internal methods excluded)
- Entity classes recommended as `internal` with matched `public interface` (naming convention `I{ClassName}`)
- Design projects updated: `Order`, `OrderLine`, `OrderLineList` all demonstrate the internal class + public interface pattern

**Test coverage:** 49 new tests (39 unit + 10 integration) covering all visibility combinations, guard emission, interface visibility, Can* methods, NF0105 diagnostic, and internal class with matched interface patterns.

**Documentation:** Updated CLAUDE-DESIGN.md, docs/ (trimming, authorization, client-server-architecture), and skills/RemoteFactory/ (SKILL.md, trimming, class-factory, anti-patterns, advanced-patterns).
