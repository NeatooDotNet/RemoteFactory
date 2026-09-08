# Plan Review — EXRM-003 — 2026-09-08

**Reviewer:** `plan-reviewer` · **Budget:** tight · **Object:** `plans/003-trimming-gate-pair.md` at draft (branch `exrm-003-trimming-gate-pair`)

**Verdict: APPROVED**

## Pass A — documented requirements

**Docs consulted:** `docs/todos/EXRM-execute-obeys-remote/todo.md` (Goal, AC, Out of Scope, Plan Index, Punchlist, Dismissed, Discovery Log); `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs`; `.../ClassFactoryWithExecute.cs`; `src/RemoteFactory/FactoryAttributes.cs`; repo `CLAUDE.md`.

**Veto-tier:** None. The plan's assertions match AC-3 and the Goal's "measures the pair rather than inferring it." Out of Scope is respected: interface factories stay out (Constraint), no NF0102 relaxation, no zTreatment work.

**Callouts:**

- **A1.** `src/RemoteFactory/FactoryAttributes.cs:102-108` — Affects: AC-5 (Must). Reachable by: a consumer reading `[Execute]`'s XML doc in IntelliSense today. The `[Remote]` XML doc still states "**`[Remote]` is decorative on `[Execute]` methods**" and "the renderer emits both a remote and a local registration for every delegate regardless of whether `[Remote]` is present" — falsified by EXRM-001 (`StaticFactoryRenderer.cs:141-158` emits *only* the unguarded local registration for a bare delegate). Step 7 rewrites the two Design comments derived from this same claim; this third, shipped location is on no Punchlist row and in no Dismissed entry. Not 003's to edit (its Scope excludes `src/RemoteFactory`) — but it is currently untracked. One option: a Punchlist row against EXRM-004's AC-5 path.
- **A2.** `Serves:` names AC-3 only — Affects: AC-1 (Must). Reachable by: the close-out audit reading bullets 1-2. AC-3 asks the gate to *measure* a body present/absent. Bullets 1 and 2 go further — the member "resolves from the harness container and runs there, returning the marker through a client-registered service" — which is AC-1's property ("runs on the client with client-container service resolution and makes no remote call"), now measured on a trimmed artifact for the first time. Either `Serves:` should read AC-3, AC-1, or the bullets over-reach their criterion.

## Pass B — codebase

**Files examined:** Gate/harness — `verify-trimmed.sh`, `Program.cs`, `ClassExecuteLegTarget.cs`, `TrimTestCommands.cs`. Generator — `ClassFactoryRenderer.cs:360-407, 810-880`, `StaticFactoryRenderer.cs:120-290`, `ClassFactoryModel.cs:58-66`, `FactoryModelBuilder.cs:194-196`, `DiagnosticDescriptors.cs:50-59`. Design — `AllPatterns.cs:352-395`, `ClassFactoryWithExecute.cs:115-145`.

**Reality check:** The plan's direction matches the code. Brief Q4 confirmed: `StaticFactoryRenderer.cs:141-158` gives a bare delegate exactly one unguarded registration in every mode including Remote, and `:162-186` emits the remote/guarded pair only for `IsRemote` delegates — so the static pair differs by `[Remote]` alone. Brief Q3: `RenderClassExecuteLocalMethod` (`:843-844`) passes `isServerOnly: method.IsInternal || method.IsRemote`, and `RenderLocalMethodOpening` (`:390-405`) emits the `Local{X}` wrapper **non-async unconditionally** with `async` only on `Local{X}Core`. So the bare sibling's state machine is `<Local{X}Core>d__`, not `<Local{X}>d__` — it cannot collide with the per-site discriminators at `verify-trimmed.sh:315-318`, and the pair needs no new discriminator. Brief Q1, second half: the republish-with-`[Remote]` artifact is the right known-bad one and the archived pre-003 artifact would be actively wrong — against it the new present checks go red because the *target does not exist*, which is the precise failure mode the positive-control STOP at `:101-110` was written to prevent. Step 6 compiles on both shapes: `FactoryModelBuilder.cs:195` fires NF0105 only when `method.IsRemote && !method.IsInternal && !method.IsStaticFactory`, and a class-level `[Execute]` is a static factory method — which is why `TrimExecTarget.RunExecCommand` is `[Remote] public static` today. Brief Q1, first half: pairing *is* sufficient against the TRIM-008 registrar-DAM regression — if the attribute named the consumer's class again, `_DoWork`/`_ProcessRecord`/`ClassExecBody_MARKER` come back present and go red in the same run.

**Veto-tier:** None.

**Callouts:**

- **B1.** `verify-trimmed.sh:239-246` vs. the arc's `Bare` prefix — Affects: AC-3 (Must). Reachable by: implementer follows EXRM-002's recorded naming (`BareExecute_`, Discovery Log 2026-09-08) and names the new client-side port method `BareExecLegInvoke` / `BareAsyncLegInvoke`. `present()` is `grep -aqF` (substring), and `ExecLegInvoke`, `ExecLegBackend`, `AsyncLegInvoke`, `AsyncLegBackend` are asserted **absent** at `:239` and `:244`. A prefix keeps the old marker as a suffix, so existing absence checks go red for the wrong reason — in the very run the plan wants to trust. The Constraint states the rule; the naming instinct violates it. One option: a distinct stem, not a prefix.
- **B2.** Step 4's "client-side service names present" cannot go red — Affects: AC-3 (Must). Reachable by: implementer runs Step 6 and finds those checks green in the known-bad artifact. Per the Constraint the port is registered *outside* `Program.cs:28`'s guard, i.e. unconditionally, so its name is rooted from DI regardless of the `[Execute]` machinery; re-adding `[Remote]` does not remove it. So (a) that half is a check that can never fail, and (b) Acceptance bullet 4 as worded — "**the new present checks** were observed red" — is unsatisfiable for it. The body-literal checks do flip and carry the measurement. One option: assert only the body literals, or word bullet 4 to name them.
- **B3.** `ClassExecuteLegTarget.cs:15-19` — Affects: AC-3 (Must). Reachable by: implementer edits this file at Step 2 and reads it as current. It says the class-`[Execute]` guard sits "inside the async body"; since TRIM-009 it sits on the non-async `Local{X}` wrapper (`ClassFactoryRenderer.cs:395-399`), `async` on `Core` only. Surrounding sentences are historical, so it may be deliberate — but it is the sentence a reader of the new pair relies on to know what varies.

**Theoretical (not triaged).**

- Adding a `public static` bare method to `TrimExecTarget` flips `AllMethodsInternal` (`ClassFactoryModel.cs:62`) — already false there via `[Remote]` promotion.
- Invoking the bare static delegate without a null guard would crash the process rather than append to `failedChecks` (`Program.cs:48-50`).

**Read report.** Beyond the brief: `FactoryModelBuilder.cs:194-196` + `DiagnosticDescriptors.cs:50-59` (Step 6's `[Remote] public` variant compiles); `ClassFactoryRenderer.cs:377-407` (wrapper/Core split); `ClassFactoryModel.cs:62`; `FactoryAttributes.cs` (grep — source of the Pass A callout). Named but unused: `reviews/002-plan-review.md`, `plans/001`, `plans/002`, `TRIM.../todo.md:119-120`, harness `README.md`, `build.yml:99-126` — the brief's distillations sufficed and no finding turned on them.

*(Transit note: the report arrived in two messages, the first truncated mid-sentence in Pass B; the tail was requested and joined here verbatim.)*

## Triage (proposed 2026-09-08; user decisions recorded below)

| # | Finding | Affects | Priority | Size | Disposition |
|---|---|---|---|---|---|
| 1 | B2 — a present check on the client-side port name can never go red; bullet 4 is unsatisfiable for it | AC-3 | Must | punch | **Amend** Step 4 and bullet 4: the gate asserts the two body literals only; service resolution stays a harness invocation check |
| 2 | B1 — a `Bare` prefix on new port names keeps existing absent markers as substrings and reddens them for the wrong reason | AC-3 | Must | punch | **Amend** the naming Constraint: new names take a distinct stem, never an existing marker with a prefix or suffix |
| 3 | B3 — `ClassExecuteLegTarget.cs:15-19` says the guard sits inside the async body; since TRIM-009 it sits on the non-async wrapper | AC-3 | Must | punch | **Punch now** (plan Punchlist): correct the sentence while editing the file at Step 2 |
| 4 | A2 — `Serves:` names AC-3 only while bullets 1–2 measure AC-1's property in a trimmed artifact | AC-1 | Must | punch | **Amend** header `Serves: AC-3, AC-1` and the Plan Index row |
| 5 | A1 — `FactoryAttributes.cs:102-108` XML doc still says `[Remote]` is decorative | AC-5 | Must | punch | **Dismiss**: already in EXRM-004's Scope ("the attribute XML docs") and its Notes inventory (`:102-108`, `:95-99`, undocumented `RemoteAttribute`); verified 2026-09-08 |

Theoretical items are carried to the plan's Notes as pre-flight pointers (the harness invocation of a bare member is null-guarded and reports through `failedChecks`; `AllMethodsInternal` is already false on `TrimExecTarget`).

**User decisions:** Accept all as proposed (2026-09-08); commit and push on the plan branch.
