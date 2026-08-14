# TRIM-009 — Code Review (per-plan, opt-in)

**Gate:** Step 5, opted in (`Code-review opt-in: Yes`). **Pass:** one (2026-08-14). Findings-only; no grade.
**Evidence set:** [`009-evidence/`](./009-evidence/) — manifest in [`009-test-review.md`](./009-test-review.md).

Every checkable finding was independently re-derived at the keyboard before being accepted. **All of them held.**

---

## Standing conclusions

**The fix is correct, minimal, and in the right place.** `git diff --name-only main..HEAD -- src/Generator/` returns exactly one file. `RenderLocalMethodOpening` is the correct seam: the guard string now exists at **two** places in the whole renderer instead of five copies, and `IsServerRuntime` appears nowhere else in `ClassFactoryRenderer.cs`. All five guarded sites route through it — including `LocalSave`, whose omission would have passed Step 1 while shipping a guarded async body.

**No argument transposition**, the risk I most wanted ruled out. `GetParameterDeclarationsWithOptionalCancellationToken` and `GetParameterIdentifiersWithCancellationToken` apply byte-identical `Where` filters and identical `params` reordering, and every call site passes the same flags to both — so the wrapper's forwarding args match the core's signature by construction, not by coincidence.

**`LocalSave`'s `virtual` is sound** (wrapper keeps it, signature unchanged, overrides still compile and still bypass the guard exactly as before). **`blankLineAfterGuard: false` is faithful** to the prior Save emission. **Holder prefix collision is impossible**, not merely unlikely: the three prefixes diverge at character 7.

**Zero incremental-cache delta confirmed:** `Model/` and `Builder/` diffs are empty and `IncrementalCacheTests` is untouched.

**Framework rules clean.** `FactoryAttributes.cs` is XML-doc-only — no API surface change. No reflection added. The one inverted assertion preserves intent and adds a regression assertion on the old target.

**All four veto-tier findings were in the claims layer, not the fix** — the same distribution as the plan review, and the same place this arc keeps paying.

## Veto-tier findings

| # | Finding | Disposition |
|---|---|---|
| V1 | **`docs/trimming.md:236` is now false and contradicts `:249` in the same section.** It still said class factories "have a type to name that is not yours", eleven lines above the sentence this plan edited to say the opposite. Step 7b's list was built from the plan instead of from the file — the exact failure the doc-anchor inventory exists to stop | **Fixed.** Rewritten to cover all three holder legs and name the interface exception |
| V2 | **`docs/trimming.md:37` still asserted the interface leg's mechanism** — "making the server-only code path unreachable to the trimmer" — the single published statement asserting precisely what H1 measured insufficient. Deferred item 20 named this artifact by name as release-blocking, and the fix pass qualified the skill and `CLAUDE-DESIGN.md` but not this | **Fixed.** Qualified to "not established", with the reason (guard inside async is not sufficient; the leg still names `{ImplName}Factory`) |
| V3 | **`CLAUDE-DESIGN.md:760` said "Every factory shape therefore emits its own forwarding holder."** The interface factory does not — `InterfaceFactoryRenderer` emits no holder (grep: zero occurrences). Contradicted by the table 10 lines below, by "the **three** holder rows", and by the carve-out 17 lines below — all written by this plan's own edit | **Fixed.** "Three of the four shapes", with the exception stated where the claim is made |
| V4 | **Class-`[Execute]` had no untrimmed self-check and no pre-fix baseline, while three artifacts stated or implied it did** — the plan's Test Evidence, `verify-trimmed.sh:127` ("Every marker here appears PRESENT in the UNTRIMMED build"), and a `[N]`-labelled block asserting the leg "was subject to the defect in full" as fact | **Fixed.** Untrimmed self-check re-run across all legs; all five Exec markers PRESENT. Both overclaims scoped, and the `[N]` block now distinguishes what is measured from what is read off the emitted source |
| V5 | **Step 8 declared deferred item 8 discharged; the row was byte-identical to `main`** | **Fixed.** Item 8 closed — and, as V1/V2 showed, the residual genuinely had not been discharged when the claim was made, so the stale row was accidentally accurate |

## Callout-tier

- **C1 — dead local.** `var asyncKeyword` in `RenderSaveLocalMethod`, unread since the modifier moved into the helper. Not a CS0219 (non-constant initializer), which is why `TreatWarningsAsErrors` missed it. **Deleted.**
- **C2 — no compile assertion on class-factory emission**, while both sibling legs have one, and `FactoryRenderer` swallows render exceptions into a `/* Error: */` comment. **Fixed** — and it failed on its first run (missing fixture usings), so it was not decorative.
- **C3 — the behaviour note overstated one clause.** "Because the public entry point is itself non-async" is true for reads but false for `Save` on an authorized factory, which is `public virtual async` and captures the throw back into a faulted `Task`. Erred conservative, but **fixed** in both docs.
- **C4 — `Local{X}Core` has no collision guard.** A method whose `UniqueName` ends in `Core` can collide with another's generated core: CS0111 in generated code with no diagnostic. No such shape exists in the repo, Design projects, or examples. **Recorded as deferred item 21**, alongside items 14/15.
- **C5 — a declared Verification item has no artifact.** The plan promised a post-fix generated-tree diff; only the pre-fix inertness diff is archived. The reviewer substituted by reading the emitted trees for all five sites plus the interface leg and found only intended changes, so the conclusion holds — but the promised evidence does not exist. **Recorded.**

## Verified emitted output

The strongest evidence in the change is `TrimSaveTargetFactory.g.cs`: `LocalInsert`/`LocalUpdate`/`LocalDelete`/`LocalSave` are all non-async wrappers forwarding to `…Core`; sync `LocalCreate` and the five sync `LocalCan*` are correctly **not** split; and `LocalSaveCore` routes to the **wrappers**, which is what makes the de-rooting prediction hold. The holder is a genuine single-method type and `AddRemoteFactoryServices` binds it via `BindingFlags.Static | NonPublic | Public`.

Only one unintended emission change exists and it is cosmetic: `public  Task<T>` → `public Task<T>` (the old `{asyncKeyword}` interpolation left a double space when empty), which `NormalizeWhitespace` collapsed anyway.

## Verdict

**The deliverable is done.** The generator change was correct on the first pass and survived scrutiny unchanged; every finding was a claim outrunning its evidence, and all are closed. Two of them — V1 and V3 — were sentences this plan itself wrote while fixing the previous plan's sentences, which is worth naming: the doc surface is now the highest-churn, lowest-verification part of this arc.

**AC6 closes as written**, with the interface-factory leg carved out in writing rather than claimed.
