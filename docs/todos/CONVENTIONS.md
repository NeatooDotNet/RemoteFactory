# Todo Conventions

This directory holds iterative todos managed with the `iterative-todo` workflow
(durable todo container + small amendable plans + append-only Discovery Log).

## Structure

```
docs/todos/
  CONVENTIONS.md                  # this file
  _ids.md                         # ID registry — one row per todo, IDs never reused
  {ID}-{kebab-name}/              # one folder per active todo
    todo.md                       # goal, acceptance, Plan Index, Discovery Log
    plans/{NNN}-{short-name}.md   # monotonic numbering; Abandoned/Retired kept
    reviews/                      # gate output + build/test logs (logs gitignored)
  completed/{ID}-{kebab-name}/    # finished todos move here, ID prefix preserved
```

## IDs

- 3–5 uppercase letters, assigned at todo creation, registered in `_ids.md`.
- Unique forever — retired IDs are never reused.
- Cross-reference plans as `{ID}-{NNN}` (e.g. `TRIM-004`), never bare `Plan 004`.

## Branching (this repo)

- Each todo gets a branch named `{ID}` off `main`; todo/plan documentation commits
  land there.
- Each plan's implementation gets its own branch `{ID}-{NNN}-{short-name}` off the
  todo branch.
- **Plan PRs target their arc branch `{ID}`; the arc PRs to `main` at close-out.**
  CI covers both: `build.yml`'s `pull_request` trigger watches `main` *and* arc
  branches (`[A-Z][A-Z][A-Z]*`), added 2026-08-14 when the PHASE arc adopted this
  flow. Without that trigger a PR into an arc branch gets no build — the gap that
  left the TRIM arc branch unbuilt and stale (TRIM dropped arc PRs for that reason;
  its note records the history).
- After the arc merges, pull `main` back into any live arc branch and continue.
- A plan branch that must build on an unmerged predecessor stacks on it
  (`{ID}-{NNN}` off `{ID}-{MMM}`) rather than off the arc branch; record the stack in
  the plan's Notes and merge in order.

## Commits

Conventional commits per the repo root `CLAUDE.md` (`feat:`/`fix:` drive release
notes; `docs:`/`test:`/`chore:` are omitted). Todo bookkeeping commits use `docs(todo):`.
