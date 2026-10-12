# Domain service, read model, context

**Plan #:** 003
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2, AC-4
**Status:** Draft
**Last Updated:** 2026-10-11
**Plan-review opt-in:** set at Step 2
**Code-review opt-in:** set at Step 2
**Branch:** cut from EXSET at Step 2
**PR:** —

---

## Scope

Replace `IExampleRepository` and `IAuthorizedRepository` with domain-service interface factories named for what they do (#112, D12), including the authorized variant, and remove repository language from CLAUDE-DESIGN and the Blazor page. Add a read model, a `[Fetch]`-only `[Factory]` class the client holds using the `bool`/`false` not-found signature (R-H, R-I), and a context (D13), a plain `[Factory]` instance a screen binds to whose verbs call the held `Order`'s verbs and persist through its root `Save`. Tests on the client/server containers. Does not touch `docs/` pages.
