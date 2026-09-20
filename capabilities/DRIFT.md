# DRIFT.md — the drift ledger of record

**What this file is.** The standing record of what the living-spec drift detector watches, what it has
found, and — most importantly — **what it cannot see**. It is a plain markdown file deliberately: the
resolver's `find_spec_files` matches only `*.spec.md` and `capabilities/<name>/spec.md`, so a non-spec
markdown file dropped in `capabilities/` is inert. It is neither a capability nor an orphan, and it never
affects a drift run. That is the point: this ledger can be honest about the detector's blind spots without
being mistaken for one of the specs it describes.

Keep it current rather than re-deriving these facts.

## How to run it

- **Use `python` or `py`, never `python3`** — on this machine `python3` is a 0-byte Microsoft Store alias
  that answers "Python was not found", and a literal reading of the agent files that say `python3 --version`
  silently drops the lifecycle journal, task folds, coverage map and drift report together.
- `python .specify/extensions/companion/scripts/drift.py [--working] [--json]`. It **always exits 0** and
  **never edits**.
- `--working` adds uncommitted edits, deletions and untracked files to each capability's changed set.
  `/speckit.companion.living-sync` consumes that same mode's `--json` as its plan, so the two are the
  read/write halves of one computation.
- **Its self-trace does not go to a spec folder.** `drift.py` records through `run_trace` with
  `UNATTRIBUTED_DIR = "specs"`, so drift lines land in **`specs/.trace.jsonl`** (two per run: a
  `drift-compute` line plus a `verdict` line), not in `specs/<feature>/.trace.jsonl`. A spec-scoped trace
  only ever shows `write-context`.

## The blind spot — read this before trusting a green run

> **`✓ All N checked capabilities in sync` is not evidence of coverage.**

The registry (`living-specs.yml` at the repo root) declares exactly **two** capabilities:

| Capability | Matches |
| --- | --- |
| `startup` | `MTM_Waitlist.Startup/**`, `Module_Startup/**` (excluding `MTM_Waitlist.Startup/Services/StartupLog*.cs`) |
| `startup-diagnostics` | `MTM_Waitlist.Startup/Services/StartupLog*.cs` |

Everything else has **no living spec**, so drift across it is invisible *by construction*:

`MTM_Waitlist.Waitlist.View/**`, `Module_Waitlist/**`, `MTM_Waitlist.Core/**`, `MTM_Waitlist.Setup/**`,
`Module_Setup/**`, `Database/**`, `Strings/**`, `Views/**`, `ViewModels/**`.

A green drift run therefore means **the registry is complete with respect to itself** — not that the
repository is in sync. Confirm the distinction with
`python .specify/extensions/companion/scripts/resolve-spec-paths.py --all --json`: `orphans: []` means no
spec is unreachable, not that every area is watched.

**Adopting a new area** (so the detector can see it) is `--speckit.companion.living-adopt`, opt-in and
surface-first, drafting `[DRAFT]` specs. Until that is run for an area, treat any "no drift" result for it
as silence, not health.

## Registry hazard: a file-level glob leaf under a whole-area parent

The resolver scores a capability by the **directory depth of its glob's literal prefix**, so a
filename glob inside a whole-area glob can never outscore its parent — the leaf is decorative and every
change folds into the parent. Fix: give the parent an `exclude` for that same glob. `matches()` checks
`exclude` first, so the child then owns those files uncontested (its score is still 1, but it is the only
match). This is exactly why `startup` carries its `exclude`. A leaf over a real **subdirectory** does not
need this; the bug is specific to file-level globs.

## Coverage links are worth machine-validating

A coverage `tests` entry must be a **test METHOD name**. A class name (e.g. `RequestImagePathPolicyTests`)
reads as satisfied in the panel while linking nothing. Validate by building the set of real method names:

```powershell
Get-ChildItem MTM_Waitlist.Tests -Recurse -Filter *.cs |
  Select-String -Pattern 'public (?:async Task|void) ([A-Za-z_0-9]+)\(' |
  ForEach-Object { $_.Matches[0].Groups[1].Value }
```

then checking every entry under `coverage.*.tests`. This caught a real bad link on 2026-09-13.
`capture.py --coverage-req` merges non-destructively (only the supplied slot is replaced), so `title` and
`tasks` survive a tests-only update.

## Documentation drift — this ledger's second job

Nothing else watches the documentation tree, so it is recorded here.

**Found 2026-09-13** (in the working tree at that time):

| Deleted artefact | Refs left dangling | Nature of the references |
| --- | ---: | --- |
| 19 `WeekendProject/Mockups/*.svg` | 31 | **Instruction** references from `WeekendProject/PromptFiles/*` — the checklists told a reader to open art that was gone. |
| `RELEASE-NOTES.md` | 5 | A `FEATURES.md` catalogue entry plus 4 provenance hits in `specs/001-module-mock-visual-fallback/tasks.md` — one inside an **already-ticked** T125 that told the reader to write into that very file. |
| `VALIDATION-PROMPT-SERVER.md` | 8 | **All provenance** for the T150/T151 host verification; the claims are now uncheckable from the file. |
| Five `PromptFiles/14-*` snapshots | — | Deleting them **agrees** with `OPEN-TASKS.md` §3.1's "retired counting trap" policy. Only the references dangled. |

Also recorded then: `FEATURES.md` was pinned to commit `7bf6857` and its maintainer index listed
`specs/001` alone although its own defect table cited `specs/002` ten times; `OPEN-TASKS.md` never mentioned
`specs/003`.

**Resolved 2026-09-20** by `specs/004-unified-card-item-picker` Phase 14 (US7, "the codebase is honest about
itself"): the `Mockups/` and `DeveloperUI-VisualPrompts.md` references are now classified in place as
**provenance, not instruction**; the `VALIDATION-PROMPT-SERVER.md` citations are classified the same way in
`OPEN-TASKS.md` §5 and `specs/001/tasks.md`; `FEATURES.md` is re-pinned and its index extended to
`specs/001`–`004`; `OPEN-TASKS.md` names `specs/002`, `specs/003` and `specs/004` and its carry-forward total
is corrected.

**The rule this leaves behind.** Classify a dangling reference as **instruction**, **provenance**, or
**already-retired** *before* proposing a fix — the three want different answers. An instruction must be
repaired or deleted; provenance needs a note saying the artefact is gone and why, so the next reader stops
looking; an already-retired artefact needs nothing but the reference removed.

## Related notes

- `.github/memories/repo/github-artifact-staleness.md` — the `.github/`-scoped staleness audit. Its
  Decisions #2 recorded `.github/copilot-startup-steps.yaml` and
  `.github/scripts/restructure-database-layout.ps1` as **DELETED**; **both are present in the tree** (restored
  with corrected "REFERENCE ONLY" / "ARCHIVED — DO NOT RE-RUN" headers). See that file's correction note.
- `.github/memories/repo/document-inventory.md` — the wider inventory this ledger is named from.
