# Documentation Onboarding Improvements Plan

**Date:** 2026-02-01
**Related Todo:** [Documentation Onboarding Improvements](../todos/documentation-onboarding-improvements.md)
**Status:** In Progress
**Last Updated:** 2026-02-01

---

## Overview

Address junior developer feedback by adding supplementary documentation that helps newcomers (of any experience level) get started faster, without rewriting existing experienced-developer-focused content.

---

## Guiding Principles

1. **ADD, don't replace** - Existing docs stay as-is; we add new entry points
2. **Experienced developers first** - New content provides shortcuts, not hand-holding
3. **Reference over tutorial** - Quick lookups beat lengthy explanations
4. **Progressive disclosure** - Simple overview → detailed docs for those who need it

---

## Proposed Changes

### Tier 1: High Value, Low Risk (Recommended)

These additions help ALL newcomers without changing existing content.

#### 1.1 Create "Attribute Quick Reference" Page

**New file**: `docs/attributes.md`

A single-page reference listing all attributes with one-line descriptions and links to detailed docs.

| Attribute | Purpose | Details |
|-----------|---------|---------|
| `[Factory]` | Marks a class for factory generation | [Factory Modes](factory-modes.md) |
| `[Remote]` | Entry point from client to server | [Client-Server](client-server.md) |
| `[Create]` | Factory creation method | [Factory Operations](factory-operations.md) |
| `[Fetch]` | Factory retrieval method | [Factory Operations](factory-operations.md) |
| ... | ... | ... |

**Why this helps**: The junior review said "there are SO many attributes" and "I have to piece it together from examples." This gives experienced developers a quick lookup without wading through tutorials.

#### 1.2 Create "Hello RemoteFactory" Minimal Example

**New file**: `docs/hello-remotefactory.md`

The absolute simplest possible example:
- One class with ONE method (`[Fetch]`)
- No `IFactorySaveMeta`, no CRUD, no repositories
- Shows exactly what you write vs what gets generated
- < 50 lines of code total

**Why this helps**: Current Getting Started jumps to a full Employee with Insert/Update/Delete/Fetch. A senior developer new to RemoteFactory also benefits from seeing the minimal case first.

#### 1.3 Add "When to Use" Decision Guide

**New file**: `docs/decision-guide.md`

Quick decision trees for common choices:

**When do I need `[Remote]`?**
```
Is this method called directly from client code?
  YES → Add [Remote]
  NO (called from another server method) → No [Remote] needed
```

**Constructor vs Method Injection?**
```
Does the client need this service too?
  YES → Constructor injection with [Service]
  NO (server-only, e.g., DbContext) → Method parameter with [Service]
```

**Which serialization format?**
```
Deploying client and server together?
  YES → Ordinal (smaller payloads)
  NO (independent deployments) → Named (version tolerant)
```

**Why this helps**: The junior said "the docs explain HOW but not WHEN." This is useful for experienced developers too - they want quick answers, not theory.

---

### Tier 2: Medium Value, Low Risk (Consider)

Small improvements to existing pages.

#### 2.1 Add One-Sentence "Why" to Index Page

Current index explains WHAT RemoteFactory does. Add 2-3 sentences about WHY:

> **The Problem**: In traditional 3-tier .NET apps, you write domain classes, then duplicate that structure in DTOs, then write controllers to expose them, then write client-side factories to call those controllers. Changes ripple through all layers.
>
> **RemoteFactory's Solution**: Write your domain classes once with attributes. The source generator creates the factories, serialization, and HTTP endpoints. One source of truth, no duplication.

#### 2.2 Add Inline Terminology Links

Where we use terms like "entity duality" or "logical mode", add brief parenthetical definitions or link to glossary:

Before: "An entity can be an aggregate root in one object graph and a child in another."

After: "An entity can be an aggregate root in one object graph and a child in another (the same `Order` class might be fetched directly as a root, or loaded as part of a `Customer` aggregate)."

#### 2.3 Clarify `[Remote]` in Client-Server Page

The junior was very confused about `[Remote]`. Add a concrete "rule of thumb" box:

> **Rule of Thumb**: If you're adding a method that the Blazor UI will call directly via the factory, add `[Remote]`. If the method is only called from other server-side code (like a child entity's method called during Save), no `[Remote]` needed.

---

### Tier 3: Lower Priority (Future)

These would help but require more effort.

#### 3.1 Add Error Handling Section

Document what happens when:
- Network fails (what exception type?)
- Server method throws (how does it propagate?)
- Authorization fails

#### 3.2 Add "Is RemoteFactory Right for Me?" Section

When to use RemoteFactory vs alternatives. Who benefits most.

#### 3.3 Create Glossary Page

Define: Logical mode, entity duality, HTTP stubs, factory modes, etc.

---

## What We're NOT Doing

Based on the constraint to keep experienced developers as the priority:

1. **NOT rewriting existing pages** for beginners
2. **NOT adding step-by-step tutorials** with screenshots
3. **NOT explaining DDD fundamentals** (per CLAUDE.md guidelines)
4. **NOT simplifying technical accuracy** for accessibility

---

## Implementation Steps

### Phase 1: Quick Reference (Tier 1.1)
1. Create `docs/attributes.md` with attribute table
2. Add to navigation
3. Verify all attributes are covered

### Phase 2: Minimal Example (Tier 1.2)
1. Create `docs/hello-remotefactory.md`
2. Write simplest possible working example
3. Show generated code side-by-side
4. Link from Getting Started as "new to RemoteFactory?"

### Phase 3: Decision Guide (Tier 1.3)
1. Create `docs/decision-guide.md`
2. Add decision trees for common choices
3. Link from relevant pages

### Phase 4: Existing Page Tweaks (Tier 2)
1. Add "why" paragraph to index
2. Add `[Remote]` rule of thumb to client-server page
3. Clarify confusing terminology inline

---

## Acceptance Criteria

- [ ] Attribute reference page exists with all attributes documented
- [ ] Minimal example shows < 50 lines of code
- [ ] Decision guide answers "when do I need [Remote]?"
- [ ] Existing documentation unchanged except for small clarifications
- [ ] Experienced developers can still find detailed information quickly

---

## Dependencies

- Review of current docs to inventory all attributes
- Identification of simplest possible RemoteFactory example

---

## Risks / Considerations

1. **Scope creep** - Easy to keep adding "one more beginner thing." Stay focused on Tier 1.
2. **Navigation clutter** - New pages need logical placement without overwhelming nav.
3. **Maintenance burden** - Minimal example and attribute reference need updating when API changes.

---

## Open Questions for User

1. **Tier 1 approval** - Do the three proposed new pages (Attribute Reference, Hello RemoteFactory, Decision Guide) seem valuable?

2. **Tier 2 scope** - Should we make the small inline improvements to existing pages, or keep changes strictly additive?

3. **Navigation placement** - Where should new pages appear? Suggestions:
   - Top-level "Quick Start" section before "Getting Started"?
   - Or as sub-pages under Getting Started?
