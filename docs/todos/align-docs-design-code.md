# Align Documentation with Design Source of Truth

**Status:** Complete
**Priority:** High
**Created:** 2026-01-30
**Last Updated:** 2026-01-31

---

## Problem

The user-facing documentation in `docs/` has discrepancies and gaps compared to the Design source of truth in `src/Design/`. This creates confusion for users and potential issues when following the documentation.

Key discrepancies identified:
1. Endpoint path mismatch (`/remotefactory` vs `/api/neatoo`)
2. Private setter examples contradict Design guidance
3. Missing documentation for important patterns and gotchas

## Solution

Audit and update both documentation and Design source to ensure complete alignment. Verify the actual runtime behavior where ambiguous, then update the incorrect source.

---

## Plans

---

## Tasks

### High Priority - Discrepancies

- [x] Verify actual endpoint path (`/remotefactory` vs `/api/neatoo`) and align both sources
- [x] Fix private setter examples in `docs/getting-started.md` to use public setters (or explain when private works)
- [x] Document method-injected service caveat: services stored in fields from method injection are lost after serialization. Solution: use constructor injection for services needed on both client and server.

### Medium Priority - Design Source Corrections

- [x] Fix Money.cs - value objects SHOULD have `[Factory]` with `[Fetch]` for self-hydration. Objects are responsible for hydrating themselves; parent passes data, child sets its own fields.
- [x] Document collection factory pattern (`OrderLineList : List<OrderLine>` with `[Factory]`)
- [x] Add testing guide with client/server container patterns (`DesignClientServerContainers`)

### Low Priority - Missing from Design

- [x] Add `[AspAuthorize]` example to Design.Domain
- [x] Add assembly-level attribute examples (`[assembly: FactoryMode]`) to Design.Client.Blazor
- [x] Add correlation context example to Design

---

## Progress Log

### 2026-01-30
- Created todo after docs-architect comparison of `docs/` vs `src/Design/`
- Identified 2 key discrepancies, 4 items missing from docs, 3 items missing from Design

### 2026-01-30 (continued)
Completed high and medium priority tasks:
1. **Endpoint path fix**: Updated Design source (`Design.Server/Program.cs`, `CLAUDE-DESIGN.md`, `README.md`) to use `/api/neatoo` (matching actual implementation)
2. **Private setter fix**: Updated `EmployeeModel.cs` sample to use public setters for `Id`, `Created`, `Modified`, `IsNew`
3. **Method-injection caveat**: Added "Serialization Caveat" section to `docs/service-injection.md` explaining that method-injected services stored in fields are lost after serialization, with constructor injection as the solution
4. **Money.cs fix**: Added `[Factory]` and `[Create]` to Money and Percentage records, updated comments to reflect that value objects should be responsible for self-hydration
5. **Collection factory pattern**: Added "Collection Factories" section to `docs/factory-operations.md` documenting the pattern
6. **Testing guide**: Added "Client/Server Container Testing" section to `docs/service-injection.md` with the DesignClientServerContainers pattern

All Design tests pass (26/26 across net8.0, net9.0, net10.0).

### 2026-01-31
Completed low priority tasks:
1. **[AspAuthorize] example**: Added `Design.Domain/Aggregates/SecureOrder.cs` demonstrating policy-based authorization with roles, multiple attributes (AND logic), and design decisions
2. **Assembly-level attributes**: Added `Design.Client.Blazor/AssemblyAttributes.cs` showing `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` with documentation of available modes
3. **Correlation context**: Added `Design.Domain/Services/CorrelationExample.cs` demonstrating CorrelationContext usage, EnsureCorrelationId(), BeginScope(), and distributed tracing patterns

Updated `CLAUDE-DESIGN.md` "Design Files to Consult" table with references to the three new files.

Build and tests verified:
- Design solution builds successfully
- All 26 tests pass across net8.0, net9.0, net10.0

---

## Results / Conclusions

All tasks complete. The documentation and Design source of truth are now fully aligned:

**Discrepancies Fixed:**
- Endpoint path standardized to `/api/neatoo`
- Private setter examples corrected to use public setters
- Method-injection serialization caveat documented

**Design Source Corrections:**
- Money.cs updated with proper `[Factory]` and `[Create]` for value object self-hydration
- Collection factory pattern documented
- Testing guide with client/server containers added

**Design Source Additions:**
- `SecureOrder.cs`: Comprehensive [AspAuthorize] example with policies, roles, and multiple attribute patterns
- `AssemblyAttributes.cs`: Assembly-level [FactoryMode] configuration for client projects
- `CorrelationExample.cs`: CorrelationContext usage for distributed tracing

The Design projects now serve as the complete, authoritative reference for all RemoteFactory patterns.
