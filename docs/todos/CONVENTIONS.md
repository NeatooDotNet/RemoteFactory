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
- PRs target `main` (CI's `pull_request` trigger only watches `main`); after merge,
  pull `main` back into the todo branch and continue.

## Commits

Conventional commits per the repo root `CLAUDE.md` (`feat:`/`fix:` drive release
notes; `docs:`/`test:`/`chore:` are omitted). Todo bookkeeping commits use `docs(todo):`.
