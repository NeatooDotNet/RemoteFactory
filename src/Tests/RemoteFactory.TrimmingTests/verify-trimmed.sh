#!/usr/bin/env bash
#
# Trimming absence gate. Asserts that server-only code is gone from a
# publish-trimmed client assembly, per factory shape.
#
# Usage:  verify-trimmed.sh <path-to-RemoteFactory.TrimmingTests.dll>
#
# WHY THIS IS A SCRIPT AND NOT SHELL-IN-YAML
#
# String absence is only observable from outside the harness process, so this
# check has to live in CI. But a gate that only ever runs in CI is a gate nobody
# can prove works. Every previous version of this check was written straight into
# build.yml and never executed against a known-bad artifact — which is how it came
# to carry an exemption justified by a diagnosis this repo later disproved, and how
# `grep -aq` on a missing path came to print success. As a script it can be run
# against an archived pre-fix DLL and observed FAILING before it is trusted.
#
# TWO ENCODINGS, OR THE CHECK CANNOT FAIL
#
# .NET metadata names (types, methods, parameters) are UTF-8 in the #Strings heap.
# String literals from method bodies are UTF-16LE in the #US heap. A plain
# `grep -a "SomeLiteral_MARKER"` searches raw bytes and therefore NEVER matches a
# UTF-16 literal — it reports "absent" for something sitting right there, and an
# absence gate built on it passes unconditionally.
#
# `tr -d '\000'` collapses UTF-16LE ASCII text down to plain ASCII, giving a second
# view to search. Metadata names contain no NULs, so they survive that view intact
# and both kinds of marker are reachable. This was not a hypothetical: the probe
# used to produce this gate's expectations had exactly this bug, and reported five
# literal markers absent from an assembly that provably contained them.

set -euo pipefail

DLL="${1:-}"
if [ -z "$DLL" ]; then
    echo "usage: $0 <path-to-RemoteFactory.TrimmingTests.dll>" >&2
    exit 2
fi

if [ ! -f "$DLL" ]; then
    echo "::error::Trimmed assembly not found at '$DLL'. The gate read nothing, so it proved nothing."
    exit 1
fi

NONUL="$(mktemp)"
trap 'rm -f "$NONUL"' EXIT
tr -d '\000' < "$DLL" > "$NONUL"

# Searches both the raw bytes (UTF-8 metadata names) and the NUL-stripped view
# (UTF-16 string literals).
present() {
    grep -aqF -- "$1" "$DLL" || grep -aqF -- "$1" "$NONUL"
}

failures=0

fail() {
    echo "::error::$1"
    failures=$((failures + 1))
}

# ---------------------------------------------------------------------------
# POSITIVE CONTROLS — must be PRESENT.
#
# Without these the gate is unfalsifiable: every absence check below passes
# trivially against a truncated, wrong, or unreadable file. Two of them are
# UTF-8-only and one is UTF-16-only, so a failure in EITHER extraction path is
# caught rather than silently turning that half of the gate into a no-op.
# ---------------------------------------------------------------------------
echo "-- positive controls"
# Named in full, not by prefix. `NeatooEventHandlerRegistrar_` alone is satisfied by the sync
# handler class, so the async handler class could stop generating entirely — taking its markers
# absent for the wrong reason — while the gate stayed green.
for control in \
    "NeatooFactoryRegistrar_TrimTestCommands" \
    "NeatooEventHandlerRegistrar_TrimRelayHandlers" \
    "NeatooEventHandlerRegistrar_TrimAsyncRelayHandlers" \
    "TrimTestCommands" \
    "ITrimIfaceQueryFactory" \
    "ITrimAsyncIfaceQueryFactory" \
    "ITrimSaveTargetFactory"
do
    if present "$control"; then
        echo "   ok      $control"
    else
        fail "Positive control '$control' is MISSING from the trimmed assembly. Either registration was silently removed, or this gate is not reading a real trimmed artifact — do not trust the absence results below."
    fi
done

# UTF-16-only control. Proves the NUL-stripped view was built and searched; if
# this is the sole failure, every literal-marker check below is a no-op.
if present "Trimming verification app completed"; then
    echo "   ok      <utf16 extraction>"
else
    fail "UTF-16 positive control missing. The NUL-stripped view is not working, so every string-literal absence check in this gate is vacuous."
fi

# ---------------------------------------------------------------------------
# ABSENCE CHECKS — per leg, so a failure names the culprit.
#
# NOT every marker here is a fix-discriminator. Mixing the two and calling them all
# "measured present before the fix, absent after" is the claim-beyond-evidence this
# arc keeps repeating, so the two kinds are labelled:
#
#   [D] discriminator  — measured PRESENT pre-fix and ABSENT post-fix. Going red means
#                        the fix regressed.
#   [R] no-regression  — absent both before and after. Going red means something NEW
#                        started leaking. Real value, but it is not evidence that any
#                        fix worked.
#   [N] new baseline   — the target did not exist before the fix, so there is no pre-fix
#                        measurement. First trimmed measurement is the baseline.
#
# Every marker here appears PRESENT in the UNTRIMMED build, which proves the PROBE can see
# it. That is necessary but NOT sufficient for the check to be meaningful: `ServerOnlyHelper`
# was untrimmed-PRESENT for months while nothing referenced it, so ILLink dropped it
# unconditionally and its absence could never have gone red. A marker is only meaningful if
# something a defect could plausibly affect actually roots it. The `*Backend` implementation
# markers below (ClassLegBackend, AsyncLegBackend, IfaceLegBackend, SaveLegBackend,
# RelayLegBackend) are reachable only from DI registrations inside the harness's own
# IsServerRuntime block, so they can only go red if the feature switch stops folding
# altogether — which ServerOnlyDirect already covers. They are kept as cheap breadth, not
# relied on as leg-specific signals.
# ---------------------------------------------------------------------------
check_absent() {
    local marker="$1" leg="$2"
    if present "$marker"; then
        fail "[$leg] '$marker' found in the trimmed assembly. Server-only code is shipping to clients."
    else
        echo "   ok      $marker"
    fi
}

echo "-- static factory ([Execute])"
# [D] _DoWork, _ProcessRecord, IServerOnlyRepository, DoServerWork — all four measured PRESENT
#     pre-fix (plan Current State, 2026-08-12 walk) and absent after. IServerOnlyRepository's
#     flip is the headline evidence that the old (?<!I) exemption was unjustified, so labelling
#     it [R] under-claimed the very marker this plan's Step 8 turns on.
# [R] ServerOnlyDirect, ServerOnlyHelper, ServerOnlyHelper_MARKER — absent before and after.
#     ServerOnlyRepository_MARKER is the IMPLEMENTATION body literal: the gate
#     this replaced asserted it via `grep -aqP '(?<!I)ServerOnlyRepository'`, and the first
#     rewrite dropped it, because grep -F "IServerOnlyRepository" cannot match the bare name.
#     Restored as an explicit marker — it is unambiguous, and being UTF-16 it also exercises
#     the NUL-stripped path.
for m in _DoWork _ProcessRecord DoServerWork IServerOnlyRepository ServerOnlyRepository_MARKER \
         ServerOnlyDirect ServerOnlyHelper ServerOnlyHelper_MARKER; do
    check_absent "$m" "static factory"
done

echo "-- static factory, async body"
# [N] async [Execute]. _DoAsyncWork is a method on the CONSUMER'S class — exactly the surface
#     the registrar-DAM defect retained — so its absence is real evidence the fix generalizes
#     to async [Execute] methods.
#
#     It is NOT evidence about async fold behaviour. The static leg's guard is a WRAPPING
#     `if (IsServerRuntime) { ... }` inside the non-async FactoryServiceRegistrar, so the fold
#     deletes the whole registration and the body is never rooted in the first place. Its
#     async-ness is therefore never exercised. Same for the relay handler below.
for m in _DoAsyncWork StaticAsyncBody_MARKER; do
    check_absent "$m" "static factory (async)"
done

echo "-- relay handler ([FactoryEventHandler<T>])"
# [D] RelayLegHandlerBody, IRelayLegPort, RelayLegInvoke, RelayLegHandlerBody_MARKER.
# [R] RelayLegBackend — absent pre-fix too (it is DI-registered only behind the harness's
#     own IsServerRuntime guard, so nothing ever rooted it). Kept, but NOT fix evidence.
for m in RelayLegHandlerBody IRelayLegPort RelayLegInvoke RelayLegHandlerBody_MARKER; do
    check_absent "$m" "relay handler"
done
check_absent "RelayLegBackend" "relay handler"
for m in AsyncRelayHandlerBody RelayAsyncBody_MARKER; do
    check_absent "$m" "relay handler (async)"
done

echo "-- interface factory"
# [R] all of them — absent pre-fix as well, since this leg never had the defect.
# NOTE what these do and do not prove. An interface factory reaches its implementation
# through the INTERFACE (GetRequiredService<ITrimIfaceQuery>() then target.LookupAsync),
# so the implementation body is never statically reachable from generated code and its
# absence follows from that indirection — NOT from feature-switch folding. Useful as a
# no-regression check on the implementation staying off the client; not evidence about
# guard elimination.
for m in TrimIfaceServerSide IIfaceLegPort IfaceLegInvoke IfaceLegBackend IfaceLegServerBody_MARKER; do
    check_absent "$m" "interface factory"
done
# The async variant carries the SAME caveat, and it is structural rather than fixable here:
# an interface factory reaches everything through interfaces, so no server-only implementation
# name can appear directly in its generated local body. These markers cannot report on whether
# the async body folded — they are no-regression checks only. See the remarks on
# ITrimAsyncIfaceQuery, and Deferred Work item 19 for why the obvious fix does not compile.
for m in TrimAsyncIfaceServerSide IfaceAsyncBody_MARKER; do
    check_absent "$m" "interface factory (async)"
done

echo "-- class factory, read path"
# [N] own port, so a class-factory leak no longer reports as a static-factory failure.
#
# Only the two IMPLEMENTATION markers are absent. IClassLegPort and ClassLegInvoke are NOT
# here: they are retained by the async FetchAsync body (TRIM-009) via its in-body
# GetRequiredService<IClassLegPort>() and the port call, exactly as ISaveLegPort/SaveLegInvoke
# are on the save leg. They are asserted PRESENT with the controlled pair below.
# ClassLegBackend and its literal stay absent because they sit behind the IClassLegPort
# interface hop, so nothing statically reaches them either way.
for m in ClassLegBackend ClassLegBackend_MARKER; do
    check_absent "$m" "class factory"
done

echo "-- async-only port (shared by the three async targets above)"
for m in IAsyncLegPort AsyncLegInvoke AsyncLegBackend; do
    check_absent "$m" "async port"
done

# ---------------------------------------------------------------------------
# THE CONTROLLED PAIR — sync vs async inside ONE class factory.
#
# ClassSyncBody_MARKER  lives in TrimTestEntity.Create      (sync)  -> expected ABSENT
# ClassAsyncBody_MARKER lives in TrimTestEntity.FetchAsync  (async) -> expected PRESENT
#
# Same class, same generated factory, same registrar, neither carrying [AuthorizeFactory<T>],
# both one-hop rooted by their own delegate registration, both reached by a direct call on the
# concrete type, both literals in the domain body rather than behind an interface hop.
# The earlier sync/async comparison (this class's Create vs TrimSaveTarget's Insert) was NOT
# controlled — it also differed in auth, target acquisition, one-hop vs two-hop rooting, and
# catch-arm count, the last being the dimension the arc's disproven TRIM-004 story blamed.
#
# The remaining co-variate is that co-variate by construction: the generator emits an extra
# `catch (OperationCanceledException)` arm for async methods, so "async" and "extra catch arm"
# cannot be separated from outside the generator.
#
# ClassAsyncBody_MARKER is asserted PRESENT for the same reason as the save/can* block: it is
# TRIM-009's defect, and the gate must fail loudly when it is fixed rather than silently
# passing.
# ---------------------------------------------------------------------------
echo "-- controlled sync/async pair (class factory)"
check_absent "ClassSyncBody_MARKER" "class factory (sync half of controlled pair)"
for m in ClassAsyncBody_MARKER IClassLegPort ClassLegInvoke; do
    if present "$m"; then
        echo "   ok      $m (still present, as TRIM-009 expects)"
    else
        fail "[class factory] '$m' is now ABSENT. If TRIM-009 has landed, promote it into the absence checks above and delete it from here. If not, the async diagnosis is wrong and must be reopened."
    fi
done

# ---------------------------------------------------------------------------
# KNOWN-BROKEN — asserted PRESENT on purpose (TRIM-009).
#
# Async generated Local* methods retain their server-only bodies; sync ones in the
# same assembly do not. That is a different defect from the registrar-DAM one this
# gate was written for, with a different fix, so TRIM-008 does not remove these.
#
# Asserting them PRESENT rather than omitting them is deliberate. Omitted, the gate
# would quietly keep passing after TRIM-009 lands and nobody would tighten it.
# Asserted, CI fails the moment the leak is fixed and the failure message says what
# to do. This is a pending marker, not an endorsement.
# ---------------------------------------------------------------------------
echo "-- save/can* (known broken, TRIM-009)"
for m in ISaveLegPort SaveLegInvoke SaveLegInsertBody_MARKER SaveLegUpdateBody_MARKER SaveLegDeleteBody_MARKER; do
    if present "$m"; then
        echo "   ok      $m (still present, as TRIM-009 expects)"
    else
        fail "[save/can*] '$m' is now ABSENT. If TRIM-009 has landed, this is good news: move '$m' up into the absence checks and delete it from this block. If TRIM-009 has NOT landed, its diagnosis is wrong and must be reopened."
    fi
done

echo
if [ "$failures" -gt 0 ]; then
    echo "::error::Trimming verification FAILED ($failures check(s))."
    exit 1
fi

echo "Trimming verification passed."
echo "  Absent:  static factory ([Execute], sync and async), relay handler (sync and async),"
echo "           interface factory implementations, and the SYNCHRONOUS class-factory body."
echo "  Present, expected, tracked as TRIM-009: every ASYNC class-factory body — the save/can*"
echo "           write path and the FetchAsync half of the controlled pair. Asserted PRESENT"
echo "           above, so this gate fails loudly the moment TRIM-009 lands."
