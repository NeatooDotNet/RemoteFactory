# EXRM-002 — Code Review

**Date:** 2026-09-08
**Reviewer:** code-reviewer (per-plan pass, opt-in; budget: tight)
**Object:** `git diff EXRM...HEAD` at `8f02dfc` — test coverage, Design samples, and one admitted generator fix
**Verdict:** CLEAN

**Logs:** build main + design PASSED (0 errors, CS8603 not in `NoWarn`); tests 777 UT / 631 IT (626 passed, 5 skipped) / 102 DT per TFM, 0 failed.

## The generator fix

- **(a) Omitting the `IsBool` term is right.** That term serves bool-returning Read; class Execute returns `Task<T>` of the containing type.
- **(b) All four render sites agree.** `GetReturnType(includeAuth: false)` now falls to the `IsNullable` arm for the public method and the interface signature (generated `IClassExecLocalAuthFactory.Run` and `RunRemote` are both `Task<ClassExecLocalAuth?>`); `includeAuth: true` short-circuits on `HasAuth` to `Authorized<T>` for the remote and local halves. No interface/implementation or remote/local mismatch.
- **(c) Nothing else changes.** `isNullable` adds nothing when `authorization == null || !HasAuth`, so no non-authorized Execute and no other operation is affected. The only other in-repo `[Execute]`-plus-auth sites are `PayrollOperations` (a `[SuppressFactory]` static delegate) and interface factories, neither routed through `BuildClassExecuteMethod`.
- **(d) The wire is sound.** It carries `Authorized<T>` (never null); `.Result` yields the null on the client — pinned by `RemoteExecute_WithAuth_Denied_StillCrossesTheWireOnce` and `BareExecute_AspAuthorize_Denied_ReturnsNull_StillCrossesTheWireOnce`.

`RecordingAspAuthorize` is a faithful double: null ≡ empty ≡ access per `Authorized.cs:42`, text ≡ denial, and `forbid: false` is what the generator emits. The combination Local targets are distinct types with their own `_Result` and registrar — bare, `public static`, `Task<T>` — so no collision; `ExecuteBehaviorTests` gained a region only.

## Veto-tier

None.

## Callouts — Must

None.

## Callouts — Should/Could

All three were stale references to `ClassExecuteWithAuth_FactoryMethod_IsNullable`, the test renamed in response to test-review round 1 while this review was reading.

| # | Finding | Affects | Disposition |
|---|---|---|---|
| 1 | `plans/002:115` names the old test method | AC-4 (Should) | Already corrected before the report landed; verified absent |
| 2 | `plans/002:119` still asserts the retracted "would not compile" claim | AC-4 (Should) | Already corrected before the report landed; verified absent |
| 3 | `ClassExecuteAuthTests.cs:14` dangling `<see cref="…FactoryMethod_IsNullable"/>` | AC-4 (Should) | **Real and fixed** — the class remarks now attribute the signature to the build (CS8603 under `TreatWarningsAsErrors`) rather than to a test method. `GenerateDocumentationFile` is not enabled, so CS1574 was latent, but the claim was wrong regardless |

Rebuilt and retested after the fix: build succeeded, 777 / 631 per TFM, 0 failed.

## Theoretical (not triaged)

- The real `AspAuthorize.Authorize` returns `authenticateResult.message` on authentication failure with `forbid: false`; a null there reads as access via the implicit conversion. Pre-existing, unrelated to this plan.
- The denied-remote round trip is proven through the serializing stand-in, not real HTTP.

## Read report

Beyond the brief: `Authorized.cs`, `AspAuthorize.cs`, `src/Directory.Build.props` `NoWarn`, the on-disk `Generated/` factories for the four new targets plus `ExecuteCombinations.g.cs`, and the pre-existing `[Execute]`-plus-auth sites in Design, the reference app, TrimmingTests and UnitTests to settle question (c). Named but unused: `plans/001-static-delegates-obey-remote.md`, `reviews/001-code-review.md`. Note: `git ls-files` shows no `Generated/` files are tracked, so question (c) was answered from the working tree rather than the diff.
