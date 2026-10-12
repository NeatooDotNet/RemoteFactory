# Create-or-fetch and value objects

**Plan #:** 004
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

Add a create-or-fetch seam: a class-level `[Remote, Execute]` that calls the factory's Fetch, then Create (D11), replacing `ClassExecuteDemo`'s use of `new`. Make `Money` and `Percentage` plain records whose constructors throw on an invalid value, and add one value object whose construction needs a service and therefore carries `[Factory]` (R-F). Move the event examples that raise inside a `[Create]` (`CheckoutOrder`, `Invoice.Finalize`, `InvoiceArchiver.Archive`) onto a write operation or a command (D3, C-06), keeping their tests' intent. Does not touch `docs/` pages.
