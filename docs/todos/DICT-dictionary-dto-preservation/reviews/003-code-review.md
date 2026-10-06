# DICT-003 code review

**Gate:** Step 5, code-reviewer, per-plan pass (opted in). The object was every claim edited in `git diff 61930d3 HEAD`, outside `docs/todos`, checked against `DtoTypeWalker.cs` and the recorded evidence.

## Round 1 — 2026-10-06 — CLEAN, three callouts

**Veto-tier:** none.

**Logs:** both builds show 0 errors. Unit tests 816, integration 628 with 5 skipped, and Design 103, all passing on net9.0 and net10.0.

**Direction and shape:**

- Every reach claim traces to the walker:
  - nullable at any level;
  - recursive generic collections;
  - both halves of every generic dictionary;
  - a consumer generic collection kept with its element;
  - public fields;
  - the depth cap of 8.
- Sorted, concurrent, immutable, and custom generic dictionaries are reached according to the code, but only `Dictionary`, `IDictionary`, and `IReadOnlyDictionary` are tested.
- The not-reached list is complete for shapes that matter. Interface and abstract members, tuples, non-generic collection subclasses, and `object` are each rejected where the docs say.
- The entity-field claim and the `service-injection.md` correction match `003-evidence/field-wire-observation.txt`.
- The remedy samples compile against `Neatoo.RemoteFactory.Internal.DtoConstructorRegistry`.
- The release bookkeeping is complete:
  - nav order 1 to 14;
  - both version properties;
  - both index rows;
  - a patch bump, which is right under the v1.0.0 commitment;
  - all five commit SHAs exist.
- Every anchor resolves, and the skill links to nothing outside itself.

### Triage

| # | Finding | Affects | Priority | Size | Disposition |
|---|---|---|---|---|---|
| 1 | `docs/trimming.md:337` says "every generic collection — anything implementing `IEnumerable<T>`". Read plainly, that includes a non-generic subclass, which the table below it correctly lists as not reached | AC-5 | Should | punch | **Fixed.** It now reads "every generic collection type — a generic type implementing `IEnumerable<T>`". |
| 2 | The v1.10.1 Testing section says "the shape tests were observed red against the unchanged generator." The evidence shows 15 shape cases red against the unchanged generator. The three consumer-collection cases were red against the first-cut walker `c381e77`, and the nullable-value case was a pin that was never shown red | AC-5 | Should | punch | **Fixed.** It now states both counts and what each ran against. The same paragraph's "exact exception the consumer hit" now names the dictionary case only, because the consumer never hit the field case. |
| 3 | The index Highlights row says "Observed red on the trimmed CI harness." The red run was local, on the harness that CI runs | AC-5 | Should | punch | **Fixed.** It now says "publish-trimmed harness". |

### Theoretical, not triaged

- `IAsyncEnumerable<T>` members are not unwrapped and are not listed. They are rarely deserializable as members.
- DTOs reachable only through non-public `[JsonInclude]` members, or with only a non-public `[JsonConstructor]`, are skipped and not listed.
- A consumer interface named `IEnumerable<T>` is treated as a collection. That can only add to what is reached.

### Reviewer read report

- **Beyond the brief:**
  - `NeatooInterfaceJsonConverterFactory.cs`, for the `$type` rationale.
  - The DICT-001 evidence, and DICT-002's `trimmed-run.txt` and `gate-round1-fixes.txt`.
  - The `v1.10.0` tag and the cited commits.
- **Named but unused:**
  - `NeatooOrdinalConverterFactory.cs`, because the probe answered the Ordinal-versus-Named question directly.
  - `closing-claim-grep.txt`.
  - The DICT-002 test source beyond its theory data and test count.
