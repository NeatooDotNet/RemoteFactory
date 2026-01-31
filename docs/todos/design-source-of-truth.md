# Create Design Source of Truth Projects

**Status:** Complete
**Priority:** High
**Created:** 2026-01-30
**Last Updated:** 2026-01-31

---

## Problem

Design decisions are being lost, forgotten, or contradicted because there's no authoritative source of truth:

- **Codebase**: API is too hard to deduce from implementation
- **User documentation**: Always behind and structured for users, not AI comprehension
- **Sample projects**: User-focused, fall behind, not structured for API deduction
- **Example projects**: User-focused, same problems
- **CLAUDE.md**: Causes confusion during design changes (AI reverts to "what was")

This leads to:
- Repeated proposals of previously-rejected designs
- Enhancements that miss critical existing functionality
- Losing track of why certain design decisions were made

## Solution

Create a new `src/Design/` directory with actual C# projects specifically designed for Claude Code to understand the RemoteFactory API. These projects will:

1. **Be the authoritative design reference** - Updated first, everything else flows from it
2. **Include extensive comments** - Not just "what" but "what we didn't do and why"
3. **Cover the full public API** - Multiple projects for different platforms
4. **Be fully functional** - Compiles and tests pass, ensuring accuracy
5. **Capture design evolution** - Commented-out code showing rejected approaches

### Key Characteristics

- Heavy comments including `// DID NOT DO THIS BECAUSE XYZ`
- Commented-out code showing alternatives that were rejected
- Comments tying back to RemoteFactory library internals where important
- Internal patterns included when they affect design decisions
- Separate solution (`src/Design/Design.sln`) to avoid noise in main solution

### Design Update Workflow

```
Design Code → Design Plan → Updated Codebase + Design Code → Samples/Examples → Documentation
```

---

## Plans

- [Design Source of Truth - Implementation Plan](../plans/design-source-of-truth-plan.md)

---

## Tasks

### Phase 1-2: Foundation & Pattern Documentation
- [x] Create `src/Design/` directory structure
- [x] Create `Design.sln` solution with project references to RemoteFactory
- [x] Create `FactoryPatterns/AllPatterns.cs` showing all three factory patterns

### Phase 3: Class Factory Pattern
- [x] Implement `Order` aggregate root with `[Remote]` entry points
- [x] Implement `OrderLine` child entity (no `[Remote]`)
- [x] Implement `Money` value object
- [x] Add lifecycle hooks demonstration (IFactoryOnStartAsync, IFactoryOnCompleteAsync, IFactorySaveMeta)

### Phase 4-5: Interface & Static Factory Patterns
- [x] Create `IExampleRepository` (Interface Factory) - in AllPatterns.cs
- [x] Create `ExampleCommands` with `[Execute]` methods (Static Factory) - in AllPatterns.cs
- [x] Create `ExampleEvents` with `[Event]` methods (Static Factory) - in AllPatterns.cs

### Phase 6: Testing
- [x] Implement ClientServerContainers-based tests (DesignClientServerContainers.cs)
- [x] Test Class Factory pattern (ClassFactoryTests.cs, AggregateTests.cs)
- [x] Test Interface Factory pattern (InterfaceFactoryTests.cs)
- [x] Test Static Factory pattern (StaticFactoryTests.cs)
- [x] Implement serialization round-trip tests (SerializationTests.cs)

### Phase 7: Server/Client Integration
- [x] Create `Design.Server` ASP.NET Core project
- [x] Create `Design.Client.Blazor` project
- [x] Create sample page demonstrating all patterns (Home.razor)

### Phase 8: Documentation & Finalization
- [x] Create `README.md` and `CLAUDE-DESIGN.md`
- [x] Update main `CLAUDE.md` to reference design projects
- [ ] Re-evaluate relationship with docs/samples after completion

### Comment Requirements
- [x] At least 10 "DID NOT DO THIS BECAUSE" comments
- [x] At least 5 "DESIGN DECISION" comments
- [x] At least 3 "GENERATOR BEHAVIOR" comments
- [x] At least 3 "COMMON MISTAKE" comments

---

## Progress Log

**2026-01-30**: Created todo and initial plan based on discussion about design source of truth problems.

**2026-01-30**: Architect review completed. Plan updated with:
- All three factory patterns (Class, Static, Interface)
- Expanded API coverage checklist (lifecycle hooks, events, authorization, assembly attributes)
- New comment patterns (GENERATOR BEHAVIOR, COMMON MISTAKE)
- Out of Scope section to prevent scope creep
- Who Updates and When maintenance section
- Reordered implementation phases (8 phases, building complexity progressively)

**2026-01-31**: Recovered work from `docDesignCSharp` branch. Merged into `design-source-of-truth` branch. Build and all 26 tests passing across net8.0/net9.0/net10.0.

All phases complete except final evaluation of docs/samples relationship.

---

## Results / Conclusions

Implementation complete. The Design Source of Truth projects provide:

- **4 projects**: Design.Domain, Design.Tests, Design.Server, Design.Client.Blazor
- **26 passing tests** across net8.0/net9.0/net10.0
- **All 3 factory patterns** demonstrated with extensive comments
- **CLAUDE.md updated** to reference Design projects as source of truth
- **Sample Blazor page** showing all patterns working through client/server boundary
