# AwaitRemote Support for IFactoryEvents

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-04-09
**Last Updated:** 2026-04-09

---

## Problem

`RaiseOptions.AwaitRemote` is defined in the API but not implemented. In Remote mode, `IFactoryEvents.Raise()` always uses fire-and-forget semantics regardless of whether `AwaitRemote` is set. The HTTP connection closes after server acknowledgment — the client cannot await full handler completion on the server.

## Solution

Implement AwaitRemote HTTP semantics:
- When `AwaitRemote` is set, the server runs handlers synchronously within the request scope and returns the result (or error) before closing the HTTP connection.
- When `AwaitRemote` is not set (default), current fire-and-forget behavior is preserved.

This requires changes to `RemoteFactoryEvents`, the server-side `RaiseFactoryEventRemote` delegate handler, and potentially the HTTP endpoint to distinguish awaited vs fire-and-forget requests.

---

## Requirements Review

**Verdict:** Pending
**Reviewed:**
**Summary:**

---

## Plans

---

## Tasks

- [ ] Design HTTP protocol for AwaitRemote (flag in request, synchronous response)
- [ ] Implement server-side synchronous handler execution for AwaitRemote
- [ ] Update RemoteFactoryEvents to pass AwaitRemote flag
- [ ] Add integration tests for AwaitRemote across client/server boundary
- [ ] Update plan business rules 10/11 to reflect v2 scope

---

## Progress Log

### 2026-04-09
- Created from factory-events-mediator todo — AwaitRemote intentionally deferred to v2
- Business rules 10 and 11 in the parent plan need updating to reflect deferral

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass

**Verification results:**
- Build: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

