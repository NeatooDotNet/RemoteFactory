# PHASE-011 — Code Review

**Plan:** [../plans/011-hardening.md](../plans/011-hardening.md)
**Mode:** Per-plan, findings-only (veto-tier / callout-tier, no grade)
**Budget:** `deep`

**Note on the opt-in:** the plan header originally said `Code-review opt-in: No (… adds no
production code)`. That reason went stale mid-implementation — Amendment A1 changed
source-generator emission for **every `[Factory]` type in every consumer assembly**. Flipped
to Yes and reviewed as a production change.

**First attempt failed** (stalled, no output). Relaunched against the corrected post-gate
state, so this review covers both commits together.

---

## Round 1 — 2026-08-31

**Verdict: CLEAN — no veto-tier findings.** Five callouts; two changed shipped code.

The reviewer verified the logs were this run's rather than copies (008 records 758 unit
tests, 011 records 762; `Neatoo.Generator.dll` rebuilt in both solution passes) and traced
**every** reader of `MethodInfo.ReturnType` and `ExecuteDelegateModel.ReturnType` before
reaching a conclusion.

### Direction confirmed

Two things I could not have settled myself, both now on the record:

- **The model-level fix is the right asymmetry, not an inconsistency with PHASE-008.** That
  plan fixed at the emission site because the relay transform's `global::` strip was
  load-bearing for five diagnostics — a *transform* defect. PHASE-011's were *capture*
  defects, and `MethodInfo.ReturnType` is where capture happens. The emission-reachable
  consumers are exactly `InterfaceFactoryRenderer:572,590` and `StaticFactoryRenderer:99,208`;
  every other site is a model-to-model copy, and the class-factory leg builds return types
  from `ServiceTypeName` and never touches this string. **No consumer builds a delegate name,
  hint name, or registration key from it.**
- **`FullyQualifiedFormatWithNullable` is correct, and the delta is exactly `global::`.**
  `ITypeSymbol.ToString()` already carried nullable annotations — proven in-repo by
  `Transform.cs:88` stripping a trailing `?` and `RelayHandler.cs:317-318` comparing against
  the literal `"System.Threading.Tasks.Task"`. Plain `FullyQualifiedFormat` would have
  silently dropped annotations; the `WithNullable` variant preserves prior behavior.

The reviewer also independently re-verified the `Clear()` deletion (six IVT assemblies, zero
call sites) and judged the A2/A3 scoping legitimate rather than a test bent to fit the code —
"measure, remove the decoy, record the measurement, queue row 013" is the disposition
CLAUDE.md's *Never Work Around Production Bugs in Tests* explicitly sanctions.

### Callouts

| # | Finding | Disposition |
|---|---------|-------------|
| **C1** | **RP-2's recorded mechanism was false.** The sync `[Execute]` added to close gate S1 is an **NF0102 error** and is `continue`d out of the model before any delegate is built — it never reaches the renderer. The log attributed RP-2's green to `StaticFactoryRenderer:99` making both shapes "converge"; they never converge. The fixture also didn't notice, because `AssertShadowedOutputCompiles` discards the generator-diagnostic tuple element and asserts only on `outputCompilation.GetDiagnostics()`, which excludes generator-reported diagnostics. The wrong mechanism had propagated to four places. | **Fixed.** `_DoWorkSync` removed (a construct no consumer could ship, emitting NF0102 inside a fixture whose job is to compile cleanly). The comment is deleted rather than corrected, because its conclusion was *narrower* than claimed: the non-`Task` line is dead **on the `[Execute]` path specifically**; whether it is reachable via non-`Task` interface methods is an **open question**, now recorded as such. |
| **C2** | **A consumer-visible regression this plan introduced.** `MethodInfo.ReturnType` is NF0102's second message argument, so `[Execute] public static Payload Run(…)` began reporting `not 'global::MyApp.Payload'`. Unpinned, because the existing NF0102 fixture returns `string` — a special type that renders identically under both formats. | **Fixed + pinned.** `ForDiagnosticMessage()` strips the prefix for the message only, at both sites; emission keeps the qualified form. New pin `NF0102_NamesTheReturnTypeWithoutTheGlobalPrefix` uses a **consumer** type. Measured as RP-7: exactly 1 red, sole coverage. |
| **C3** | *"MINIMALLY qualified"* is the wrong term, in the durable emission-site comment and three other places. `ToString()` renders **namespace-qualified without `global::`**; `MinimallyQualifiedFormat` would give bare `Payload` — and a bare `Payload` inside `namespace TestNamespace` binds to the **correct** type, so the documented CS0029 could not have occurred. | **Fixed** in the source comment, RP-0, and the plan. The reviewer's framing is why it mattered: that comment is what the next author will use to judge whether another `ToString()` site is safe. |
| **C4** | `FullyQualifiedFormatWithNullable`'s own XML doc still said it is "used for property type extraction," now false. | **Fixed** — it names the return-type role and warns that widening/narrowing it affects emission for every `[Factory]` type. |
| **C5** | The plan justified the `Clear()` removal by citing the `Internal` **namespace** policy. `FactoryEventHandlerRegistry` is in `Neatoo.RemoteFactory`, not `…Internal`, so that policy does not reach it. Removal still correct on the independent ground the Constraints state. | **Amended, not swapped.** The bullet is struck through with the correct reasoning beside it, so the wrong authority stays visible. |

### Flagged forward, not actioned

- **Step 7B check, not a Step 5 finding:** this commit changes generator emission for every
  `[Factory]` type with no Design-project change. The reviewer judged that non-applicable here
  (no API or observable pattern a Design example could demonstrate) but asked that the
  close-out **confirm** rather than inherit that judgment. Carried to Step 7.
- A latent false positive was **removed** in passing: `InterfaceFactoryRenderer:582` tests
  `!returnType.StartsWith("Task")`, so a return type in a namespace beginning with `Task`
  previously skipped the wrapper. `global::` prefixing makes it fail correctly.
- `Types.cs:692`'s `IsBool` substring match — already recorded by the test gate; not re-raised.

### Suites at close

Unit **763×2 TFMs** (762 → 763, +1 for the C2 pin), integration **595×2 (+5 standing
skips)**, Design **98×2**. Both solutions built, 0 errors.

### Worth carrying forward

**C1 is the arc's signature failure mode found in the one place meant to prevent it.** The
red-proof log exists so that "would go red" claims are measured rather than asserted — and it
carried a confident, unmeasured mechanism for a green result. Fifth instance across the arc,
and the first located in a log entry rather than in a test. The tell is unchanged: a causal
sentence explaining a measurement, written without measuring the explanation.
