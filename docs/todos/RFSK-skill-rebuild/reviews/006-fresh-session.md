# RFSK-006 — Fresh-session test, scored

**Date:** 2026-10-16
**Skills under test:** `~/.claude/skills/RemoteFactory`, byte-identical to the repository's `skills/RemoteFactory` before each run (`diff -rq` empty), with the installed `neatoo` and `mudneatoo` skills beside it. Claude Code 2.1.301.
**Prompts:** `006-runs/s1.txt`–`s10.txt`. s1–s5 are SKR-007's prompts, verbatim (`cmp`). s6, s8 and s10 open with "using the RemoteFactory library, without Neatoo", so the RemoteFactory skill is the only design guidance they get.
**Scorer:** the eleven RemoteFactory trap rows in `SKILL.md` (VisionAlignment section 4: T3, T4, T5, T6, T11's RemoteFactory half, T13, T14, T15, T18, T19, T20) and SKR-007's four open findings:
- F-a: the "persistence routing" framing;
- F-b: N+1 as the reason child operations are `internal`;
- F-c: a repository-named interface factory;
- F-d: `[Remote, Create]` where creation needs nothing from the server.

**Scoring rules:**
- A row is fallen into when the output does what the row's question assumes, or when its prose gives the habit's answer to that question.
- "—" means the scenario gives the row no occasion.
- Every output was screened for each row's signature, and each hit and each scenario's governing passage was read:
  - controllers and DTO classes;
  - `interface I…Repository`;
  - "N+1" and "persistence routing";
  - `[Remote, Create]`, and `[Remote]` on a child operation;
  - an unassigned `Save`;
  - a stored method-injected service;
  - "`[Remote]` … runs on the server" and "without `[Remote]` … runs on the client".
- Line references are into `run2/outN.md`, or into `run3/out10.md` where marked.

## Run record

Three runs. Only run2 and run3 are scored.

**run1 (contaminated, not scored): `006-runs/run1/`.**
- **Command:** `claude -p "$(cat sN.txt)" --allowedTools "Skill Read Glob Grep"`, run in an empty scratch directory.
- **Exit codes:** every session exited 0.
- **Why it was set aside:** the user settings carry `defaultMode: auto` and allow `Read`, `Grep` and `Glob` everywhere, so `--allowedTools` restricted nothing.
- **What leaked (the code review found this):** the transcripts record `permissionMode: auto`, and six sessions read the repositories:
  - s1, s3 and s7 ran Bash into Neatoo `src` and `src/Design`;
  - s6 read RemoteFactory `src/Design` with Read and Grep;
  - s8 and s10 ran Bash into RemoteFactory `src/Design`.

**run2 and run3: the isolated mechanism.** Each scenario ran in its own empty scratch directory outside the repository, with stdin from `/dev/null`:

```
claude -p "$(cat sN.txt)" --permission-mode dontAsk --strict-mcp-config \
  --allowedTools "Skill" "Read(~/.claude/skills/**)" "Glob(~/.claude/skills/**)" "Grep(~/.claude/skills/**)" \
  --disallowedTools "Bash" "PowerShell" "Agent" "Task" "WebFetch" "WebSearch" "Write" "Edit" "NotebookEdit" \
    "Read(~/source/**)" "Read(//c/Users/KeithVoels/source/**)" "Read(//c/src/**)" \
    "Read(~/AppData/**)" "Read(//c/Users/KeithVoels/AppData/**)" \
    "Read(~/.claude/projects/**)" "Read(~/.claude/todos/**)" "Read(~/.claude/plans/**)"
```

**The isolation was proven before the runs.** A probe session was asked to read seven targets:

| Target | Result |
|---|---|
| The repository's `CLAUDE.md` | Denied |
| A grep of RemoteFactory `src/Design` | Denied |
| A glob of Neatoo `src/Design` | Denied |
| `~/.claude/projects` | Denied |
| Bash | No such tool in the session |
| MCP | No MCP tools |
| `~/.claude/skills/RemoteFactory/SKILL.md` | Read |

**The isolation was verified after the runs** from every run2 and run3 transcript:
- `permissionMode` was `dontAsk` throughout.
- No session used Bash, and every tool call targeted `~/.claude/skills`, except two attempts that were denied:
  - s6 globbed `src/Design/Design.Domain/{Entities/OrderLine.cs,Aggregates/Order.cs}` under the RemoteFactory checkout, the paths the skill's snippet-source links name;
  - s10 grepped RemoteFactory `src`.

**stderr:** each session printed only the two settings notices about the user settings' `Write(...)` allow rules.

**Results:**

| Run | Scenario | Exit code | Lines |
|---|---|---|---|
| run2 | s1 | 0 | 747 |
| run2 | s2 | 0 | 504 |
| run2 | s3 | 0 | 520 |
| run2 | s4 | 0 | 816 |
| run2 | s5 | 0 | 610 |
| run2 | s6 | 0 | 578 |
| run2 | s7 | 0 | 804 |
| run2 | s8 | 0 | 478 |
| run2 | s9 | 0 | 406 |
| run2 | s10 | 0 | 520 |
| run3 | s10 only, after the amendment below | 0 | 455 |

## Verdict

**After the amendment, no scenario falls into any RemoteFactory trap row or any of the four findings.** AC-7 is met.

- **One row was fallen into in run2.** s10, the "what is RemoteFactory for" scenario, gave the habit's answer to T18 in prose (out10:1): "An operation marked `[Remote]` runs on the server … An operation without `[Remote]` runs on the client with no network call."
- **The cause was in the skill.** s10 read `references/setup.md`, whose factory-modes sentence ended "everything else runs on the client".
- **The fix.** That sentence now says a call the client makes to a `[Remote]` operation crosses, any other call runs on the tier that makes it, and the same operation called on the server runs there locally. The skill was re-synced and s10 re-run as run3. run3 describes `[Remote]` as the call crossing (run3 out10:5, 113).

## Scenarios

| # | Scenario | Preamble |
|---|---|---|
| s1 | Contact edit with phones and Save | Neatoo + RemoteFactory |
| s2 | Contact search form and results | Neatoo + RemoteFactory |
| s3 | Duplicate-email check while typing | Neatoo + RemoteFactory |
| s4 | Open-or-create today's visit with action items | Neatoo + RemoteFactory |
| s5 | Nightly dormant-contacts job | Neatoo + RemoteFactory |
| s6 | Company with addresses: rename, edit, add, remove, save; say why each method is or isn't `[Remote]` | RemoteFactory only |
| s7 | Merge duplicates: choose winning values, confirm, keep one and delete the other in one transaction | Neatoo + RemoteFactory |
| s8 | A pure-computation formatter on both tiers, a server-only audit trail on save | RemoteFactory only |
| s9 | Recent-contacts sidebar; a company lookup against an external API only the server may call | Neatoo + RemoteFactory |
| s10 | What RemoteFactory is for; a create that needs server data beside one that needs nothing | RemoteFactory only |

## Row by row

Every scenario that reaches a row is listed with its own evidence.

| Row | Result | Evidence by scenario |
|---|---|---|
| T3 load-or-create | pass | **s4:** `Visit.OpenToday` is a class-level static `[Remote, Execute]` returning `FetchForDay(...) ?? Create(...)`. Both operations are `internal` (out4:123-124, 163-171). |
| T4 `[Create]` from loaded data | pass | **s1:** each loaded phone is built by its own `Fetch(row)`, handed its row by the list (out1:337-343, 386-392).<br>**s4:** the same shape (out4:319, 369-376).<br>**s6:** the same shape (out6:218, 290-297).<br>**s7:** the merge model's `[Create]` reads both contacts; scored as a pass, see Observation 1 (out7:238, 320-351). |
| T5 the client and the repository | pass | **s2, s3:** persistence is a server-only `[Service]` (out2:207-210; out3:34-38).<br>**s9:** the client calls a read model (out9:64-76) or a domain service named for what it does, `ICompanyRegistry`: "It is not a repository" (out9:106-118).<br>No `I…Repository` in any output. |
| T6 the application service | pass | **s3:** a static `[Factory]` command when the call returns and is done (out3:30-45).<br>**s7:** a static `[Remote, Execute]` command for the merge, which works across two aggregates (out7:383-430).<br>**s4:** a class-level static `[Remote, Execute]` when the result is the aggregate (out4:163). |
| T11 who writes a child's row | pass | **s1, s4, s6:** the root makes its own row and hands that row's child collection to the list factory's `Save` (out1:260-264, 288; out4:233, 257; out6:128-131, 144). The list makes, finds or removes each child's row and hands each child its own (out1:395-416; out4:378-398; out6:303-325). The child maps itself (out1:345-363; out4:327-340; out6:231-250). The root flushes once. |
| T13 `Save()` and the object | pass | **Reassigned wherever the object is kept:** s1 (out1:502), s3 (out3:389), s4 (out4:622), s5 (out5:239, 491), s6 (out6:540), s8 (out8:464), s9 (out9:385), s10 (run3 out10:380, 431).<br>**s7:** saves both contacts on the server without reassigning (out7:423-425). Neither object is used afterwards: the surviving id is taken before the saves (out7:415, 428). The row's answer is "reassign either way", so this is recorded as Observation 3. |
| T14 constructor services | pass | **s8:** the pure-computation formatter comes through the constructor and is registered on both tiers; the audit trail and the database are `[Service]` parameters on `[Remote] internal` writes (out8:7-8, 173-178, 219-240).<br>**s3:** the rule's constructor takes the command delegate, never the DbContext (out3:66-75).<br>**s4:** the server-only `ICurrentSalesperson` is a `[Service]` on the `[Remote, Execute]` (out4:163-171).<br>**s1, s7:** view models take generated factories through their constructors (out1:440; out7:536-544).<br>No method-injected service is stored in a field anywhere. |
| T15 `[Remote]` on a child's `[Insert]` | pass | **s1, s4, s6:** no child or list operation carries `[Remote]`; the child `Create` is `public` and local (out1:331-367; out4:276-277, 311-340; out6:205-250, 553-563). |
| T18 does `[Remote]` mean "runs on the server" | **run2: fallen into (s10, prose); run3: pass** | **s10, run2:** the prose gives the habit's answer (out10:1); see Verdict. **s10, run3:** `[Remote]` is the call crossing: "If an operation is marked `[Remote]`, the call goes over the wire"; "the client's call crosses" (run3 out10:5, 113). Its remaining "Without `[Remote]`, the method runs in the browser" is Observation 2.<br>**s6:** child operations "only run inside `Company`'s server operations, which have already crossed" (out6:561, 563).<br>**s1, s4:** the same placement reasoning (out1:367; out4:277).<br>**s5, s7:** server code calls the `[Remote]` Fetch and Save directly, with no second hop (out5:224-239; out7:406-425).<br>**s8:** "the `[Remote] internal` write operations … run on the server" (out8:8, 147) describes `[Remote] internal` methods, for which it is true. |
| T19 a command that needs the server | pass | **s3:** a `[Remote, Execute]` for the server call (out3:34-45).<br>**s5:** an `internal static [Execute]` without `[Remote]`, because no client starts the job: "A `[Remote]` command would open a public endpoint … A plain static `[Execute]` would ship the EF query to the client" (out5:202-204, 261). |
| T20 a DTO from a controller | pass | No controller and no DTO class in any output. A screen's server truth is a `[Fetch]`-only read model (out2:193-210; out7:187-235; out9:52-90).<br>**Hand-written records that cross:** `ContactListItem` (out7:198), `RecentContact`, `RegisteredCompany` (out9:113) and `ContactValues` (out7:47). Each is a value inside a read model, an interface-factory return or the merge model, never a transfer type that stands in for a domain object (R-H).<br>**Framing:** "no DTOs and no controllers" in s1, s2, s3, s6, s7 and s10 (out1:15; out2:482; out3:300; out6:9; out7:187; run3 out10:5). |

## The four findings

| Finding | Reached by | Result |
|---|---|---|
| F-a "persistence routing" as the headline | s10 (run2 and run3) | Not fallen into. s10 opens with one domain assembly on both tiers, so there are "no controllers or DTOs" (run3 out10:5; out10:1). "Persistence routing" appears in no output. |
| F-b N+1 as the reason child operations are `internal` | s6, s4, s1 | Not fallen into. The reason given is placement: the operations "only run inside `Company`'s server operations, which have already crossed. `[Remote]` here wouldn't add a round trip, but it would give the client a way in that bypasses the company" (out6:561). s4 says "they only run inside the visit's save, which is already on the server" (out4:277), and s1 the same (out1:367). "N+1" appears in no output. |
| F-c a repository-named interface factory | s9 | Not fallen into. The run's only interface factory is `ICompanyRegistry`, "named for what it does. It is not a repository" (out9:106-118). |
| F-d `[Remote, Create]` where creation needs nothing | s10, s7, s1-s6, s8 | Not fallen into. Each `[Remote, Create]` needs server data and says so: s10's contact reads its default region from the Settings table (out10:218-222; run3 out10:113-116); s7's merge model reads both contacts (out7:238, 320). Every other root `Create` is local: out1:219, out2:163, out3:163, out5:66, out6:94, out8:195, and the note in run3 out10:149, 183. s4's `Create` is `internal` and called only by `OpenToday`, because the salesperson comes from the server (out4:124, 178). |

## Observations (no row fallen into)

1. **s7's merge model is a `ValidateBase` with no row, built by a `[Remote, Create]` that reads both contacts** (out7:238, 320-351). That is scored as a pass on T4 and F-d. The attribute is a claim about the object: the merge is not in the store and is never saved, so "not in the store yet" holds. Building it needs the server (D14). It is not a loaded child claiming to be new, which is what T4 forbids. Recorded because it differs from the checkout context's shape, a `[Fetch]` that holds the loaded aggregate.
2. **run3's s10 still says "Without `[Remote]`, the method runs in the browser"** (run3 out10:5). In s10 every call comes from the client, so this is true of every call it makes, and the same paragraph says `[Remote]` marks the call going over the wire. Recorded rather than re-run again.
3. **s7 saves on the server without reassigning** (out7:423-425). The row says "reassign either way". Neither object is used afterwards, so nothing reads a stale instance. Recorded for the user.
4. **s6 and s10 tried to read the repository and were denied.** s6 tried the exact paths named in the skill's mdsnippets "snippet source" links. A distributed copy has no checkout behind those links, so this affects only a test run on this machine.
5. **s7 opens the transaction in its merge command** (out7:421). The command saves two roots, and VisionAlignment section 3 puts a cross-root operation in an `[Execute]` seam.
