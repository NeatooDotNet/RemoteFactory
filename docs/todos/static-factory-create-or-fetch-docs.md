# Document Static Factory "Create or Fetch" Pattern

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-02-07
**Last Updated:** 2026-02-07

---

## Problem

The documentation and skill lack a compelling, real-world use case for static factory methods. Static factories are for making decisions *before* object instantiation, but this isn't well illustrated with a concrete example.

## Solution

Document the "Create or Fetch" pattern: a static factory method that checks existing data (e.g., database lookup) and decides whether to Create a new object or Fetch an existing one. The static factory method takes `IFactory<T>` of its own type as a `[Service]` parameter, then calls either Create or Fetch on that factory based on the data.

This pattern demonstrates:
- **Why static factory methods exist** - the decision must happen before instantiation
- **Self-referencing factory injection** - a static method on `Foo` taking `IFactory<Foo>` as a service
- **Real business logic** - "get or create" is a universally understood pattern

### Key implementation detail

The static factory method receives `IFactory<T>` for its own type via `[Service]`, then delegates to Create or Fetch on that factory after making the decision.

---

## Plans

(None yet)

---

## Tasks

- [ ] Add "Create or Fetch" example to Design.Domain static factory samples
- [ ] Add tests in Design.Tests demonstrating the pattern
- [ ] Update documentation (`docs/`) with the pattern explanation
- [ ] Update skill (`skills/RemoteFactory/references/static-factory.md`) with the pattern
- [ ] Run mdsnippets to embed any new code samples

---

## Progress Log

### 2026-02-07
- Created todo to track this documentation work

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]

---

## Results / Conclusions

[What was learned? What decisions were made?]
