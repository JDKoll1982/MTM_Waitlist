# ABANDONED — `specs/005-setup-work-order-search`

**This is not a feature. There is no spec here, and there never was.**

A `/speckit.specify` run for a *Setup work-order search* feature was started on **2026-09-13 at 23:19:48Z**
and ended without producing `spec.md`. The only surviving artefact is the run journal
`.trace.jsonl` (58 lines), which is the evidence for everything below.

## What the journal shows

| Time (UTC) | What happened |
| --- | --- |
| 23:19:48 | `lifecycle` — the run created `.spec-context.json` (349 bytes). |
| 23:19:48 → 23:39:58 | 45 `capture` calls, 5 `set`, 4 `lifecycle`, 2 `finish` — so the pipeline ran for about 20 minutes and got as far as writing context. |
| 23:20:52 | `unknown`, `ok: false` — **"the writer exited without recording an outcome."** |
| 23:39:58 | `unknown`, `ok: false` — **the same failure again.** This is where it ended. |

`spec.md` is never named in the journal, and it is not on disk. `.spec-context.json` is also gone — the
directory was cleaned afterwards, leaving only the journal.

## Why it is kept rather than deleted

- **The journal is the audit trail.** Deleting this directory would delete the only record that the run
  happened, and the Companion's `doctor` reads journals to explain exactly this class of failure. The
  evidence is worth more than the tidiness.
- **The failure is already diagnosed, and it is not this feature's fault.** The `unknown` /
  *"the writer exited without recording an outcome"* pair is the documented silent-loss mode of
  `write-context.py`: every capture script returns 0 on failure, so the trigger appears only in the
  specific line above it. The known triggers are `[WinError 5] Access is denied` renames while
  `.spec-context.json` is open in an editor, and the stale `python3` interpreter prerequisite in the
  agent files. See `capabilities/DRIFT.md` and the companion notes it cites.

## Consequences a reader must know

1. **Feature number `005` is consumed.** `create-new-feature.ps1` takes the highest number from the
   **directories** under `specs/`, so the next real `/speckit.specify` run will be **`006`**. That is
   correct — the number was genuinely used — but it will look like a gap to anyone who expects `005` to
   be the next spec. It is not a gap.
2. **This directory is invisible to tooling, by design.**
   `resolve-spec-paths.py --all --json` reports `orphans: []` and does not list this folder: it matches
   only `*.spec.md` and `capabilities/<name>/spec.md`. So this marker file and the journal neither create
   a false capability nor break a drift run.
3. **Nothing here is a task.** No box in any checklist, `tasks.md` or `OPEN-TASKS.md` is owned by this
   directory. If the *Setup work-order search* feature is wanted, start it fresh with
   `/speckit.specify` and let it take the next number.
