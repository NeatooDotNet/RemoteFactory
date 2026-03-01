# Convert All Applications to Hosted Blazor WebAssembly

**Status:** Complete
**Priority:** High
**Created:** 2026-02-28
**Last Updated:** 2026-02-28

---

## Problem

All Blazor WebAssembly applications in the repository (Design, Examples, Reference App) were structured as **standalone** Blazor WASM. This meant:

- Users must start **two separate projects** (server + client) to run any application
- Client projects hardcode server URLs (e.g., `http://localhost:5000/`)
- CORS must be configured as a workaround for cross-origin calls
- The developer experience is unnecessarily complex

This also affected user-facing guidance — RemoteFactory documentation and skill content instructed users to define a `BaseAddress` URL for the HttpClient, which is unnecessary with hosted WASM.

## Solution

Converted all applications to **hosted Blazor WebAssembly**, where the server project hosts the client:

1. Server references client project
2. Server calls `UseBlazorFrameworkFiles()` and `MapFallbackToFile("index.html")`
3. Single `dotnet run` starts everything
4. Client uses relative URLs (no hardcoded BaseAddress)
5. CORS configuration removed (same origin)

Additionally updated all user-facing guidance:
- Documentation (docs/)
- RemoteFactory skill (skills/RemoteFactory/)
- README files
- Design project README

### Affected Projects

| Group | Server | Client |
|---|---|---|
| Design | `src/Design/Design.Server` | `src/Design/Design.Client.Blazor` |
| Person Example | `src/Examples/Person/Person.Server` | `src/Examples/Person/PersonApp` |
| OrderEntry Example | `src/Examples/OrderEntry/OrderEntry.Server` | `src/Examples/OrderEntry/OrderEntry.BlazorClient` |
| Reference App | `src/docs/reference-app/EmployeeManagement.Server.WebApi` | `src/docs/reference-app/EmployeeManagement.Client.Blazor` |

---

## Plans

- [Convert All Applications to Hosted Blazor WebAssembly](../plans/completed/hosted-blazor-wasm-conversion.md)

---

## Tasks

- [x] Architect designs conversion plan
- [x] Developer reviews plan
- [x] Implement hosted WASM conversion across all projects
- [x] Update user-facing documentation and skill content
- [x] Architect verifies implementation

---

## Progress Log

### 2026-02-28
- Researched current project structure — confirmed all four project groups are standalone WASM
- Identified impact on user-facing guidance (no more BaseAddress URL configuration)
- Created todo
- Architect designed 6-phase conversion plan
- Developer reviewed and approved with 7 corrections
- Phases 1-4: Package config + Design + Person + OrderEntry converted
- Phase 5: Reference App (EmployeeManagement) converted with sample file updates
- Phase 6: mdsnippets run, docs/skill prose updated
- Architect independently verified: all builds pass, all tests pass, implementation correct

---

## Completion Verification

- [x] All builds pass (3 solutions, 0 warnings, 0 errors)
- [x] All tests pass (450+466+19+29+48 tests x 3 TFMs, zero failures)
- [x] Design project builds successfully
- [x] Design project tests pass (29 x 3 TFMs)
- [x] Each application starts with a single `dotnet run` on the server project
- [x] Client loads correctly when served by the host
- [x] RemoteFactory API calls work without CORS

**Verification results:**
- Build: All 3 solutions pass with 0 warnings, 0 errors
- Tests: All pass across net8.0, net9.0, net10.0

---

## Results / Conclusions

Successfully converted all four application groups from standalone to hosted Blazor WebAssembly.

Key outcomes:
- **Single startup**: Each application now requires only `dotnet run` on the server project
- **No hardcoded URLs**: Clients use `builder.HostEnvironment.BaseAddress` (same origin)
- **No CORS needed**: Server and client share the same origin in hosted mode
- **Documentation updated**: Docs, skill, and READMEs reflect hosted WASM pattern
- **CORS guidance preserved**: Recontextualized for non-hosted deployments (MAUI, WPF, separate origins)

Notable implementation finding: OrderEntry required `ReferenceOutputAssembly="false"` on the client ProjectReference due to its shared-source compilation pattern (Domain.Client and Domain.Server compile the same files).

Latent bug fixed: Design.Client.Blazor was hardcoded to `localhost:5000` but Design.Server ran on port 5085 — hosted WASM eliminates this class of configuration errors entirely.
