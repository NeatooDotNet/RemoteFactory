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
# Every marker below was measured PRESENT before its leg was fixed and ABSENT
# after, on a publish-trimmed artifact. See TRIM-008.
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
for m in _DoWork _ProcessRecord DoServerWork IServerOnlyRepository ServerOnlyDirect ServerOnlyHelper; do
    check_absent "$m" "static factory"
done

echo "-- relay handler ([FactoryEventHandler<T>])"
for m in RelayLegHandlerBody IRelayLegPort RelayLegInvoke RelayLegBackend RelayLegHandlerBody_MARKER; do
    check_absent "$m" "relay handler"
done

echo "-- interface factory"
for m in TrimIfaceServerSide IIfaceLegPort IfaceLegInvoke IfaceLegBackend IfaceLegServerBody_MARKER; do
    check_absent "$m" "interface factory"
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

echo "Trimming verification passed: server-only code absent for static, relay, and interface legs."
