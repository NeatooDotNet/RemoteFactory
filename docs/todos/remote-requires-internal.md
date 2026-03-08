# [Remote] Requires Internal Methods

**Status:** In Progress
**Priority:** High
**Created:** 2026-03-08
**Last Updated:** 2026-03-08

---

## Problem

NF0105 currently enforces `[Remote]` + `internal` = error, requiring `[Remote]` methods to be `public`. This is backwards. `[Remote]` methods are never called directly by clients — clients call the generated factory. Making them `public` defeats IL trimming for no benefit, since the guard (`IsServerRuntime` throw) on a public method is pointless — the trimmer won't remove the method body.

## Solution

Flip NF0105: `[Remote]` + `public` = error (was `[Remote]` + `internal`). `[Remote]` methods must be `internal` so the trimmer can remove their bodies from client assemblies. The generated factory interface method remains `public` (client-callable) regardless of the source method's access modifier.

Target behavior:

| Source Method | `[Remote]` | Factory Interface | Guard | Client Trimmable |
|---|---|---|---|---|
| `public` | yes | **ERROR** (NF0105) | — | — |
| `public` | no | **public** | no | No |
| `internal` | yes | **public** | yes | **Yes** |
| `internal` | no | **internal** | yes | Yes |

Must verify trimming works on the Person example domain model library after the change.

---

## Clarifications

**Q1 (Architect):** Under the new regime, a class could have `[Remote] internal void Fetch(...)` and `[Create] internal void Create(...)`. Should `[Remote]` on an `internal` method effectively promote that method's factory interface visibility to `public`? i.e., "a method contributes a public interface member if it has `[Remote]` OR if it is `public`, and an internal interface member otherwise."

**A1 (from approved table):** Yes. This is exactly what the target behavior table specifies — row 3 (`internal` + `[Remote]` → factory interface `public`). `[Remote]` promotes the factory method to public because the client needs to call it through the factory. The source method stays internal for trimming.

Architect confirmed **Ready** after this clarification.

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-08
**Verdict:** APPROVED (with scope notes)

### Relevant Requirements Found

**1. Critical Rule 10 / Anti-Pattern 8: "[Remote] on internal methods is a contradiction"**

This is the rule the todo proposes to flip. It is documented in multiple locations:

- `src/Design/CLAUDE-DESIGN.md` Critical Rules section (Rule 10): _"[Remote] on internal methods is a contradiction -- emits diagnostic NF0105"_
- `src/Design/CLAUDE-DESIGN.md` Anti-Pattern 8: _"[Remote] marks a client-to-server entry point. internal means server-only. These are contradictory. The generator emits diagnostic error NF0105 and skips the method. Remove [Remote] if the method is server-only, or make it public if clients should call it."_
- `skills/RemoteFactory/references/anti-patterns.md` Anti-Pattern 11: Same wording, with solution _"Remove [Remote] or make method public"_
- `src/Design/Design.Domain/Entities/OrderLine.cs` line 89: Common mistake comment _"[Remote, Create] <-- WRONG: [Remote] + internal triggers NF0105"_
- `src/Generator/DiagnosticDescriptors.cs` line 55: _"Method '{0}' is marked [Remote] but has internal accessibility. [Remote] methods are client entry points and must be public."_
- `src/Generator/Builder/FactoryModelBuilder.cs` line 177: NF0105 check fires when `method.IsRemote && method.IsInternal`

The todo proposes **inverting** this rule: `[Remote] + internal` becomes the correct pattern; `[Remote] + public` becomes the error. This directly contradicts the current documented requirement. However, this is an **intentional requirement change** requested by the product owner, not an accidental contradiction. The purpose is to enable IL trimming on `[Remote]` method bodies, which the current requirement prevents.

**2. Critical Rule 2: Factory Method Visibility Controls Guard Emission and Trimming**

`src/Design/CLAUDE-DESIGN.md` documents the current decision table:

| Method Declaration | Guard Emitted? | Client Behavior | Trimmable? |
|---|---|---|---|
| `[Remote] public` | Yes | Routes to server via delegate fork | Yes (guarded) |
| `public` (no Remote) | No | Runs locally on client | No (always available) |
| `internal` (no Remote) | Yes | Throws if called when IsServerRuntime=false | Yes (guarded) |
| `[Remote] internal` | N/A | Diagnostic NF0105 -- contradiction | N/A |

The todo proposes replacing this table with:

| Source Method | `[Remote]` | Factory Interface | Guard | Client Trimmable |
|---|---|---|---|---|
| `public` | yes | ERROR (NF0105) | -- | -- |
| `public` | no | public | no | No |
| `internal` | yes | public | yes | Yes |
| `internal` | no | internal | yes | Yes |

Row 3 is new behavior: `[Remote] + internal` produces a **public** factory interface method (promoted from internal because the client needs to call through the factory). This is a semantic change to how factory interface visibility is determined -- currently, `[Remote]` does not affect interface member visibility (only method accessibility does).

**3. Factory Interface Visibility Rules**

`src/Design/CLAUDE-DESIGN.md` documents:

| Method Visibility | Generated Interface | Interface Members |
|---|---|---|
| All methods public | public interface IXxxFactory | All methods included |
| All methods internal | internal interface IXxxFactory | All methods included |
| Mix of public and internal | public interface IXxxFactory | All methods included; internal methods get internal modifier |

The todo introduces a new dimension: `[Remote]` on an `internal` method promotes that method's factory interface contribution to `public`. This means visibility is now determined by: if the method has `[Remote]`, treat it as public for interface purposes, even though the source method is `internal`. The existing rules for non-`[Remote]` methods remain unchanged.

**4. Design Debt: "Automatic [Remote] detection"**

`src/Design/CLAUDE-DESIGN.md` Design Debt table: _"Must be explicit. Security risk of accidental exposure. Reconsider When: Never -- explicit is a core principle."_

This todo does NOT propose automatic `[Remote]` detection. It keeps `[Remote]` explicit but changes which access modifier it pairs with. No conflict.

**5. IL Trimming Documentation**

`docs/trimming.md` documents the current pattern:

| Method Declaration | Guard? | Trimming Behavior |
|---|---|---|
| `[Remote] public` | Yes | Method body trimmed |
| `public` (no [Remote]) | No | Method body survives trimming |
| `internal` (no [Remote]) | Yes | Method body trimmed |

`skills/RemoteFactory/references/trimming.md` recommends: _"Use internal classes with public [Remote] entry points."_ And its table says aggregate root factory methods should be `public` with `[Remote]`.

Both will need updating to reflect the new pattern where `[Remote]` requires `internal`.

**6. Client-Server Architecture Documentation**

`docs/client-server-architecture.md` shows the pattern with `[Remote]` on `public` methods:
```csharp
[Remote, Create] public void Create(...) { }    // Client calls this
[Remote, Fetch]  public Task<bool> Fetch(...) { }  // Client calls this
```

And the visibility table:
| Declaration | Client Can Call? | Crosses Network? | Factory Interface |
|---|---|---|---|
| [Remote] public | Yes | Yes | Included in public interface |

All these will need updating.

**7. Existing Design Source of Truth Code**

Every `[Remote]` method in the Design projects and examples currently uses `public`:
- `Order.cs`: `[Remote, Create] public void Create(...)`, `[Remote, Fetch] public void Fetch(...)`, etc.
- `SecureOrder.cs`: All `[Remote]` methods are `public`
- `AllPatterns.cs`: `[Remote, Create] public Task Create(...)`, `[Remote, Fetch] public Task Fetch(...)`
- `ExampleCommands/ExampleEvents`: `[Remote, Execute] private static` and `[Remote, Event] private static` (static factory pattern -- different rules)
- `Person example`: `[Remote] [Fetch] public async Task<bool> Fetch(...)`, etc.

All class factory `[Remote]` methods will need to change from `public` to `internal`.

**8. Attributes Reference Documentation**

`docs/attributes-reference.md` shows `[Remote]` with `public` methods in every code example (snippets pulled from reference-app via MarkdownSnippets). The reference-app source code will need updating, followed by running mdsnippets.

**9. Common Mistakes Summary**

`src/Design/CLAUDE-DESIGN.md` item 9: _"[Remote] on internal methods -- Contradictory: [Remote] = client entry point, internal = server-only. Emits NF0105."_

This will need to be replaced with: "[Remote] on public methods -- Contradictory: [Remote] methods should be internal for trimming. Emits NF0105."

### Gaps

**G1. Static factory [Remote] methods are unaffected.**
Static factory `[Remote, Execute]` and `[Remote, Event]` methods use `private static` with underscore prefix. The todo's proposed table only covers class factory methods (instance methods with `public`/`internal`). The architect must confirm that the NF0105 change applies only to non-static methods, and that static factory patterns remain unchanged.

**G2. Constructor [Create] with [Remote] is not addressed.**
The current codebase has `[Create]` on constructors (e.g., `PersonModel()`) without `[Remote]`. If someone adds `[Remote]` to a constructor, the access modifier interaction is unclear. The current NF0105 check might not apply to constructors. The architect should confirm constructor behavior.

**G3. Entity duality with the new rule.**
The current entity duality pattern shows:
```csharp
[Remote, Fetch] public Task<bool> Fetch(...)        // Aggregate root context
[Fetch]         internal void FetchAsChild(...)      // Child context
```
Under the new rule, this becomes:
```csharp
[Remote, Fetch] internal Task<bool> Fetch(...)       // Aggregate root context -- promoted to public on interface
[Fetch]         internal void FetchAsChild(...)       // Child context -- stays internal on interface
```
Both methods are now `internal` on the source class. The generator must distinguish them by `[Remote]` presence to determine factory interface visibility. The architect should verify this distinction works correctly.

### Contradictions

No contradictions that would warrant a VETO. The todo is an **intentional, product-owner-approved change** to existing requirements. The current rules (`[Remote]` requires `public`, `[Remote] + internal` = NF0105) are being replaced, not accidentally violated.

The change is internally consistent: `[Remote]` will mean "client entry point that crosses to server, but the source method is server-only and trimmable." The generated factory interface method (which is what the client actually calls) remains `public`. The `internal` access modifier on the source method enables IL trimming of the method body. This resolves the tension identified in the todo's Problem statement.

### Recommendations for Architect

1. **Scope the NF0105 change carefully for static factories.** Confirm static factory patterns (`private static` methods with `[Remote, Execute]` and `[Remote, Event]`) are unaffected by this change. NF0105 should only apply to non-static class factory methods.

2. **Update the factory interface visibility rule.** The current rule is: method visibility determines interface member visibility. The new rule adds: `[Remote]` on an `internal` method promotes that method's interface contribution to `public`. Document this clearly in the plan.

3. **Comprehensive documentation update list.** The following documents and source files reference the old `[Remote] + public` pattern and must be updated:
   - `src/Design/CLAUDE-DESIGN.md` (Critical Rules, Anti-Patterns, Quick Decisions, Common Mistakes, all code examples)
   - `src/Design/Design.Domain/Aggregates/Order.cs` (all `[Remote]` methods)
   - `src/Design/Design.Domain/Aggregates/SecureOrder.cs` (all `[Remote]` methods)
   - `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs` (class factory `[Remote]` methods)
   - `src/Design/Design.Domain/FactoryPatterns/ClassFactoryWithExecute.cs`
   - `src/Design/Design.Domain/Entities/OrderLine.cs` (comments referencing the old rule)
   - `src/Design/Design.Domain/Services/CorrelationExample.cs` (class factory `[Remote]` methods)
   - `src/Generator/DiagnosticDescriptors.cs` (NF0105 message)
   - `src/Generator/Builder/FactoryModelBuilder.cs` (NF0105 condition logic)
   - `docs/trimming.md`
   - `docs/client-server-architecture.md`
   - `docs/attributes-reference.md` (pulls from reference-app via MarkdownSnippets)
   - `skills/RemoteFactory/references/trimming.md`
   - `skills/RemoteFactory/references/anti-patterns.md`
   - `skills/RemoteFactory/references/class-factory.md`
   - `skills/RemoteFactory/references/advanced-patterns.md`
   - `src/docs/reference-app/` (source for MarkdownSnippets -- all `[Remote]` methods in samples)
   - `src/Examples/Person/Person.DomainModel/PersonModel.cs`

4. **Person example verification.** The todo specifically requires verifying the Person example domain model library trims correctly after the change. The Person example has `internal class PersonModel` with `[Remote]` on `public` methods -- these must change to `internal` methods.

5. **Breaking change.** This is a breaking change for users. Any existing `[Remote] public` method will now trigger NF0105. The release notes must include a migration guide (change `public` to `internal` on all `[Remote]` methods).

---

## Plans

- [Remote Requires Internal Plan](../plans/remote-requires-internal.md)

---

## Tasks

- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3)
- [x] Architect plan creation & design (Step 4)
- [x] Developer review (Step 5) — Approved with 2 scope additions (integration test targets, reference-app)
- [ ] Implementation (Step 7)
- [ ] Verification (Step 8)
- [ ] Documentation (Step 9)

---

## Progress Log

### 2026-03-08
- Created todo from discussion about `[Remote]` + access modifier contradiction
- Identified that Person example has `internal class PersonModel` with `[Remote]` on `public` methods — trimming is defeated
- User confirmed proposed table: flip NF0105 so `[Remote]` requires `internal`, not `public`
- User emphasized verifying Person example domain model library trimming

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass
- [ ] Person example domain model library trims correctly

**Verification results:**
- Build: [Pending]
- Tests: [Pending]
- Trimming: [Pending]

---

## Results / Conclusions

