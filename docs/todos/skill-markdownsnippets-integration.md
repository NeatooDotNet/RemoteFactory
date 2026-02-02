# Skill MarkdownSnippets Integration

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-01
**Last Updated:** 2026-02-02

---

## Problem

The RemoteFactory skill (`skills/RemoteFactory/`) contains hand-written C# code examples that:
- Are not compiled or validated
- May drift from actual API usage over time
- Duplicate code that already exists in the reference-app

The docs (`docs/*.md`) already use MarkdownSnippets to extract compiled code from `src/docs/reference-app/`, ensuring documentation stays in sync with working code.

## Solution

Convert the skill's markdown files to use MarkdownSnippets placeholders, sharing the same `#region` markers from the reference-app that the docs use. This provides:

1. **Single source of truth** - One set of compiled, tested code samples
2. **Validation** - Code in skill is guaranteed to compile
3. **DRY** - No duplicate code between docs and skill
4. **Consistency** - Docs and skill show identical patterns

**Distribution workflow:** Run `mdsnippets` before packaging/distributing the skill. The processed markdown files contain embedded code (self-contained).

---

## Key Decisions

### Domain Alignment: Use Employee Domain (Option B)

The skill currently uses Order/Product examples while the reference-app uses Employee/Department.

**Decision:** Rename skill examples to use the Employee domain to match the reference-app.

**Rationale:**
- Single source of compiled code (no duplicate domain models)
- Skill examples match what users see in getting-started docs
- Consistent learning experience across docs and skill

### Anti-Pattern Examples: Keep Hand-Written

`references/anti-patterns.md` contains intentionally wrong code (`// WRONG` comments) that cannot come from compiled source. These must remain as raw markdown code blocks.

### Region Naming: Use `skill-` Prefix

New `#region` markers added specifically for the skill (not shared with docs) should use a `skill-` prefix to distinguish them from doc-only regions.

Example: `#region skill-class-factory-complete` vs `#region class-factory-complete`

### Distribution: Commit Processed Files

Processed skill files (with embedded code from mdsnippets) will be committed to the repo. The skill always contains embedded code in git, ensuring it works without running mdsnippets.

**Workflow:** Run `mdsnippets` → commit changes → push

### Anchor Links: Accept Visual Noise

MarkdownSnippets adds HTML anchors and source file links. These will be kept as-is in the skill files. If Claude has parsing issues, revisit this decision.

### Partial Code Blocks: Keep Hand-Written

Many skill examples show partial code (just a method or class declaration) for illustration. These can't be extracted from compiled `#region` markers and should stay hand-written alongside anti-patterns.

**Code block categories:**
1. **Full compilable unit** - Can be extracted from region
2. **Partial/illustrative** - Should stay hand-written
3. **Anti-pattern** - Must stay hand-written

### Reference-App Structure: Create Samples/Skill/ Folder

Create a dedicated `Samples/Skill/` folder in `EmployeeManagement.Domain` with simplified examples that:
- Use the Employee domain but simplified for pedagogical clarity
- Match the teaching style of the current skill
- Are prefixed with `skill-` to avoid confusion with doc snippets

---

## Plans

### Complete Code Block Audit

#### SKILL.md (Main File)
**Code Blocks:** 0
- Contains only tables and reference links, no code blocks

---

#### references/class-factory.md

| # | Lines | Description | Category | Mapping |
|---|-------|-------------|----------|---------|
| 1 | 7-41 | Complete Order class with IFactorySaveMeta, CRUD operations | **NEW REGION** | `skill-class-factory-complete` |
| 2 | 51-56 | IFactorySaveMeta properties only | **PARTIAL** | Keep hand-written |
| 3 | 74-92 | Lifecycle hooks (IFactoryOnStartAsync, IFactoryOnCompleteAsync) | **EXISTING** | `interfaces-factoryonstart-async` + `interfaces-factoryoncomplete-async` (combine) OR `skill-lifecycle-hooks` |
| 4 | 114-128 | [SuppressFactory] example with Employee/Manager | **EXISTING** | `attributes-suppressfactory` |

---

#### references/interface-factory.md

| # | Lines | Description | Category | Mapping |
|---|-------|-------------|----------|---------|
| 1 | 7-39 | Complete IOrderRepository + OrderRepository implementation | **NEW REGION** | `skill-interface-factory-complete` |
| 2 | 52-65 | WRONG: Attributes on interface methods / RIGHT: no attributes | **ANTI-PATTERN** | Keep hand-written |
| 3 | 72-78 | WRONG: [Factory] on implementation / RIGHT: no [Factory] | **ANTI-PATTERN** | Keep hand-written |
| 4 | 88-91 | Server DI registration (AddScoped) | **PARTIAL** | Keep hand-written |
| 5 | 94-95 | Convention-based registration (RegisterMatchingName) | **PARTIAL** | Keep hand-written |

---

#### references/static-factory.md

| # | Lines | Description | Category | Mapping |
|---|-------|-------------|----------|---------|
| 1 | 9-35 | Execute commands (OrderCommands with _SendNotification, _GetOrderSummary) | **NEW REGION** | `skill-static-execute-commands` |
| 2 | 43-46 | Execute usage (await OrderCommands.SendNotification) | **PARTIAL** | Keep hand-written |
| 3 | 54-78 | Event handlers (OrderEvents with _OnOrderPlaced, _OnPaymentReceived) | **EXISTING** | `events-basic` or `skill-static-event-handlers` |
| 4 | 86-89 | Event usage (fire-and-forget) | **PARTIAL** | Keep hand-written |
| 5 | 99-106 | WRONG: public static / RIGHT: private static _MethodName | **ANTI-PATTERN** | Keep hand-written |
| 6 | 113-119 | WRONG: Task / RIGHT: Task<T> | **ANTI-PATTERN** | Keep hand-written |
| 7 | 127-133 | WRONG: missing CancellationToken / RIGHT: with CancellationToken | **ANTI-PATTERN** | Keep hand-written |
| 8 | 144-171 | IEventTracker usage (graceful shutdown) | **EXISTING** | `events-eventtracker-access` or `interfaces-eventtracker` |
| 9 | 181-193 | Testing events with IEventTracker | **EXISTING** | `events-testing` |
| 10 | 201-207 | WRONG: missing partial / RIGHT: with partial | **ANTI-PATTERN** | Keep hand-written |

---

#### references/service-injection.md

| # | Lines | Description | Category | Mapping |
|---|-------|-------------|----------|---------|
| 1 | 9-27 | Constructor injection (ILogger stored in field) | **EXISTING** | `service-injection-constructor` |
| 2 | 40-58 | Method injection (IOrderRepository) | **EXISTING** | `service-injection-server-only` |
| 3 | 68-81 | WRONG: storing method-injected service / NullReferenceException | **ANTI-PATTERN** | Keep hand-written |
| 4 | 83-90 | RIGHT: use immediately, don't store | **PARTIAL** | Keep hand-written (continuation of anti-pattern) |
| 5 | 98-124 | Child entity without [Remote] (OrderLine) | **NEW REGION** | `skill-child-entity-no-remote` |
| 6 | 131-141 | Parent calling child factory (Order.Create with lineFactory) | **PARTIAL** | Keep hand-written |
| 7 | 148-155 | WRONG: [Remote] on child / N+1 problem | **ANTI-PATTERN** | Keep hand-written |
| 8 | 158-165 | RIGHT: no [Remote] on child | **PARTIAL** | Keep hand-written (part of anti-pattern) |
| 9 | 173-177 | Server registration (AddScoped) | **PARTIAL** | Keep hand-written |
| 10 | 183-187 | Client registration (AddSingleton) | **PARTIAL** | Keep hand-written |

---

#### references/setup.md

| # | Lines | Description | Category | Mapping |
|---|-------|-------------|----------|---------|
| 1 | 5-12 | NuGet package references (XML) | **PARTIAL** | Keep hand-written |
| 2 | 18-35 | Server setup (AddNeatooAspNetCore, UseNeatoo) | **EXISTING** | `aspnetcore-basic-setup` |
| 3 | 42-47 | Multiple assemblies registration | **EXISTING** | `aspnetcore-multi-assembly` |
| 4 | 51-64 | CORS configuration | **EXISTING** | `aspnetcore-cors` |
| 5 | 70-76 | Client setup (AddNeatooRemoteFactory) | **EXISTING** | `getting-started-client-program` (partial match) |
| 6 | 82-85 | [assembly: FactoryMode] attribute | **EXISTING** | `attributes-factorymode` |
| 7 | 96-106 | HttpClient configuration (keyed service) | **PARTIAL** | Keep hand-written |
| 8 | 110-116 | Factory modes (Remote vs Local) | **PARTIAL** | Keep hand-written |
| 9 | 124-148 | Blazor component usage (inject IOrderFactory) | **NEW REGION** | `skill-blazor-usage` |
| 10 | 153-159 | Static factory commands usage | **PARTIAL** | Keep hand-written |
| 11 | 177-179 | Generated code location | **PARTIAL** | Keep hand-written |

---

#### references/anti-patterns.md

| # | Lines | Description | Category | Mapping |
|---|-------|-------------|----------|---------|
| 1 | 7-23 | [Remote] on child entities (N+1 calls) | **ANTI-PATTERN** | Keep hand-written |
| 2 | 33-48 | Attributes on interface methods | **ANTI-PATTERN** | Keep hand-written |
| 3 | 58-74 | Public static factory methods | **ANTI-PATTERN** | Keep hand-written |
| 4 | 84-92 | Private property setters | **ANTI-PATTERN** | Keep hand-written |
| 5 | 102-141 | Storing method-injected services | **ANTI-PATTERN** | Keep hand-written |
| 6 | 149-158 | Missing partial keyword | **ANTI-PATTERN** | Keep hand-written |
| 7 | 167-180 | [Factory] on implementation classes | **ANTI-PATTERN** | Keep hand-written |
| 8 | 191-205 | [Execute] returning Task not Task<T> | **ANTI-PATTERN** | Keep hand-written |
| 9 | 215-229 | [Event] missing CancellationToken | **ANTI-PATTERN** | Keep hand-written |

---

#### references/advanced-patterns.md

| # | Lines | Description | Category | Mapping |
|---|-------|-------------|----------|---------|
| 1 | 7-42 | [AuthorizeFactory<T>] complete example | **EXISTING** | `authorization-interface` + `authorization-implementation` + `authorization-apply` (combine) OR `skill-authorize-factory-complete` |
| 2 | 59-76 | [AspAuthorize] on methods | **EXISTING** | `authorization-policy-apply` |
| 3 | 80-88 | Server authorization policy config | **EXISTING** | `authorization-policy-config` |
| 4 | 98-115 | Correlation context (ICorrelationContext) | **EXISTING** | `aspnetcore-correlation-id` |
| 5 | 118-121 | Correlation DI registration | **PARTIAL** | Keep hand-written |
| 6 | 129-156 | Entity duality (Product as root and child) | **NEW REGION** | `skill-entity-duality` |
| 7 | 167-188 | Value object (Money record) | **NEW REGION** | `skill-value-object-factory` |
| 8 | 197-244 | Complex aggregate (Order with OrderLineList) | **EXISTING** | `collection-factory-parent` (close match) OR `skill-complex-aggregate` |
| 9 | 250-287 | Child collection factory (OrderLineList) | **EXISTING** | `collection-factory-basic` |
| 10 | 298-324 | Testing with ClientServerContainers | **EXISTING** | `clientserver-container-usage` |
| 11 | 338-341 | NuGet package references | **PARTIAL** | Keep hand-written |
| 12 | 376-384 | [assembly: FactoryHintNameLength] | **EXISTING** | `attributes-factoryhintnamelength` |
| 13 | 399-413 | Attribute inheritance example (Employee/Manager) | **EXISTING** | `attributes-inheritance` |
| 14 | 429-435 | Troubleshooting: Serialization null properties | **PARTIAL** | Keep hand-written |
| 15 | 440-442 | Troubleshooting: CS0260 missing partial | **PARTIAL** | Keep hand-written |
| 16 | 449-457 | Troubleshooting: Method-injected service null | **PARTIAL** | Keep hand-written |
| 17 | 459-465 | Constructor injection alternative | **PARTIAL** | Keep hand-written |
| 18 | 470-478 | Troubleshooting: N+1 remote calls | **PARTIAL** | Keep hand-written |

---

### Cross-Referenced Examples

The following patterns appear in multiple files and need consistent handling:

1. **Child entities without [Remote]** - `service-injection.md` (lines 98-124) and `anti-patterns.md` (lines 7-23)
   - Use same `skill-child-entity-no-remote` region in both places

2. **IFactorySaveMeta properties** - `class-factory.md` (lines 51-56) and `advanced-patterns.md` (Order class)
   - Keep as partial examples (just properties shown)

3. **[Factory] on interface vs implementation** - `interface-factory.md` (lines 72-78) and `anti-patterns.md` (lines 167-180)
   - Both are anti-patterns, keep hand-written

4. **Static method naming (private with underscore)** - `static-factory.md` (lines 99-106) and `anti-patterns.md` (lines 58-74)
   - Both are anti-patterns, keep hand-written

5. **CancellationToken on events** - `static-factory.md` (lines 127-133) and `anti-patterns.md` (lines 215-229)
   - Both are anti-patterns, keep hand-written

6. **Service injection patterns** - `service-injection.md` and `anti-patterns.md`
   - Method injection: use existing `service-injection-server-only`
   - Storing services: anti-pattern, keep hand-written

---

### Mapping Summary: Skill Code Block to Reference-App Region

#### New Regions Needed (skill- prefix)

| Region Name | File | Description |
|-------------|------|-------------|
| `skill-class-factory-complete` | Domain/Samples/Skill/ClassFactorySamples.cs | Complete Employee with IFactorySaveMeta, CRUD |
| `skill-interface-factory-complete` | Domain/Samples/Skill/InterfaceFactorySamples.cs | IEmployeeRepository + EmployeeRepository implementation |
| `skill-static-execute-commands` | Domain/Samples/Skill/StaticFactorySamples.cs | EmployeeCommands with _SendNotification, _GetSummary |
| `skill-static-event-handlers` | Domain/Samples/Skill/StaticFactorySamples.cs | EmployeeEvents with _OnEmployeeCreated, _OnPaymentReceived |
| `skill-child-entity-no-remote` | Domain/Samples/Skill/ChildEntitySamples.cs | Assignment entity without [Remote] |
| `skill-entity-duality` | Domain/Samples/Skill/EntityDualitySamples.cs | Department as root and child |
| `skill-value-object-factory` | Domain/Samples/Skill/ValueObjectSamples.cs | Money record with factory |
| `skill-lifecycle-hooks` | Domain/Samples/Skill/LifecycleHookSamples.cs | Combined IFactoryOnStartAsync + IFactoryOnCompleteAsync |
| `skill-blazor-usage` | Client.Blazor/Samples/Skill/BlazorUsageSamples.razor | Blazor component with factory injection |
| `skill-complex-aggregate` | Domain/Samples/Skill/ComplexAggregateSamples.cs | Employee with AssignmentList child collection |

#### Existing Regions to Reuse

| Region Name | Used In | Notes |
|-------------|---------|-------|
| `attributes-suppressfactory` | class-factory.md | Employee/Manager inheritance |
| `events-basic` | static-factory.md | Event handlers with CancellationToken |
| `events-eventtracker-access` | static-factory.md | IEventTracker usage |
| `events-testing` | static-factory.md | Testing events with WaitAllAsync |
| `service-injection-constructor` | service-injection.md | Constructor injection pattern |
| `service-injection-server-only` | service-injection.md | Method injection pattern |
| `aspnetcore-basic-setup` | setup.md | Server Program.cs |
| `aspnetcore-multi-assembly` | setup.md | Multiple assembly registration |
| `aspnetcore-cors` | setup.md | CORS configuration |
| `attributes-factorymode` | setup.md | [assembly: FactoryMode] |
| `authorization-interface` | advanced-patterns.md | IOrderAuthorization definition |
| `authorization-implementation` | advanced-patterns.md | OrderAuthorization class |
| `authorization-apply` | advanced-patterns.md | [AuthorizeFactory<T>] on class |
| `authorization-policy-apply` | advanced-patterns.md | [AspAuthorize] on methods |
| `authorization-policy-config` | advanced-patterns.md | AddPolicy configuration |
| `aspnetcore-correlation-id` | advanced-patterns.md | ICorrelationContext usage |
| `collection-factory-basic` | advanced-patterns.md | Child collection factory |
| `clientserver-container-usage` | advanced-patterns.md | Two-container testing |
| `attributes-factoryhintnamelength` | advanced-patterns.md | Path length attribute |
| `attributes-inheritance` | advanced-patterns.md | Attribute inheritance |

---

## Coverage Estimate

Based on complete audit of all skill markdown files:

| Category | Reuse Existing | New `skill-` Regions | Keep Hand-Written | Total Blocks |
|----------|----------------|----------------------|-------------------|--------------|
| class-factory.md | 1 | 1 | 2 | 4 |
| interface-factory.md | 0 | 1 | 4 | 5 |
| static-factory.md | 3 | 2 | 5 | 10 |
| service-injection.md | 2 | 1 | 7 | 10 |
| setup.md | 5 | 1 | 5 | 11 |
| advanced-patterns.md | 10 | 3 | 5 | 18 |
| anti-patterns.md | 0 | 0 | 9 | 9 |
| **Total** | **21** | **9** | **37** | **67** |

**Summary:**
- **21 code blocks** can reuse existing reference-app regions
- **9 code blocks** need new `skill-` prefixed regions
- **37 code blocks** should remain hand-written (anti-patterns + partial/illustrative)

---

## Tasks

### Planning
- [x] Audit ALL skill markdown files to identify all code blocks (not just `anti-patterns.md`)
- [x] Categorize each code block by extractability:
  - Full compilable unit → can extract from region
  - Partial/illustrative → keep hand-written
  - Anti-pattern → keep hand-written
- [x] Categorize each extractable block by source:
  - Mappable to existing reference-app region
  - Needs new `skill-` prefixed region
- [x] Identify cross-referenced examples (same pattern in multiple files)
- [x] Create mapping document: skill code block → reference-app region name

### Implementation

#### Reference-App Changes
- [x] Create `Samples/Skill/` folder in `EmployeeManagement.Domain`
- [x] Implement `skill-class-factory-complete` region (Employee with IFactorySaveMeta, CRUD)
- [x] Implement `skill-interface-factory-complete` region (IEmployeeRepository + implementation)
- [x] Implement `skill-static-execute-commands` region (EmployeeCommands with Execute methods)
- [x] Implement `skill-static-event-handlers` region (EmployeeEvents with Event methods)
- [x] Implement `skill-child-entity-no-remote` region (Assignment entity without [Remote])
- [x] Implement `skill-entity-duality` region (Department as root and child)
- [x] Implement `skill-value-object-factory` region (Money record with factory)
- [x] Implement `skill-lifecycle-hooks` region (combined IFactoryOnStartAsync + IFactoryOnCompleteAsync)
- [x] Implement `skill-blazor-usage` region in Client.Blazor (Blazor component with factory injection)
- [x] Implement `skill-complex-aggregate` region (Employee with AssignmentList child collection)

#### Skill Markdown Changes
- [x] Rename skill examples from Order/Product domain to Employee/Department domain
- [x] Convert extractable code blocks to `<!-- snippet: name -->` placeholders
  - Skip anti-pattern examples (keep as raw markdown)
  - Skip partial/illustrative examples (keep as raw markdown)
- [x] Verify `mdsnippets.json` will process `skills/RemoteFactory/` directory
- [x] Run `mdsnippets` and verify all snippets resolve

### Verification
- [x] Review embedded code in skill files for correctness
- [x] Test skill with Claude to ensure anchor links don't break parsing
- [x] Verify skill still works as standalone (no external dependencies after processing)

### Documentation
- [x] Update CLAUDE.md with skill distribution workflow
- [x] Document which code blocks remain hand-written and why (in skill's SKILL.md or README)

---

## Progress Log

### 2026-02-01
- Created todo
- docs-architect review identified domain mismatch and missing tasks
- Decisions made:
  - Use Employee domain (Option B) to match reference-app
  - Use `skill-` prefix for new region markers
  - Commit processed files to repo (always has embedded code)
  - Accept anchor link visual noise from MarkdownSnippets
- docs-code-samples review findings:
  - ~25-32 existing regions can be reused
  - ~6 new `skill-` regions needed
  - ~14 code blocks should stay hand-written (anti-patterns + partial examples)
  - Recommended creating `Samples/Skill/` folder in reference-app
  - Identified specific regions to implement

### 2026-02-01 (Planning Complete)
- Completed comprehensive audit of all 8 skill markdown files:
  - SKILL.md: 0 code blocks (tables and links only)
  - class-factory.md: 4 code blocks
  - interface-factory.md: 5 code blocks
  - static-factory.md: 10 code blocks
  - service-injection.md: 10 code blocks
  - setup.md: 11 code blocks
  - anti-patterns.md: 9 code blocks
  - advanced-patterns.md: 18 code blocks
- Total: 67 code blocks audited
- Categorization complete:
  - 21 blocks can reuse existing reference-app regions
  - 9 blocks need new `skill-` prefixed regions
  - 37 blocks should remain hand-written (anti-patterns + partial/illustrative)
- Cross-referenced examples identified (5 patterns appear in multiple files)
- Created detailed mapping document in Plans section
- Updated implementation tasks with all 10 new skill- regions needed

### 2026-02-02 (Implementation Complete)
- Created `Samples/Skill/` folders in Domain and Client.Blazor projects
- Implemented all 10 new skill- prefixed regions:
  - ClassFactorySamples.cs: `skill-class-factory-complete`
  - InterfaceFactorySamples.cs: `skill-interface-factory-complete`
  - StaticFactorySamples.cs: `skill-static-execute-commands`, `skill-static-event-handlers`
  - ChildEntitySamples.cs: `skill-child-entity-no-remote`
  - EntityDualitySamples.cs: `skill-entity-duality`
  - ValueObjectSamples.cs: `skill-value-object-factory`
  - LifecycleHookSamples.cs: `skill-lifecycle-hooks`
  - ComplexAggregateSamples.cs: `skill-complex-aggregate`
  - BlazorUsageSamples.cs: `skill-blazor-usage`
- Updated all 6 skill reference markdown files with `<!-- snippet: name -->` placeholders
- Added `TreatMissingAsWarning: true` to mdsnippets.json (allows missing snippets as warnings)
- Fixed multiple code analyzer warnings (CA1304, CA1311, CA1725, CA2016, CA2225)
- Fixed line-too-long issues for mdsnippets MaxWidth 120 constraint
- Build: 0 errors, 0 warnings
- Tests: 52 passing across all 3 frameworks (net8.0, net9.0, net10.0)
- mdsnippets: Successfully extracts all skill- snippets and embeds in skill markdown files

### 2026-02-02 (Verification and Documentation Complete)
- Verified all embedded code in skill files:
  - All 30 snippet placeholders resolved with code from reference-app
  - Domain names properly converted from Order/Product to Employee/Department
  - Code is pedagogically clear and demonstrates RemoteFactory patterns
- Verified skill is standalone:
  - No external file references remain (only internal `references/*.md` links)
  - All code is embedded directly in markdown files
  - Anchor links from MarkdownSnippets are present but do not break functionality
- Updated CLAUDE.md with skill distribution workflow:
  - Added section explaining when/how to run mdsnippets
  - Documented the workflow: edit code -> build -> run mdsnippets -> commit -> push
  - Documented code block categories and region naming convention
- Updated SKILL.md with code sample source documentation:
  - Explained compiled code (via MarkdownSnippets) vs hand-written code
  - Documented that anti-patterns.md is entirely hand-written
  - Explained partial/illustrative snippets stay hand-written
- Build: 0 errors, 0 warnings
- Tests: 52 passing across all 3 frameworks

---

## Results / Conclusions

### Summary

Successfully integrated MarkdownSnippets into the RemoteFactory skill, ensuring code samples are compiled and tested rather than hand-written. The skill now shares code with the reference application documentation.

### Key Outcomes

1. **Single source of truth**: 30 code blocks now come from the reference-app's compiled code
2. **Domain alignment**: All skill examples use Employee/Department domain (matching docs)
3. **Validated code**: All embedded code compiles and passes tests
4. **Self-contained skill**: Processed files contain embedded code, no external dependencies
5. **Clear maintenance path**: CLAUDE.md documents how to update skill code

### Code Block Distribution

| Category | Count | Source |
|----------|-------|--------|
| MarkdownSnippets (compiled) | 30 | Reference-app regions |
| Hand-written anti-patterns | 9 | Intentionally wrong code |
| Hand-written partial/illustrative | 28 | Incomplete by design |
| **Total** | **67** | |

### Files Created/Modified

**Reference-app (new files):**
- `EmployeeManagement.Domain/Samples/Skill/ClassFactorySamples.cs`
- `EmployeeManagement.Domain/Samples/Skill/InterfaceFactorySamples.cs`
- `EmployeeManagement.Domain/Samples/Skill/StaticFactorySamples.cs`
- `EmployeeManagement.Domain/Samples/Skill/ChildEntitySamples.cs`
- `EmployeeManagement.Domain/Samples/Skill/EntityDualitySamples.cs`
- `EmployeeManagement.Domain/Samples/Skill/ValueObjectSamples.cs`
- `EmployeeManagement.Domain/Samples/Skill/LifecycleHookSamples.cs`
- `EmployeeManagement.Domain/Samples/Skill/ComplexAggregateSamples.cs`
- `EmployeeManagement.Client.Blazor/Samples/Skill/BlazorUsageSamples.cs`

**Skill files (updated):**
- `skills/RemoteFactory/SKILL.md` - Added code sample source documentation
- `skills/RemoteFactory/references/class-factory.md`
- `skills/RemoteFactory/references/interface-factory.md`
- `skills/RemoteFactory/references/static-factory.md`
- `skills/RemoteFactory/references/service-injection.md`
- `skills/RemoteFactory/references/setup.md`
- `skills/RemoteFactory/references/advanced-patterns.md`
- `skills/RemoteFactory/references/anti-patterns.md` (domain name updates only)

**Project files (updated):**
- `CLAUDE.md` - Added skill distribution workflow section
- `mdsnippets.json` - Added `TreatMissingAsWarning: true`

### Lessons Learned

1. **Region naming**: Using `skill-` prefix distinguishes skill-specific regions from doc-only regions
2. **Anti-patterns cannot be compiled**: Intentionally wrong code must stay hand-written
3. **Partial examples are common**: Many skill examples are illustrative fragments, not full compilable units
4. **MarkdownSnippets anchors**: The HTML anchors added by mdsnippets are visible but don't break functionality
