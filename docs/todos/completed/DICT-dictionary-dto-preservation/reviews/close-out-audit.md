# DICT close-out audit

**Gate:** Step 7, code-reviewer, whole arc. Run on `DICT` at `9fb1696`, after main was merged in at `5ce7bc1`.

## 2026-10-06 — Grade: A

No veto-tier findings. Every Acceptance Criterion traces to code and evidence, and both solutions are green on both TFMs.

### Acceptance Criteria trace

| AC | Word | Evidence | Holds? |
|---|---|---|---|
| AC-1 | Must | `DtoTypeWalker.cs` `ExpandCandidates` (a `KeyValuePair` yields K and V, recursively), `AsKeyValuePair`, `StripNullable`. All three callers go through it: signatures at `FactoryGenerator.Types.cs:819,836`, entities at `:271`, events at `FactoryGenerator.Events.cs:69`. Unit tests cover signature return, signature parameter, entity property, and event record property, plus the shape, bucket, nullable, and consumer-collection cases. `RunDictionaryValue` passes on the trimmed client in `002-evidence/trimmed-run.txt` and in round 2 of `gate-round1-fixes.txt`. | Yes. On a trimmed client only the entity path is exercised. The signature and event paths are shown by the generator's emitted registration, as the DICT-001 amendment of 2026-10-05 records. |
| AC-2 | Could | The same expansion. `DictionaryKeyDto_Registered`. | Yes |
| AC-3 | Must | The only `new TrimDictValue` is inside the async `[Remote]` body `TrimDictCarrier.FetchAsync`. The check is red with a no-constructor `NotSupportedException` at `effedce`, which leaves `src/Generator` untouched (`001-evidence/trimmed-run.txt`), and green untrimmed (`untrimmed-run.txt`). | Yes |
| AC-4 | Should | `v1.7.0.md:130`, a Migration Guide paragraph marked as added after the release. The "Upgrading across v1.7.0" section of `docs/trimming.md`. | Yes |
| AC-5 | Should | Covered in:<br>• `docs/trimming.md`, "What the walk reaches", "What the walk does not reach", and the Limitations bullet<br>• the skill's trimming reference<br>• `service-injection.md:42`<br>• `CLAUDE-DESIGN.md`<br>• `FactoryEventHandlerPattern.cs:146-152`<br>• the v1.10.1 notes<br><br>Each claim was checked against the walker. Release bookkeeping is complete:<br>• `nav_order` runs 1 to 14<br>• both index rows are present<br>• both version properties are set<br>• the v1.10.0 dependency is stated<br><br>Releasing is outside this gate. | Yes |
| AC-6 | Should | `SerializedMemberType`, called from `WalkMembers`. Unit tests cover a field on a DTO, a field on an entity, a record field, and non-public, static, and const fields that are not walked. `RunPublicField` is red at `$.Carried` in `001-evidence` and green in `002-evidence` and round 2. | Yes |

### Build and test

| Log | Result |
|---|---|
| `closeout-build.log` | 0 errors. 2 warnings, the WASM-workload notice in `OrderEntry.BlazorClient`, which predates this arc |
| `closeout-design-build.log` | 0 warnings, 0 errors |
| `closeout-test.log` | 0 failed. Unit 816 and integration 628 passed per TFM. The 5 skipped integration tests are `RelayTimingTests` and `ShowcasePerformanceTests`, which were already skipped on `main` |
| `closeout-design-test.log` | 0 failed. Design 103 passed per TFM |

- **Trimmed harness:** last run after `513d0c0`. No generator, harness, or runtime file has changed since then. CI runs the harness on the arc → main PR.
- **Sacred tests:** the arc only adds to `src/Tests`. The one-line change to `verify-trimmed.sh` is the line continuation for its new positive control.

### Container

- **Plans:** 4 issued of a cap of 6. 004 is Retired, with a reason.
- **Deferrals:** none untraced.
- **PRs:** none open into `DICT`.
  - #106 and #109 are merged into `DICT`.
  - #107 was merged into the stacked branch, 27 seconds after #106.
  - Every SHA cited by the plans and the release notes is reachable from `DICT`.
- **Prose budget:**
  - The 2026-09-11 / DICT Discovery Log entry is about 64 words against a budget of 60. **Accepted.**
  - The Goal is 146 words against 150.
- **Gates and evidence:**
  - Every Done plan's Gate Record has at most two rounds.
  - No Test Evidence row is `MISSING`.
  - All 19 cited test methods exist: 17 Facts plus 2 Theories of 3 cases each, so 23 cases.
- **Scope:** Out of Scope holds, and no Dismissed item was raised again.

### Callouts

None that affect a criterion.

### Theoretical, not triaged

- A doc comment at `src/Generator/FactoryGenerator.Types.cs:807-808` still says "public properties of discovered DTOs". It is an internal comment and outside AC-5's artifacts. Routed to Follow-on.
- Two lines in plan 003 are stale:
  - The Scope says it "does not restate the v1.7.0 migration hazard". The amendment of 2026-10-06 records the reversal.
  - The Notes say v1.10.0 must be published first. The dependency bullet supersedes this.
- The v1.10.1 notes use a `**Breaking changes:**` header line, the settled style since v1.8.0. Their Commits list named DICT-003's commits by plan rather than SHA. **Fixed at Step 8:** `d861388`, `8d00c09`, and `d2fb050` are now listed.
- Sorted, concurrent, and immutable dictionaries are reached by the code but not tested, because the unit-test helper references CoreLib only. Routed to Follow-on.

### Reviewer read report

- **Beyond the brief:**
  - the three generator call sites
  - `.github/workflows/build.yml`
  - the frontmatter of the 1.x release-notes pages and the template in `index.md`
  - `gh pr view` for #106, #107, and #109
  - `git diff 066c68a 5ce7bc1`
  - a grep of published docs for consumer names; none were found in this arc's edits
- **Named but unused:**
  - the per-plan logs, beyond the counts already cited
  - `unit-red.txt` and `unit-green-and-negative-control.txt`
  - the generated-registrar excerpts
