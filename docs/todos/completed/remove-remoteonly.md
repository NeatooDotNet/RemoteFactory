# Remove RemoteOnly Factory Mode

**Status:** Complete
**Priority:** Medium
**Created:** 2026-03-07
**Last Updated:** 2026-03-08


---

## Problem

The `FactoryMode.RemoteOnly` assembly attribute and all its supporting code should be removed. It adds complexity to the generator for a mode that is no longer needed.

## Solution

Remove the `RemoteOnly` enum value, the `FactoryModeAttribute`, and all conditional logic in the generator that branches on `FactoryMode.RemoteOnly` vs `FactoryMode.Full`. Remove the `RemoteOnlyTests` test project. Update docs, examples, and skills that reference RemoteOnly.

Key areas discovered:
- **Attribute definition:** `src/RemoteFactory/FactoryAttributes.cs` — `FactoryMode` enum and `FactoryModeAttribute`
- **Generator conditionals:** `src/Generator/FactoryGenerator.cs`, `src/Generator/FactoryGenerator.Types.cs`, `src/Generator/Renderer/ClassFactoryRenderer.cs`, `src/Generator/Renderer/InterfaceFactoryRenderer.cs`
- **Test project:** `src/Tests/RemoteOnlyTests/` (3 projects: Domain, Client, Server, Integration)
- **Examples:** `src/Examples/OrderEntry/` (client assembly attributes, Program.cs)
- **Design projects:** `src/Design/Design.Client.Blazor/AssemblyAttributes.cs`
- **Docs:** `docs/trimming.md`, `docs/factory-modes.md`, `docs/decision-guide.md`, `docs/attributes-reference.md`, `docs/service-injection.md`, `docs/events.md`, `README.md`
- **Skills:** `skills/RemoteFactory/SKILL.md`, `skills/RemoteFactory/references/setup.md`, `skills/RemoteFactory/references/trimming.md`
- **Reference app:** `src/docs/reference-app/` (multiple sample files)
- **Release notes:** `docs/release-notes/` (historical references)
- **Solution file:** `src/Neatoo.RemoteFactory.sln`

---

## Clarifications

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-07
**Verdict:** APPROVED

### Relevant Requirements Found

**1. FactoryMode is a compile-time concept; NeatooFactory is the runtime concept (must not be confused).**

The compile-time `FactoryMode` enum (`Full`, `RemoteOnly`) in `src/RemoteFactory/FactoryAttributes.cs:148-162` controls what the source generator emits. The runtime `NeatooFactory` enum (`Server`, `Remote`, `Logical`) in `src/RemoteFactory/AddRemoteFactoryServices.cs:10-26` controls DI registration behavior. Removing `FactoryMode.RemoteOnly` and `FactoryModeAttribute` is purely a compile-time generator concern. The runtime `NeatooFactory` enum and its three modes (Server, Remote, Logical) are unaffected and must remain intact.

**2. IL Trimming with IsServerRuntime guards is the replacement mechanism.**

The completed exploration plan at `docs/plans/completed/explore-trimming-remote-only.md` (Rule 13, line 74) explicitly states: "WHEN an assembly uses `[assembly: FactoryMode(FactoryMode.RemoteOnly)]`, THEN `LocalMethod` methods are NOT generated at all (current behavior). The feature switch approach targets `FactoryMode.Full` assemblies where Local methods exist but should be trimmable. These two mechanisms are complementary, not competing." IL Trimming via `NeatooRuntime.IsServerRuntime` (`src/RemoteFactory/NeatooRuntime.cs`) now provides the same bundle-size reduction benefit that RemoteOnly provided, making RemoteOnly redundant.

**3. Generator branching on FactoryMode.RemoteOnly exists in multiple locations.**

The generator conditionally branches on `FactoryMode.RemoteOnly` in:
- `src/Generator/FactoryGenerator.cs` lines 212, 237, 784 (FactoryServiceRegistrar local method/event skipping, constructor generation)
- `src/Generator/FactoryGenerator.Types.cs` lines 1033, 1041, 1053, 1330 (local constructor assignments, service registrations, LocalMethod skipping)
- `src/Generator/Renderer/ClassFactoryRenderer.cs` lines 177, 264 (field nullability, constructor generation, entity/save registration, write method rendering)
- `src/Generator/Renderer/InterfaceFactoryRenderer.cs` lines 93, 156 (constructor generation)

All these branches produce a subset of what `FactoryMode.Full` produces. After removal, only the Full code path remains, which is the correct default.

**4. Design Source of Truth references FactoryMode.RemoteOnly.**

`src/Design/CLAUDE-DESIGN.md` line 670 references `Design.Client.Blazor/AssemblyAttributes.cs` as demonstrating `[FactoryMode]` configuration. The actual file (`src/Design/Design.Client.Blazor/AssemblyAttributes.cs`) contains `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` with extensive design decision comments. Both must be updated.

**5. The Design.Client.Blazor project currently uses RemoteOnly mode.**

`src/Design/Design.Client.Blazor/AssemblyAttributes.cs` applies `[assembly: FactoryMode(FactoryMode.RemoteOnly)]`. After removal, this file should either be deleted or converted to demonstrate IL Trimming configuration instead. The Design.Client.Blazor `Program.cs` does NOT reference FactoryMode — it only uses `NeatooFactory.Remote`, which is unaffected.

**6. Dedicated RemoteOnly test project exists.**

`src/Tests/RemoteOnlyTests/` contains 4 sub-projects (Domain, Client, Server, Integration) that specifically test the RemoteOnly compile-time mode. The todo correctly identifies these for removal. The behavioral coverage they provide (client-server round-trip testing) is already covered by `src/Tests/RemoteFactory.IntegrationTests/` which uses the Full mode with the two-container pattern.

**7. Published docs extensively reference RemoteOnly.**

The following docs reference RemoteOnly as a feature:
- `docs/factory-modes.md` — Entire "Compile-Time Modes" section documents RemoteOnly
- `docs/trimming.md` — "Trimming vs RemoteOnly" comparison section
- `docs/attributes-reference.md` — `[assembly: FactoryMode]` entry in Quick Lookup table and Assembly-Level Attributes section
- `docs/decision-guide.md` — "Full vs RemoteOnly Mode?" and "IL Trimming or RemoteOnly?" sections
- `docs/service-injection.md` — Brief mention of "RemoteOnly mode"
- `docs/events.md` — Brief mention of "RemoteOnly mode"
- `README.md` — Feature list, client assembly mode snippet, and documentation links

**8. Skills reference RemoteOnly.**

- `skills/RemoteFactory/SKILL.md` line 43 — Quick decisions table mentions RemoteOnly
- `skills/RemoteFactory/references/setup.md` lines 124-146 — "[assembly: FactoryMode] for Client Assemblies" section
- `skills/RemoteFactory/references/trimming.md` lines 81-93 — "Trimming vs RemoteOnly" section

**9. FactoryGenerationUnit model carries FactoryMode through the pipeline.**

`src/Generator/Model/FactoryGenerationUnit.cs` has a `Mode` property of type `FactoryMode` that flows from `TypeInfo.FactoryMode` through the builder to all three renderers. After removal, this can either be removed entirely (since the only remaining value would be `Full`) or left as a single-value enum. The architect should decide which simplification approach to take.

**10. The FactoryMode enum itself is in the public NuGet package API surface.**

`FactoryMode` enum and `FactoryModeAttribute` class are defined in `src/RemoteFactory/FactoryAttributes.cs` (lines 148-200), which ships in the `Neatoo.RemoteFactory` NuGet package. Removing them is a **breaking change** for any consumer who uses `[assembly: FactoryMode(FactoryMode.RemoteOnly)]`. This must be called out in release notes with a migration guide.

**11. No Design Debt conflict.**

The Design Debt table in `src/Design/CLAUDE-DESIGN.md` does not list RemoteOnly or FactoryMode as a deliberately deferred feature. RemoteOnly is an existing feature being removed, not a deferred one being proposed. No Design Debt conflict exists.

### Gaps

**1. No existing requirement documents what happens when FactoryMode.Full is the only mode.**

After removal, the `FactoryMode` enum, `FactoryModeAttribute`, and all conditional generator logic can be completely removed rather than leaving a single-value enum. However, there is no existing guidance on whether to keep the `FactoryMode` enum with only `Full` (for future extensibility) or remove the entire concept. The architect must decide.

**2. No migration guide exists for consumers currently using RemoteOnly.**

Consumers using `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` need a documented migration path: remove the attribute, rely on IL Trimming for bundle size reduction. This needs to be included in the plan.

**3. Reference app samples use FactoryModeOption (a different enum name?).**

Several snippets in docs reference `FactoryModeOption.RemoteOnly` and `FactoryModeOption.Full` (e.g., `docs/factory-modes.md` line 82, `docs/attributes-reference.md` line 340). This may be a naming inconsistency in the reference app samples vs the actual enum name `FactoryMode`. The architect should verify whether `FactoryModeOption` is an alias in the reference app or a documentation error.

### Contradictions

None found. Removing RemoteOnly does not contradict any documented design pattern, anti-pattern, or design debt entry. The IL Trimming feature (completed and documented) provides the same client-side bundle reduction that RemoteOnly provided, making this a valid simplification.

### Recommendations for Architect

1. **This is a breaking change.** The `FactoryMode` enum and `FactoryModeAttribute` are in the public API surface of the NuGet package. Plan for a major version bump and migration guide in release notes.

2. **Remove the entire FactoryMode concept, not just RemoteOnly.** Since `Full` is the default and would be the only remaining value, the enum, attribute, and all generator branching can be eliminated entirely. This is cleaner than leaving vestigial single-value types.

3. **Update Design.Client.Blazor to demonstrate IL Trimming instead.** Replace the `AssemblyAttributes.cs` file content (or remove it). Update `CLAUDE-DESIGN.md` line 670 to reflect the new setup. The client setup should demonstrate the `RuntimeHostConfigurationOption` approach from `docs/trimming.md`.

4. **Verify the FactoryModeOption naming.** Check whether `FactoryModeOption` in reference app samples is a real type or a documentation error before updating those files.

5. **Update all docs to remove the "Trimming vs RemoteOnly" framing.** In `docs/trimming.md`, `docs/decision-guide.md`, `docs/factory-modes.md`, and skill files, IL Trimming should be presented as the sole mechanism for client bundle reduction.

6. **The `FactoryGenerationUnit.Mode` property can be removed.** Once all RemoteOnly branches are eliminated, there is no need to carry a mode through the generation pipeline.

---

## Plans

- [Remove RemoteOnly Factory Mode Plan](../../plans/completed/remove-remoteonly-plan.md)

---

## Tasks

- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3)
- [x] Architect plan creation & design (Step 4)
- [x] Developer review (Step 5)
- [x] Implementation (Step 7)
- [x] Verification (Step 8) — Architect: VERIFIED, Requirements: SATISFIED
- [x] Documentation (Step 9) — Completed during implementation (removal task)
- [x] Completion (Step 10)

---

## Progress Log

### 2026-03-07
- Created todo
- Discovered RemoteOnly is referenced in 66 files across generator, tests, docs, examples, and skills
- Requirements review completed (APPROVED, no contradictions)
- Architect plan created: [Remove RemoteOnly Factory Mode Plan](../../plans/completed/remove-remoteonly-plan.md)
- Verified FactoryModeOption is a documentation error (not a real type)
- Identified 20 testable business rules and 12 test scenarios
- Developer review raised 2 blocking + 5 non-blocking concerns
- Architect addressed all 7 concerns:
  - Concern 1 (BLOCKING): Added OrderEntry example collapse (delete Domain.Client + Domain.Server, update BlazorClient/Server refs)
  - Concern 2 (BLOCKING): Corrected reference-app file count from 4 to 12
  - Concern 3: Corrected skill file count (only setup.md has content)
  - Concern 4: Documented InterfaceFactoryRenderer guard difference in Risks
  - Concern 5: Decided to collapse OrderEntry Domain.Client/Server into Domain
  - Concern 6: Explicitly documented FactoryText becomes parameterless
  - Concern 7: Acknowledged dual guard collapse (compile-time + runtime -> runtime-only)
- Confirmed RemoteFactory.TrimmingTests already validates IL Trimming as replacement
- Updated plan to 24 business rules and 16 test scenarios
- Developer re-review raised Concern 8 (BLOCKING): OrderEntry collapse structurally unsound -- `OrderEntry.Domain` is bare directory (no .csproj), domain files use `#if CLIENT` guards referencing OrderEntry.Ef types
- Architect resolved Concern 8 via Option (a): Create new `OrderEntry.Domain.csproj`, remove `#if CLIENT`/`#if !CLIENT` guards from domain files, add EF project reference. `[Service]` method-injection ensures EF is server-only at runtime; IL Trimming removes it at publish time.
- Updated plan to 26 business rules, 18 test scenarios, and 46 implementation steps
- Plan set to "Draft (Architect)" for developer re-review

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors (independently verified by architect)
- Tests: 0 failures — 478 unit + 476 integration + 29 design + 47 reference-app per TFM

---

## Results / Conclusions

The entire `FactoryMode` compile-time concept has been removed from RemoteFactory:

- **Generator simplified**: Removed `FactoryMode` enum, `FactoryModeAttribute`, `GetFactoryMode()`, and all conditional branching across 6 generator files. The Full-mode code paths are now unconditional.
- **Model cleaned**: Removed `Mode` property from `FactoryGenerationUnit`, `FactoryText`, and `TypeInfo`. Removed `mode` parameters from all renderer methods and builder calls.
- **Tests**: Deleted `RemoteOnlyTests` project (4 sub-projects). No test coverage lost — `IntegrationTests` and `TrimmingTests` already cover the same scenarios.
- **OrderEntry restructured**: Created new `OrderEntry.Domain.csproj` replacing `Domain.Client` and `Domain.Server`. Removed `#if CLIENT` conditional compilation guards. EF dependency handled via IL Trimming at publish time.
- **Design projects**: Deleted `Design.Client.Blazor/AssemblyAttributes.cs`. Updated `CLAUDE-DESIGN.md`.
- **Docs/Skills/Reference app**: Updated 7 published docs, 1 skill file, and 8+ reference-app files. All RemoteOnly/FactoryMode/FactoryModeOption references removed from active source.
- **Breaking change**: Requires major version bump. Migration: remove `[assembly: FactoryMode(FactoryMode.RemoteOnly)]`, use IL Trimming instead.
- **IL Trimming** (`NeatooRuntime.IsServerRuntime`) and **runtime modes** (`NeatooFactory` enum) are completely untouched.
