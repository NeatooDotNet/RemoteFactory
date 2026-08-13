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
for control in \
    "NeatooFactoryRegistrar_" \
    "NeatooEventHandlerRegistrar_" \
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
#
# Every marker in both categories has been shown PRESENT in the UNTRIMMED build, so
# none of them is an assertion that could never fail.
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
# [D] _DoWork, _ProcessRecord — retained pre-fix by the registrar-DAM defect.
# [R] the rest. ServerOnlyRepository_MARKER is the IMPLEMENTATION body literal: the gate
#     this replaced asserted it via `grep -aqP '(?<!I)ServerOnlyRepository'`, and the first
#     rewrite dropped it, because grep -F "IServerOnlyRepository" cannot match the bare name.
#     Restored as an explicit marker — it is unambiguous, and being UTF-16 it also exercises
#     the NUL-stripped path.
for m in _DoWork _ProcessRecord DoServerWork IServerOnlyRepository ServerOnlyRepository_MARKER \
         ServerOnlyDirect ServerOnlyHelper ServerOnlyHelper_MARKER; do
    check_absent "$m" "static factory"
done

echo "-- static factory, async body"
# [R] async [Execute]. The static leg's clean result was measured only on synchronous
#     bodies until TRIM-008's test review; this makes it a measurement.
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
for m in TrimAsyncIfaceServerSide IfaceAsyncBody_MARKER; do
    check_absent "$m" "interface factory (async)"
done

echo "-- class factory, read path"
# [R] own port, so a class-factory leak no longer reports as a static-factory failure.
# Matters now: TRIM-009 changes this leg.
for m in IClassLegPort ClassLegInvoke ClassLegBackend ClassLegBackend_MARKER; do
    check_absent "$m" "class factory"
done

echo "-- async-only port (shared by the three async targets above)"
for m in IAsyncLegPort AsyncLegInvoke AsyncLegBackend; do
    check_absent "$m" "async port"
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

echo "Trimming verification passed: server-only code absent for the static, relay, interface, and"
echo "class-factory-read legs, including their async bodies. The class-factory WRITE path"
echo "(save/can*) still leaks by design of the current code — tracked as TRIM-009 and asserted"
echo "PRESENT above so this gate fails loudly the moment that changes."
