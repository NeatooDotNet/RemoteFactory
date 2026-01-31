# Create Design Source of Truth Projects

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-30
**Last Updated:** 2026-01-30

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
- [x] Create sample page demonstrating all patterns

### Phase 8: Documentation & Finalization
- [x] Create `README.md` and `CLAUDE-DESIGN.md`
- [x] Update main `CLAUDE.md` to reference design projects
- [ ] Re-evaluate relationship with docs/samples after completion

### Comment Requirements
- [x] At least 10 "DID NOT DO THIS BECAUSE" comments (achieved: 10+)
- [x] At least 5 "DESIGN DECISION" comments (achieved: 15+)
- [x] At least 3 "GENERATOR BEHAVIOR" comments (achieved: 5+)
- [x] At least 3 "COMMON MISTAKE" comments (achieved: 8+)

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

**2026-01-30**: Implementation started. Completed:
- Phase 1: Created src/Design/ with Design.sln, Design.Domain, Design.Tests
- Phase 2: AllPatterns.cs with all three factory patterns side-by-side
- Phase 3: Order aggregate, OrderLine child entity, Money value object
- Phase 6 (partial): 11 tests passing, covering Class Factory pattern

Key learnings documented in code:
- Static factory methods must be `private static` with underscore prefix
- Interface factory methods don't need operation attributes
- Properties need public setters for serialization
- Child factory references aren't preserved across serialization (documented as design consideration)

**2026-01-30**: Phase 6 completed. All testing done:
- InterfaceFactoryTests.cs: 4 tests for Interface Factory pattern
- StaticFactoryTests.cs: 4 tests for Static Factory pattern (Execute and Event delegates)
- SerializationTests.cs: 7 tests for serialization round-trip behavior
- All 26 tests passing across net8.0, net9.0, net10.0

Key learnings added:
- Event delegates use `Event` suffix (e.g., `ExampleEvents.OnOrderPlacedEvent`)
- Collections require local mode for full functionality (factory references lost in remote mode)
- Value objects (records) serialize correctly via JSON

**2026-01-30**: Phase 7 completed. Server/Client integration:
- Design.Server: ASP.NET Core with AddNeatooAspNetCore() and UseNeatoo()
- Design.Client.Blazor: Blazor WASM with AddNeatooRemoteFactory() and keyed HttpClient
- Home.razor: Interactive sample page demonstrating all three factory patterns
- Both projects added to Design.sln, all 26 tests still passing

**2026-01-30**: Phase 8 completed. Documentation:
- README.md: Overview, project structure, three patterns, running instructions
- CLAUDE-DESIGN.md: Quick reference for Claude Code with all patterns and rules
- Updated main CLAUDE.md to reference Design projects as source of truth
- All phases 1-8 complete (except final evaluation of docs/samples relationship)

---

## Results / Conclusions

