# RFSK-006 — Fresh-session test, scored

**Date:** 2026-10-16
**Skills under test:** `~/.claude/skills/RemoteFactory`, byte-identical to the repository's `skills/RemoteFactory` at the arc's `d85feaa` (`diff -rq` empty), with the installed `neatoo` and `mudneatoo` skills beside it.
**Mechanism:** as SKR-007. One headless `claude -p` per scenario (Claude Code 2.1.301), each started in its own empty scratch directory outside the repository, so there was no repository `CLAUDE.md`, no project memory and no `src/Design`. `--allowedTools "Skill Read Glob Grep"`: the sessions could read the installed skills and nothing else, and could not write files. The user-global `~/.claude/CLAUDE.md` loads in every session; it holds no RemoteFactory doctrine. Each session's stderr carried only a settings notice about a `Write(...)` allow rule.
**Prompts and outputs:** `006-runs/s1.txt`–`s10.txt` and `006-runs/out1.md`–`out10.md`. s1–s5 are SKR-007's prompts, verbatim. s6, s8 and s10 use a preamble that says "using the RemoteFactory library, without Neatoo", so the RemoteFactory skill is the only design guidance.
**Scorer:** the eleven RemoteFactory trap rows in `SKILL.md` (VisionAlignment section 4: T3, T4, T5, T6, T11's RemoteFactory half, T13, T14, T15, T18, T19, T20) and SKR-007's four open findings (F-a the "persistence routing" framing, F-b N+1 as the reason child operations are `internal`, F-c a repository-named interface factory, F-d `[Remote, Create]` where creation needs nothing from the server). A row is fallen into when the output does what the row's question assumes. "—" means the scenario gives the row no occasion. Every output was screened for each row's signature (controllers and DTO classes, `interface I…Repository`, "N+1", "persistence routing", `[Remote, Create]`, `[Remote]` on a child operation, an unassigned `Save`, a stored method-injected service), and each hit and each scenario's governing passage was read. Line references are into the outputs.

## Verdict

**No scenario falls into any RemoteFactory trap row or any of the four findings.** AC-7 is met. Five rows SKR-007 never exercised (T4, T6, T11, T14, T18) each have a scenario that reaches them, and all pass.

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

| Row | Passes | Evidence |
|---|---|---|
| T3 load-or-create | s4 | `Visit.OpenToday` is a class-level `[Remote, Execute]` returning `FetchForDay(...) ?? Create(...)` (out4:126-138) |
| T4 `[Create]` from loaded data | s1, s4, s6 | Loaded children are built by their own `Fetch(row)`, handed by the list (out6:98-107, 169-175; out1:315-322; out4:263) |
| T5 the client and the repository | s2, s3, s9 | Persistence is a server-only `[Service]` (out2:130, 154; out3:54, 74); what the client calls is a read model (out9:68, 90-104) or a domain service named for what it does, `ICompanyRegistryLookup` (out9:147). No `I…Repository` in any output |
| T6 the application service | s3, s7 | A static `[Remote, Execute]` command when the call returns and is done (out3:67-74); a class-level `[Remote, Execute]` returning the kept aggregate for the merge (out7:213-249) |
| T11 who writes a child's row | s1, s4, s6 | The root hands its row's child collection to the list factory's `Save`; the list makes, finds or removes each child's row; the child maps itself (out6:180-207, 281-300; out1:161, 185, 326-347; out4:197, 221, 273-293) |
| T13 `Save()` and the object | s1, s4, s5, s6, s7 | Reassigned everywhere it is kept (out1:567; out4:605; out5:215; out6:404-415; out7:243-246). The one unassigned `Save` is a deleted duplicate whose result is not used (out7:243) |
| T14 constructor services | s1, s7, s8 | The pure-computation formatter through the constructor, registered on both tiers; the audit trail and the database context as `[Service]` on `[Remote]` operations, server-only (out8:7-9, 134, 178-199, 286-292, 339). Generated factories through the list's constructor (out1:296-301) |
| T15 `[Remote]` on a child's `[Insert]` | s1, s4, s6 | No child operation carries `[Remote]` (out1:240-262; out4:334-355; out6:87-118) |
| T18 does `[Remote]` mean "runs on the server" | s6, s9, s10 | `[Remote]` is explained as where the client's call crosses, by where the services exist (out6:425-429; out9:410-413; out10:5); a `[Remote]` call made on the server runs locally (out7:218-219; out4:127) |
| T19 a command that needs the server | s3, s5 | `[Remote, Execute]` for the server call the rule makes (out3:54, 74). The nightly job is a server class, not a `[Factory]` command, because no client starts it (out5:150) |
| T20 a DTO from a controller | s2, s9, s10 | No controller and no DTO class in any output; a screen's server truth is a `[Fetch]`-only read model (out2:141-154; out9:68-104); "no DTOs and no controllers" is the framing (out10:5) |

## The four findings

| Finding | Reached by | Result |
|---|---|---|
| F-a "persistence routing" as the headline | s10 | Not fallen into. RemoteFactory is "one domain assembly … in both the Blazor WebAssembly client and the ASP.NET Core server, so we write no DTOs and no controllers" (out10:5). The phrase appears in no output |
| F-b N+1 as the reason child operations are `internal` | s6 (asked directly), s1, s4 | Not fallen into. The reason given is placement: the child operations "only run inside `Company.Fetch` / `Update`, which are already on the server. Marking them `[Remote]` would put them on the client's factory interface and give the client a way around the root" (out6:429). "N+1" appears in no output |
| F-c a repository-named interface factory | s9, s2, s3 | Not fallen into. No `I…Repository` type anywhere; the interface factory is `ICompanyRegistryLookup` (out9:147) |
| F-d `[Remote, Create]` where creation needs nothing | s10, s6, s1, s4 | Not fallen into. The one `[Remote, Create]` is the contact whose default region is in the server's Settings table, and it says why (out10:31, 59); the note beside it is a local `public` `[Create]` (out10:96). Every other root and child `Create` is local |

## Observations (no row fallen into)

1. **s4 makes the visit's `Create` `internal`, server-only, because the salesperson comes from the request's identity and only `OpenToday` starts a visit** (out4:139-155). That is the `[Create]` rule applied: it needs the server, and nothing else should call it. Recorded because it differs from the common shape, a `public` local root `Create`.
2. **s7 opens its own transaction inside the class-level `[Execute]` through `db.Database`** (out7:233). The model puts the transaction in the root's operation; here the operation is the merge seam, which saves two roots, so the seam owns it. Recorded for the user; no row covers a two-root transaction.
