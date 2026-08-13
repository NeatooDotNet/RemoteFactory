# TRIM-008 — Plan Review

**Date:** 2026-08-12
**Reviewed:** `plans/008-registrar-dam-over-preservation.md` (Draft)
**Branch:** `TRIM-008-registrar-dam-over-preservation` at `06ea1f5`, working tree dirty with the Step-6 measurement spike
**Verdict:** **CONCERNS** — 5 veto-tier, 8 callout-tier

Two earlier attempts at this review died on API errors mid-response. The second left one usable lead (non-`[Remote]` `[Execute]` regression), which was chased down separately and ruled out — see below.

---

## Scope: split NOT recommended

The reviewer explicitly rejected splitting the plan. The doc pass is inseparable — the docs are false only while the code is broken, so a separately-mergeable doc plan would either publish a lie or publish a correction to code that has not landed. B9 closure is the *evidence* for the fix, not adjacent work. Nine steps is inside the cap.

Flagged instead: Steps 8 (docs) and 9 (container) are the parts most likely to be under-executed at the end of a long plan, and are exactly what the close-out audit must re-verify.

---

## Veto-Tier

### B1 — New harness targets get no pre-fix baseline; "absent" would prove nothing

Steps run fix (1–2) → tests (4) → **add targets (5)** → measure (6). The relay-handler, interface-factory, and Save/Can* targets **do not exist yet**, so the first and only trimmed measurement of their markers happens with the fix already applied. An "absent" result is then satisfied equally by *"the fix removed it"* and by *"the marker was never rooted in the first place."*

This is the arc's signature failure for the third time: TRIM-001's gate caught a harness that passed with emission disabled; the close-out audit's V1 caught a fixture whose guarded collections were never constructed.

**Accepted.** Step order changes: targets land and get a pre-fix probe **before** the leg that fixes them. Each new marker must have a recorded present-before / absent-after pair.

Note the static leg is unaffected — its baseline was captured against pre-existing targets (`_DoWork`, `_ProcessRecord`), so that measurement stands.

### B2 — Nothing asserts registration still works, and the runtime fails silently

`AddRemoteFactoryServices.cs:168-170` uses `method?.Invoke(...)`. A holder whose forwarding method is missing or misnamed produces **no diagnostic and no exception** — registration simply stops for that type and surfaces later as an unrelated DI failure. The change introduces a new name-coupling (generated holder ↔ the hard-coded `"FactoryServiceRegistrar"` string) that did not previously exist.

Every Acceptance bullet is an *absence* or *generated-text* assertion, and **absence assertions pass more easily when registration is dead.** The counter-signal is missing.

Partial mitigation exists but was unnamed: `TrimmingTests/Program.cs:83,133` resolves `TrimTestCommands.DoWork` and the harness exits non-zero on failure, so the **static leg has a real positive control**. The **relay leg structurally cannot** have one in the trimmed harness — `RelayHandlerRenderer.cs:82` wraps every `RegisterHandler` in `if (NeatooRuntime.IsServerRuntime)`, so on a client publish there is nothing to resolve. Relay registration correctness rests on the untrimmed integration suite, which is nowhere stated and is known-flaky (deferred item 10).

**Accepted.** Adding a registration-works Acceptance bullet naming the signal for each leg, and a Constraint pinning the holder method name.

### B3 — Byte-identity evidence has a nonzero expected delta and no partition

TRIM-006's diff was conclusive because the expected delta was **zero**. Here the static and relay legs change by design, so the diff *will* be nonzero, and the plan gives no way to distinguish "only the two legs moved" from "the two legs moved and something drifted with them." As written the evidence degrades to eyeballing a nonzero diff — the same non-discriminating class the close-out audit vetoed as V2.

The property itself is achievable: `FactoryRenderer.Render` dispatch, `CleanupSource`, and `NormalizeWhitespace` are untouched shared code, and each remaining leg has its own renderer.

**Accepted.** Evidence becomes an *expected-delta-set equality* check — enumerate the files expected to change, assert the actual set equals it.

### A1 — `src/Design/` is absent from the plan, and Step 8 has no bucket for "true today, false after the fix"

Repo `CLAUDE.md` names the Design projects the single source of truth. `CLAUDE-DESIGN.md:756` is a falsified anchor listed in the todo's own Discovery Log, yet `src/Design/` appears nowhere in the plan's Scope, Steps, Acceptance, or Constraints.

Worse, Step 8's three buckets (false claims / TRIM-005 artifacts / silences) miss a **fourth**: statements that are *accurate today and become inaccurate after the fix*. Two live anchors sit in it — `docs/trimming.md:222` and `CLAUDE-DESIGN.md:756`, both of which accurately describe "preserve all methods on the referenced type", which is precisely the property the fix removes for two of four shapes.

**Accepted.** Fourth bucket added; `src/Design/` named explicitly.

### A2 — AC6 has no Design-surface disposition before a release that closes AC4

Deferred item 11 accepts the Design demonstration gap for TRIM-002/007 on the grounds that preservation is unobservable in untrimmed Design tests. That reason extends to AC6 — over-preservation is equally unobservable — but the plan never says so.

**Accepted.** Explicit accept-with-reason recorded for AC6.

---

## Callout-Tier

- **B4 — Holder name was a prefix-extension of the user's type.** `MyCommandsNeatooFactoryRegistrar` keeps `global::Ns.MyCommands` as a substring, making the "does not name the consumer's type" assertion a false red and an unclosed `Contains` a false green — the same naive-substring class as deferred item 5. **Fixed in code**: holder is now `NeatooFactoryRegistrar_{TypeName}` (prefix), which breaks the namespace-qualified substring outright. Relay will use a distinct prefix.
- **B5 — Relay output bypasses `NormalizeWhitespace` and has no parse-error signal**, yet the plan adds a top-level type to it. `DiagnosticTestHelper.RunGenerator` returns `OutputCompilation`, which `AssemblyAttributeEmissionTests` discards. **Accepted:** new relay emission tests assert zero `DiagnosticSeverity.Error` on the output compilation, not just string containment.
- **B6 — Deferred item 15 changes error signature** from CS0111 to CS0111 + CS0101 if both legs share a holder name shape. Addressed by distinct per-leg prefixes.
- **B7 — "verified in CI at HEAD" overstates.** CI verifies a top-level holder *registers correctly under trimming*; it does not verify DAM narrowing, because TRIM-007's holder has one method and nothing to narrow. The shapes are analogous, not identical (per-assembly vs per-type). **Accepted** — wording tightened.
- **B8 — The plan asserted intent about the TRIM-005 advisory as fact** ("*This is why* it proposed a nested type — it was solving accessibility, not DAM breadth"), in the same paragraph indicting the arc for that habit. The Discovery Log's framing — "design work found a *second*, unstated reason" — is the defensible one. **Accepted**, restored.
- **B9 — Acceptance bullet 7 is fixture-presence, not behavioural.** "The harness carries targets" is satisfied by adding files. Folded into B1's present-before/absent-after requirement.
- **B10 — CI-gate bullet claims the wrong thing.** It cites a one-time keyboard red-before-green where Step 7 promises a *durable* positive control. Vacuity confirmed real: `build.yml:113-114` greps a path that, if absent, makes `grep -aq` return non-zero, the `if` false, and the step print success. Tier is right; wording is wrong.
- **B11 — Two orphan deferrals.** "Narrowing the DAM" and "`[ModuleInitializer]` registration" are out-of-scope with no Deferred Work row. Also notes the Constraints rationale for not narrowing is directionally right but imprecise — `DynamicallyAccessedMemberTypes` has no sub-method granularity, so no narrowing keeps `FactoryServiceRegistrar` rooted while dropping siblings.
- **A3 — AC6 says "every factory shape"; Acceptance covers two.** Needs one sentence on where class/interface evidence comes from.
- **A4 — The ~40 anchors are not enumerated anywhere the implementer can work from.** This set has been mis-inventoried twice already. Enumerate before the doc pass.

---

## Confirmed by the reviewer (independent check)

- `attr.Type` is consumed at exactly one site; retargeting is behaviour-preserving at the only consumption point.
- Forwarding rather than hosting is correct — the holder reaches `internal static FactoryServiceRegistrar`, not the `private static` `[Execute]` methods, so CS0122 is avoided without depending on unverified ILLink behaviour.
- No `[FactoryEventHandler<T>]` target exists in the harness; the string appears only in a comment.
- The CI exemption and its falsified rationale are exactly where the plan says.
- `docs/trimming.md:35` is the static bullet (false) and `:36` the interface bullet (true) — the do-not-touch list is correct.
- No transcription smell; code-level detail is confined to Current State, its sanctioned home.

## Ruled out separately

**Non-`[Remote]` `[Execute]` regression.** `ExecuteDelegateModel` has no `IsRemote` field; `StaticFactoryRenderer.cs:141-160` emits both a remote and a local registration for *every* delegate with no `[Remote]` check, and only the local one is guarded by `IsServerRuntime`. Such bodies are already unreachable on a trimmed client and merely DAM-retained, so the fix strips something already dead. Incidental finding: **`[Remote]` is decorative on `[Execute]` static methods** — `FactoryModelBuilder.cs:168` exempts static factories from NF0105 — yet NF0105's message and several doc pages present `[Remote]` as the trimming-enabling marker. Belongs in the Step 8 corrections.
