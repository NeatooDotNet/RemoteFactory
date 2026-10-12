# Service injection and skill-only embeds

**Plan #:** 010
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** Draft
**Last Updated:** 2026-10-11
**Plan-review opt-in:** set at Step 2
**Code-review opt-in:** set at Step 2
**Branch:** cut from EXSET at Step 2
**PR:** —

---

## Scope

Migrate the `service-injection-*`, `serialization-caveat-*`, `clientserver-container-*` and every remaining `skill-*` region into Design and re-point `service-injection.md` and the skill references, and fix `interface-factory.md` (no embeds today), deleting the reference-app sources and the duplicate keys. Fix `service-injection.md`'s findings: "server-only" versus "that tier's container", the client-supplied query string, the side-effecting `[Create]` constructor (the R-J warning), and child operations' visibility (R-C). Skill prose around the re-pointed embeds is left for Todo 2.
