# TRIM-005 — Server-only reference over-retention in trimmed clients

**Plan #:** 005
**Date:** 2026-08-11
**Related Todo:** [../todo.md](../todo.md)
**Status:** In Progress
**Last Updated:** 2026-08-11
**Plan-review opt-in:** Yes (changes the emitted body shape of every guarded factory method across all three renderers — a generated-code contract change on the seam the published trimming story is sold on)
**Code-review opt-in:** Yes (behavior-changing generator work)

---

## Scope

Close the over-retention gap TRIM-004 surfaced: a publish-trimmed client keeps the `IServerOnlyRepository` TypeDef name and its `DoServerWork` member reference because the generated server-only method bodies survive ILLink's constant folding — the methods are rooted client-side by delegate registration, and the current early-`throw` guard shape leaves the rest of the body (including an exception-handling region) in a form the trimmer declines to eliminate. Per the user's direction (2026-08-11) this plan targets the **generator fix**: reshape the emitted guard so ILLink can fully eliminate the guarded region on a client, making server-only type and member references vanish from trimmed client binaries and making `docs/trimming.md`'s claims true as written rather than approximately true. The plan starts by turning the gap into a red trimmed-harness pin before any generator edit, so the fix has a sensitivity-proven acceptance signal. Does NOT change guard *placement* policy (which methods get guards stays exactly as documented), does NOT change `NeatooRuntime.IsServerRuntime` or its feature-switch substitution contract, does NOT change the runtime throw behavior consumers observe, and does NOT touch DTO preservation (TRIM-001/002) or event preservation (TRIM-003/007).

---

## Intent

- A publish-trimmed client binary contains no server-only type name or member reference that is reachable only through guarded factory bodies — the server-only grep `docs/trimming.md` tells consumers to run genuinely returns nothing.
- The published trimming story stops overpromising: the IP-exposure and bundle-size claims become literally accurate instead of "implementations are gone, names linger."
- TRIM-004's CI gate tightens from an implementation-only grep to the full server-only surface, so the stronger guarantee is enforced rather than asserted.
- Consumers observe no behavioral change: guard placement, the non-server-runtime throw, and every lifecycle hook keep their current semantics.

---

## Framework & Architectural Alignment

- The fix stays entirely inside the generator's renderers. `docs/trimming.md`'s central promise — "the guards are in RemoteFactory's **generated** code, not your application code" — means no consumer action, no runtime API change, and no domain-model change is admissible as part of this plan.
- `NeatooRuntime.IsServerRuntime` remains the single `[FeatureSwitchDefinition]` seam. This plan changes only the *shape* of the code the switch guards, never the switch, its default, or its `RuntimeHostConfigurationOption` substitution contract.
- Guard placement policy is documented behavior and stays frozen: `[Remote] internal` and plain `internal` methods are guarded; `public` non-`[Remote]` methods are not.
- Authorization checks remain inside the guarded region — auth types are server-only and must trim with it.
- Whatever shape lands must hold across all three factory renderers plus the relay-handler renderer, so the trimming guarantee doesn't depend on which factory pattern a consumer picked.
- Trimmed verification follows the TRIM-004 harness contract established for this todo; the harness check and CI gate are the acceptance pins, with a keyboard negative control proving sensitivity (TRIM-001/002/007 precedent).

---

## Constraints & Invariants

- Invoking a guarded factory method in a non-server runtime still throws `InvalidOperationException` with the current message — the untrimmed path is behaviorally unchanged.
- Guard placement is unchanged: no method gains or loses a guard.
- Authorization checks stay inside the guarded region.
- Logging, correlation, and stopwatch instrumentation keep their current semantics and ordering, as do the write-style lifecycle hooks (`IFactoryOnStart`, `IFactoryOnCancelled`, `IFactoryOnComplete`) including the cancellation path.
- No renderer is left on the old shape — class, interface, static, and relay-handler emissions stay mutually consistent.
- Expected-generated-code tests are updated to the new shape with their **original assertion intent preserved**; assertions are not weakened or dropped to accommodate the new emission.
- Incremental-generator caching is unaffected — this plan changes render output, not transform-output records (TRIM-006 still owns that hole).
- Full suite green on net9.0 and net10.0; CI trimming gate green.

---

## Steps

1. Turn the gap into a red pin first: extend the trimming harness with a server-only-surface check asserting that the server-only interface name and its member reference are absent from the trimmed client binary. It must fail at HEAD before any generator edit.
2. Determine, at the keyboard with the harness as the instrument, why ILLink retains the guarded body at the current shape — distinguishing the exception-handling region, client-side rooting, and substitution behavior as causes rather than assuming which one dominates.
3. Reshape the guarded emission in the class-factory renderer so the server-only region becomes eliminable, preserving the runtime throw contract exactly.
4. Propagate the same shape to the interface, static, and relay-handler renderers so every guarded emission trims consistently.
5. Update the expected-generated-code unit tests to the new shape, preserving what each existing assertion was there to catch.
6. Tighten TRIM-004's CI grep from implementation-only to the full server-only surface once the harness check is green, so the stronger guarantee is enforced going forward.
7. Keyboard negative control: revert the guard shape, confirm the new check goes red, restore.
8. Bring `docs/trimming.md` and the harness README to shipped behavior — the trimming story states what is actually eliminated, and any residue that remains is stated plainly rather than implied away.

---

## Acceptance

- [ ] A publish-trimmed client binary contains no server-only interface name or member reference reachable only through guarded factory bodies. `[trimmed-harness]`
- [ ] Invoking a guarded factory method in a non-server runtime still throws `InvalidOperationException` with the existing message. `[integration]`
- [ ] Generated factory code for all four guarded emission sites carries the new guard shape, and the existing generated-code assertions still catch what they were written to catch. `[unit]`
- [ ] The new server-only-surface check's sensitivity is proven by a keyboard negative control (old guard shape → red). `[explicit-skip: one-off keyboard verification, per TRIM-001/002/007 precedent]`
- [ ] TRIM-004's CI gate asserts the tightened server-only surface rather than implementation types only. `[explicit-skip: CI gate configuration, not a behavior test]`
- [ ] `docs/trimming.md` and the harness README describe shipped behavior with no surviving overpromise. `[explicit-skip: doc delta, reviewed at code review]`
- [ ] Full solution build/test green (net9.0 + net10.0); CI trimming gate green. `[explicit-skip: build/test/CI gates]`

---

## Current State (Pre-Flight)

Partially walked at draft time (2026-08-11) on branch `TRIM` (602a6d4) — completed at Step 3 before the first edit.

- **Feature switch:** `NeatooRuntime.IsServerRuntime` (`src/RemoteFactory/NeatooRuntime.cs:12-16`) is a genuine `[FeatureSwitchDefinition("Neatoo.RemoteFactory.IsServerRuntime")]` over `AppContext.TryGetSwitch`, defaulting `true`. Substitution is available; the question is folding, not switch plumbing.
- **Current guard shape** (`ClassFactoryRenderer.cs:326-331`, `:757-762`, `:819-824`, plus the interface/static/relay renderers): `if (!NeatooRuntime.IsServerRuntime) throw new InvalidOperationException("Server-only method called in non-server runtime.");` emitted only when `method.IsInternal || method.IsRemote`, followed by the rest of the body in the same method.
- **The suspected blocker:** the guarded body opens a `try` (`:358`, `:843`) with `catch (Exception _ex)` (`:527`) and, for async write-style lifecycle, an additional `catch (OperationCanceledException)` (`RenderWriteLifecycleOnCancelled`, `:678-686`). An EH region in the method body is the prime suspect for ILLink declining unreachable-block elimination — to be confirmed at Step 2, not assumed.
- **Renderer inventory:** `ClassFactoryRenderer.cs`, `InterfaceFactoryRenderer.cs`, `StaticFactoryRenderer.cs`, `RelayHandlerRenderer.cs` all reference the guard/`NeatooRuntime` seam.
- **CI gate as it stands:** `.github/workflows/build.yml:110-114` greps the published harness DLL for `ServerOnlyDirect` and `(?<!I)ServerOnlyRepository` — the negative lookbehind deliberately exempts the interface name, with a comment saying it "is expected to remain." That exemption is what Step 6 reverses.
- **Harness:** `src/Tests/RemoteFactory.TrimmingTests/` — `ServerOnlyTypes.cs`, `TrimTestEntity.cs`, `TrimTestCommands.cs`, `DirectFeatureSwitchTest.cs`, `Program.cs`, `README.md`. README already frames the search as "server-only IMPLEMENTATION types … should return nothing."
- **Doc anchors to correct:** `docs/trimming.md:36` (interface factories — "making the server-only code path unreachable to the trimmer"), `:42` (the feature switch — "All code behind the `false` branch is eliminated"), plus the "Verifying Trimming Results" grep guidance and the harness README's claim.

---

## Test Evidence

Filled after implementation, before the Step 5 gate.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| | | | |

---

## Plan Amendments

(None yet.)

---

## Abandonment / Retirement Reason

<!-- Only if Status becomes Abandoned or Retired. -->

---

## Notes

- **Outcome fork resolved by the user (2026-08-11):** the Scope's original "generator fix *or* documented acceptance" is settled in favor of the fix. If Step 2 proves ILLink cannot eliminate the guarded region regardless of shape, the workflow response is Abandon-with-reason plus a successor docs-correction plan — deliberately *not* a fallback branch inside these Steps.
- Functional boundary enforcement is already intact at HEAD: server-only *implementations* are trimmed today, and a client that calls a guarded method still throws. This plan is bundle size, IP surface, and documentation accuracy — not a correctness fix. That framing sets the bar for how much generated-code churn is worth accepting.
- Release notes for the whole TRIM arc (todo Acceptance Criterion 4) remain owned by the todo-level release step, not by this plan — same disposition as TRIM-007.
- Branch topology: implemented on `TRIM-005-server-only-guard-shape` off `TRIM`. That base carries the TRIM-007 bookkeeping commit from the closed PR #72, which rides along in this plan's PR to `main`.
