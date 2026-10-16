# Core references as compiled mechanism

**Plan #:** 003
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** Draft
**Last Updated:** 2026-10-16
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** —
**PR:** —

---

## Scope

Rebuild `class-factory.md`, `interface-factory.md`, `static-factory.md`, `service-injection.md`, `setup.md` and `anti-patterns.md` as mechanism pages under the model: every C# block a `src/Design` region, every RIGHT half of an anti-pattern replaced by an embed or a sentence, and the WRONG blocks kept short. The repository-named interface factories (`IOrderRepository`, `IEmployeeRepository`, `IMyRepository`) become domain services or `[Service]` parameters (#112); the blocks naming the retired `SkillConsultation`, `SkillEmployeeCommands` and `SkillEmployeeEvents` types go (#148); method injection stops being titled "Server Only" and the child `Create` is shown `public` (T14, R-C). Does NOT touch `factory-events.md`, `trimming.md`, `polymorphic-hierarchy.md` or `advanced-patterns.md` (004).
