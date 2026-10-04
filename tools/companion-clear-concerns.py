#!/usr/bin/env python3
"""Retire concerns from the Companion's list without losing them.

The Companion writer only ever *appends* a concern. There is no flag to resolve or
remove one, and `--set` cannot carry a list (it coerces to a scalar), so a concern
that has been dealt with sits in the panel for ever and the list stops being a list
of open business.

This moves a concern out of `.spec-context.json` and into a plain log beside the
spec (`concerns-log.md`), recording the concern in its own words plus the reason it
was cleared. The list shrinks; the note survives. It reads and writes through the
Companion's own store module, so `.spec-context.json` is written exactly the way
the sanctioned writer writes it.

Usage:
    python tools/companion-clear-concerns.py --list
    python tools/companion-clear-concerns.py --entry "3|the guard now exists; T189 closes the gap"
    python tools/companion-clear-concerns.py --entry "7|... " --entry "13|..."

`--entry` takes `<number>|<reason>`. Numbers are read against the list as it stands
when the command starts, so several entries can be cleared in one call. The reason
is required: a cleared concern with no reason is indistinguishable from one that was
quietly dropped. Idempotent, and it never touches a concern it was not asked about.
"""

from __future__ import annotations

import json
import re
import sys
from datetime import date
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
COMPANION_SCRIPTS = REPO_ROOT / ".specify" / "extensions" / "companion" / "scripts"

if not COMPANION_SCRIPTS.is_dir():
    sys.exit(f"Companion scripts not found at {COMPANION_SCRIPTS}")

sys.path.insert(0, str(COMPANION_SCRIPTS))
from spec_context import atomic_write, read_ctx  # noqa: E402

LOG_HEADER = """# Concerns cleared

Concerns moved off the Companion's list, each recorded in its own words with the
reason it was cleared. The list in `.spec-context.json` holds open business only;
this file is the trail for the ones that came off it.
"""


def feature_dir_from_argv(argv: list[str]) -> Path:
    for index, arg in enumerate(argv):
        if arg == "--feature-dir" and index + 1 < len(argv):
            candidate = Path(argv[index + 1])
            return candidate if candidate.is_absolute() else (REPO_ROOT / candidate)
    pointer = REPO_ROOT / ".specify" / "feature.json"
    if pointer.is_file():
        found = json.loads(pointer.read_text(encoding="utf-8")).get("feature_directory")
        if found:
            candidate = Path(found)
            return candidate if candidate.is_absolute() else (REPO_ROOT / candidate)
    sys.exit("No feature directory given and .specify/feature.json has none.")


def collect_entries(argv: list[str]) -> list[tuple[int, str]]:
    pairs: list[tuple[int, str]] = []
    for index, arg in enumerate(argv):
        if arg != "--entry" or index + 1 >= len(argv):
            continue
        raw = argv[index + 1]
        if "|" not in raw:
            sys.exit(f"--entry needs '<number>|<reason>', got {raw!r}")
        number, reason = raw.split("|", 1)
        if not number.strip().isdigit():
            sys.exit(f"--entry number must be a whole number, got {number!r}")
        if not reason.strip():
            sys.exit(f"--entry {number.strip()} has no reason; a cleared concern needs one")
        pairs.append((int(number.strip()), reason.strip()))
    return pairs


def append_to_log(log: Path, cleared: list[tuple[int, str, str]]) -> None:
    text = log.read_text(encoding="utf-8") if log.is_file() else LOG_HEADER
    if not text.endswith("\n"):
        text += "\n"
    today = date.today().isoformat()
    for _number, note, reason in cleared:
        marker = " ".join(note.split())[:120]
        if marker and marker in text:
            continue
        text += f"\n## {today} — {reason}\n\n"
        text += f"> {note}\n"
    log.write_text(text, encoding="utf-8")


def main(argv: list[str]) -> int:
    feature_dir = feature_dir_from_argv(argv)
    target = feature_dir / ".spec-context.json"
    if not target.is_file():
        sys.exit(f"Missing {target}")

    ctx = read_ctx(target)
    concerns = ctx.get("concerns")
    if not isinstance(concerns, list):
        concerns = []

    if "--list" in argv or not collect_entries(argv):
        print(f"[clear-concerns] {len(concerns)} concern(s) open")
        for number, item in enumerate(concerns, start=1):
            note = item.get("note") if isinstance(item, dict) else str(item)
            flat = re.sub(r"\s+", " ", str(note)).strip()
            print(f"  [{number}] {flat[:150]}")
        return 0

    pairs = collect_entries(argv)
    ordered = sorted(pairs, key=lambda pair: pair[0])
    out_of_range = [number for number, _ in ordered if not 1 <= number <= len(concerns)]
    if out_of_range:
        sys.exit(f"Concern number(s) out of range: {out_of_range} (list holds {len(concerns)})")
    duplicate = [number for number, _ in ordered if [n for n, _ in ordered].count(number) > 1]
    if duplicate:
        sys.exit(f"Concern number(s) given twice: {sorted(set(duplicate))}")

    cleared: list[tuple[int, str, str]] = []
    for number, reason in ordered:
        item = concerns[number - 1]
        note = item.get("note") if isinstance(item, dict) else str(item)
        cleared.append((number, str(note), reason))

    append_to_log(feature_dir / "concerns-log.md", cleared)

    drop = {number for number, _ in ordered}
    remaining = [item for number, item in enumerate(concerns, start=1) if number not in drop]
    ctx["concerns"] = remaining
    atomic_write(target, ctx)

    for number, note, reason in cleared:
        flat = re.sub(r"\s+", " ", note).strip()[:110]
        print(f"[clear-concerns] cleared {number}: {reason}")
        print("                 was: " + flat)
    print(f"[clear-concerns] {len(cleared)} cleared, {len(remaining)} still open")
    print(f"[clear-concerns] trail written to {(feature_dir / 'concerns-log.md').relative_to(REPO_ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
