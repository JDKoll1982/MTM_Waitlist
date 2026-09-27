#!/usr/bin/env python3
"""Show the open tasks on the Companion's Tasks card.

The card is built from `task_summaries` in `.spec-context.json`, and the Companion
writer only ever puts *finished* tasks there: `--task` and `--close-task` are
finish-only by design. So the card lists what has been done and nothing else, and
its "done / total" reads "69/69", because the total it shows is the number of
entries in that same map rather than the number of tasks in the file.

The viewer already knows how to draw a task that is not finished. Its status map
has a third bucket: anything outside {DONE, DONE_WITH_CONCERNS, COMPLETED,
COMPLETE} is rendered as `status.toLowerCase()` in the not-done style, and counted
as not done. So the gap is data, not UI: the open tasks are simply never recorded.

This adds or refreshes one entry per **open** task, taken from `tasks.md` itself,
and never touches an entry that records finished work. It reads and writes through
the Companion's own store module, so the file is written exactly as the sanctioned
writer writes it — same reader, same atomic temp-file-and-replace. Idempotent: run
it again whenever tasks.md changes.

Usage:
    python tools/companion-open-tasks.py [feature-dir]
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
COMPANION_SCRIPTS = REPO_ROOT / ".specify" / "extensions" / "companion" / "scripts"

if not COMPANION_SCRIPTS.is_dir():
    sys.exit(f"Companion scripts not found at {COMPANION_SCRIPTS}")

sys.path.insert(0, str(COMPANION_SCRIPTS))
from spec_context import atomic_write, read_ctx  # noqa: E402

# The viewer counts a task as done for these and no others.
DONE_STATUSES = {"DONE", "DONE_WITH_CONCERNS", "COMPLETED", "COMPLETE"}

# The status an open task carries. Deliberately outside DONE_STATUSES, so the
# viewer counts it as not done and prints it as "open".
OPEN_STATUS = "OPEN"

TASK_RE = re.compile(r"^(?P<indent>\s*)-\s*\[(?P<mark>[ xX])\]\s*\*\*(?P<id>T\d{3})\*\*\s*(?P<rest>.*)$")
STOP_RE = re.compile(r"^\s*(-\s*\[|\*\*|#|\||>)")
FILE_TAIL_RE = re.compile(r"\s+·\s+`.*$")
TAG_RE = re.compile(r"^\[[^\]]+\]\s*")


def feature_dir_from_argv() -> Path:
    if len(sys.argv) > 1:
        candidate = Path(sys.argv[1])
        return candidate if candidate.is_absolute() else (REPO_ROOT / candidate)
    pointer = REPO_ROOT / ".specify" / "feature.json"
    if pointer.is_file():
        data = json.loads(pointer.read_text(encoding="utf-8"))
        found = data.get("feature_directory")
        if found:
            candidate = Path(found)
            return candidate if candidate.is_absolute() else (REPO_ROOT / candidate)
    sys.exit("No feature directory given and .specify/feature.json has none.")


def parse_tasks(tasks_file: Path) -> list[tuple[str, str, str]]:
    """Return (id, mark, title) for every task in tasks.md.

    A task's text may wrap onto following lines. Continuation stops at a blank
    line, another task, a heading, or a line that opens a new bold or list block.
    """
    tasks: list[tuple[str, str, str]] = []
    lines = tasks_file.read_text(encoding="utf-8").splitlines()
    index = 0
    while index < len(lines):
        match = TASK_RE.match(lines[index])
        if not match:
            index += 1
            continue
        parts = [match.group("rest")]
        look = index + 1
        while look < len(lines):
            line = lines[look]
            if not line.strip() or STOP_RE.match(line):
                break
            parts.append(line.strip())
            look += 1
        title = " ".join(part for part in parts if part)
        title = FILE_TAIL_RE.sub("", title)
        title = TAG_RE.sub("", title.strip())
        title = re.sub(r"\s+", " ", title).strip()
        tasks.append((match.group("id"), match.group("mark").lower(), title))
        index = look
    return tasks


def main() -> int:
    feature_dir = feature_dir_from_argv()
    target = feature_dir / ".spec-context.json"
    tasks_file = feature_dir / "tasks.md"
    for required in (target, tasks_file):
        if not required.is_file():
            sys.exit(f"Missing {required}")

    tasks = parse_tasks(tasks_file)
    ctx = read_ctx(target)
    summaries = ctx.get("task_summaries")
    if not isinstance(summaries, dict):
        summaries = {}

    added, refreshed, kept, unfinished_without_summary = 0, 0, 0, []
    for task_id, mark, title in tasks:
        existing = summaries.get(task_id)
        status = str(existing.get("status", "")).upper() if isinstance(existing, dict) else ""
        if mark == "x":
            if status in DONE_STATUSES:
                kept += 1
            else:
                # Ticked in the file but never journaled. Do not invent a record
                # of finished work: that is what --close-task exists to write.
                unfinished_without_summary.append(task_id)
            continue
        if status in DONE_STATUSES:
            # A finished record outranks a cleared box; never demote real work.
            kept += 1
            continue
        entry = dict(existing) if isinstance(existing, dict) else {}
        entry["status"] = OPEN_STATUS
        entry["did"] = title
        entry.setdefault("files", [])
        entry.setdefault("concerns", [])
        if isinstance(existing, dict):
            refreshed += 1
        else:
            added += 1
        summaries[task_id] = entry

    ctx["task_summaries"] = summaries
    atomic_write(target, ctx)

    values = list(summaries.values())
    total = len(values)
    done = sum(1 for v in values if str(v.get("status", "")).upper() in DONE_STATUSES)
    print(f"[open-tasks] tasks.md holds {len(tasks)} task(s)")
    print(f"[open-tasks] added {added} open entr(ies), refreshed {refreshed}, left {kept} finished alone")
    print(f"[open-tasks] the Tasks card will now show {done} done of {total}")
    if unfinished_without_summary:
        shown = ", ".join(unfinished_without_summary[:12])
        more = "" if len(unfinished_without_summary) <= 12 else f" (+{len(unfinished_without_summary) - 12} more)"
        print(f"[open-tasks] ticked but not journaled, left untouched: {shown}{more}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
