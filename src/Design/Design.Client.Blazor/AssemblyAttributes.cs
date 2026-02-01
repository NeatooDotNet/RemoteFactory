// =============================================================================
// DESIGN SOURCE OF TRUTH: Assembly-Level Factory Mode Configuration
// =============================================================================
//
// This file demonstrates using assembly-level attributes to configure
// factory behavior for an entire client assembly.
//
// =============================================================================

using Neatoo.RemoteFactory;

// -----------------------------------------------------------------------------
// [assembly: FactoryMode] - Sets the default factory mode for all factories
//
// DESIGN DECISION: RemoteOnly mode for Blazor WASM client projects
//
// In a Blazor WASM project, all factory operations should go to the server.
// Setting FactoryMode.RemoteOnly at assembly level ensures:
// 1. All factories serialize requests to the server
// 2. No local execution - server-only services aren't available anyway
// 3. Smaller client bundle - no local implementation code generated
//
// AVAILABLE MODES:
// - FactoryMode.RemoteOnly: All operations go to server (client projects)
// - FactoryMode.Full: Local + remote execution (server projects)
// - FactoryMode.Logical: Single-tier, no serialization (testing, desktop apps)
//
// DID NOT DO THIS: Set mode per-class in client projects
//
// Reasons:
// 1. Consistency - all factories behave the same way
// 2. Less boilerplate - one attribute instead of many
// 3. Prevents mistakes - can't accidentally use Full mode on client
// -----------------------------------------------------------------------------

[assembly: FactoryMode(FactoryMode.RemoteOnly)]

// -----------------------------------------------------------------------------
// Alternative: Per-Class Factory Mode (when you need exceptions)
//
// If a specific class needs a different mode, apply [FactoryMode] at class level.
// Class-level attributes override assembly-level:
//
// [Factory]
// [FactoryMode(FactoryMode.Logical)]  // Override for this class only
// public partial class LocalOnlyCache { ... }
//
// Use cases for overriding:
// - Local-only caching that doesn't need server
// - Shared classes with different behavior per project
// - Testing scenarios within production code
// -----------------------------------------------------------------------------
