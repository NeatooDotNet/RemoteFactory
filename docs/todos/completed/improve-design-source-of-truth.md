# Improve Design Source of Truth for Claude Code

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-01
**Last Updated:** 2026-02-01

---

## Problem

The Design source of truth (`src/Design/`) is effective for Claude Code, but a Claude Code expert review identified several improvements that would make it even more useful for evaluating designs and understanding patterns.

## Solution

Add documentation sections to CLAUDE-DESIGN.md and Design files that provide clearer guidance, prevent common mistakes, and optimize for Claude Code's pattern recognition.

---

## Plans

---

## Tasks

### High-Value Additions

- [x] **Add "WHEN TO USE THIS PATTERN" decision table** in CLAUDE-DESIGN.md
  - Class Factory: Aggregate roots with lifecycle, entity management
  - Interface Factory: Remote services without entity identity
  - Static Factory: Stateless commands, events, side effects

- [x] **Create "ANTI-PATTERNS" section** in CLAUDE-DESIGN.md
  - Document what NOT to do at the pattern level
  - Include examples of wrong vs. right approaches
  - Cover entity duality mistakes, [Remote] misuse, etc.

- [x] **Add quick reference "DECISIONS TABLE"** in CLAUDE-DESIGN.md
  ```markdown
  | Question | Answer | File | Reason |
  |----------|--------|------|--------|
  | Should this method be [Remote]? | Only aggregate root entry points | Order.cs vs OrderLine.cs | Client/server boundary |
  | Can I use private setters? | No | AllPatterns.cs:73 | AOT compilation + source generation |
  | Should interface methods have attributes? | No | AllPatterns.cs:203 | Interface IS the boundary |
  ```

- [x] **Add "How to Use This Reference" section** in CLAUDE-DESIGN.md
  - Explain workflow for Claude to use the Design files
  - 1. To understand a pattern: Read relevant file in Design.Domain/
  - 2. To verify syntax: Check Design.Tests/ for working examples
  - 3. To propose a change: Cross-reference against "DID NOT DO THIS" sections
  - 4. To understand generator behavior: Look for [GENERATOR BEHAVIOR] comments

### Additional Improvements

- [x] **Add front matter** to CLAUDE-DESIGN.md for quick reference
  ```yaml
  ---
  design_version: 1.0
  last_updated: 2026-02-01
  target_frameworks: [net8.0, net9.0, net10.0]
  ---
  ```

- [x] **Add "INTEGRATION CHECKLIST"** for design completeness validation
  - [x] At least one Class Factory with lifecycle hooks
  - [x] At least one Interface Factory
  - [x] Child entities without [Remote]
  - [x] Event handlers with CancellationToken

- [x] **Consider "DESIGN DEBT" or "FUTURE DESIGN" section**
  - Document open questions with reasoning why deferred
  - Prevents Claude from repeatedly proposing same trade-offs

- [x] **Add "SERIALIZATION ROUND-TRIP" guide** in Design.Tests
  - Show which types can cross the boundary
  - Why IOrdinalSerializable matters
  - Two DI container test pattern visual

---

## Progress Log

**2026-02-01:** Created todo based on Claude Code expert agent review of Design source of truth effectiveness. The review concluded the current implementation is "highly effective" and "emphatically worth the maintenance" but identified these improvements to maximize value.

**2026-02-01:** Implemented all improvements:
- Updated `src/Design/CLAUDE-DESIGN.md` with:
  - Front matter (design_version, last_updated, target_frameworks)
  - "How to Use This Reference" workflow section
  - "When to Use Each Pattern" decision table with detailed guidance
  - "Quick Decisions Table" with 9 common questions and file references
  - "Anti-Patterns" section with 7 detailed anti-patterns including wrong/right code examples
  - "Design Completeness Checklist" for validating pattern coverage
  - "Design Debt and Future Considerations" table documenting deferred trade-offs
- Updated `src/Design/Design.Tests/FactoryTests/SerializationTests.cs` with:
  - Comprehensive "SERIALIZATION ROUND-TRIP GUIDE" header documentation
  - Types that can/cannot cross the boundary
  - Explanation of IOrdinalSerializable and how it works
  - ASCII visual diagram of the two DI container test pattern
  - Usage examples for client, server, and local scopes
- All 26 Design.Tests pass across net8.0, net9.0, and net10.0

---

## Results / Conclusions

All improvements from the todo have been implemented. The CLAUDE-DESIGN.md file is now significantly more comprehensive:

1. **Front matter** provides quick version/framework reference
2. **How to Use This Reference** guides Claude through the workflow
3. **When to Use Each Pattern** decision table clarifies pattern selection
4. **Quick Decisions Table** answers 9 common questions with file references
5. **Anti-Patterns** section documents 7 detailed anti-patterns with code examples
6. **Design Completeness Checklist** enables validation of pattern coverage
7. **Design Debt table** documents 5 deferred considerations with reasoning
8. **Serialization Round-Trip Guide** in SerializationTests.cs provides visual explanation of the two DI container pattern

The Design source of truth is now optimized for Claude Code to:
- Quickly find answers to common questions
- Understand the rationale behind design decisions
- Avoid anti-patterns with clear wrong/right examples
- Verify pattern coverage with the checklist
- Understand why certain features are intentionally not implemented
