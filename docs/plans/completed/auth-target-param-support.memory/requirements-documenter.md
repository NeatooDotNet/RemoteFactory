# Requirements Documenter -- Auth Target Parameter Support

Last updated: 2026-04-06
Current step: Requirements Documented

## Key Context

PR #58 added two features: type-based parameter matching for auth methods and target parameter authorization on write operations. No formal plan file exists in docs/plans/; the orchestrator relayed context directly.

All documentation was reviewed against generator source code (FactoryGenerator.Transform.cs, FactoryModelBuilder.cs, ClassFactoryRenderer.cs). Generator behavior confirmed:
- Target param detection: auth method params matched by display string against both serviceSymbol (interface) and typeSymbol (concrete class)
- CanXxx suppression: FactoryModelBuilder.AddCanMethods skips methods whose auth methods have any IsTarget param
- Type-based matching: auth params matched by type to factory method params, consumed in order

## Mistakes to Avoid

None identified.

## User Corrections

None.

## Documentation Tracking

### Expected Deliverables

From PR #58 scope:
1. Design project files demonstrating both features
2. CLAUDE-DESIGN.md Quick Decisions Table entries
3. CLAUDE-DESIGN.md Design Completeness Checklist update
4. CLAUDE-DESIGN.md Design Files table update
5. docs/authorization.md new sections
6. skills/RemoteFactory/references/advanced-patterns.md updates

### Requirements Documentation Updated

| File | What Changed | Why |
|------|-------------|-----|
| `src/Design/CLAUDE-DESIGN.md` | Added checklist item for parameterized auth (line ~789) | New pattern demonstrated in Design project |
| `src/Design/CLAUDE-DESIGN.md` | Added ParamAuthOrder.cs, ParamAuthOrderAuth.cs, ParamAuthorizationTests.cs to Design Files table | New files need to be discoverable |
| `src/Design/CLAUDE-DESIGN.md` | 3 Quick Decisions Table entries (pre-existing, verified accurate) | Type-matched params, target params, CanSave suppression |
| `docs/authorization.md` | Two new sections: "Parameterized Authorization Methods" and "Target Parameter Authorization" (pre-existing, verified accurate) | Published docs for new features |
| `skills/RemoteFactory/references/advanced-patterns.md` | Two new subsections under AuthorizeFactory (pre-existing, verified accurate) | Skill reference consistency |

### Developer Deliverables

None. All source code changes were already merged (PR #58). Design project files are already committed.

### Step 8 Part B Needed?

No general documentation deliverables identified -- Step 8 Part B can be skipped. Release notes (v0.29.0.md) were already merged with the PR.
