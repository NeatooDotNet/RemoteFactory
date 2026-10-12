# ASP.NET Core, modes, serialization, setup

**Plan #:** 006
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

Migrate the `aspnetcore-*`, `modes-*` and `serialization-*` regions into Design (`Design.Server`, `Design.Client.Blazor`, `Design.Tests`), re-point their embeds in `aspnetcore-integration.md`, `factory-modes.md`, `serialization.md` and the skill's `setup.md`, and delete the reference-app sources they replace. Fix those pages' findings: the request/response DTO shapes, the 409 and event-hosting claims, Remote-mode wording, and the two-container testing sample that round-trips nothing.
