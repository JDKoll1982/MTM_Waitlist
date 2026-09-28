# UI automation verification log

**Moved here** on 2026-09-27 from `.github/instructions/winui3-ui-automation.instructions.md` by
`/speckit.token-budget` (optimize.tokens), because it was the largest block of dated evidence in a
file that loads on every request. The text is unchanged; the instruction file keeps a pointer.

Read this when you need to know **what was actually measured and when**, or when a recipe in the
instruction file is questioned. The recipes themselves live in that file and are unaffected.

---

## Verification status (2026-09-11 and 2026-09-20, current Debug build)

The recipes above were exercised against the built app
(`bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe`) on `MTMFG-161` — the table
below on **2026-09-11**, and the pre-shell windows only; the second table, further down, on
**2026-09-20**, when the app was driven all the way into the shell:

| Claim | Result |
|---|---|
| Executable path | **correct** — file present and current |
| `Start-Process` returns immediately and console output is not captured | **correct** |
| `MainWindowHandle` resolves once a window exists | **correct** |
| Window lookup by process id | **correct** — 1 top-level window found |
| Never locate by window title | **confirmed, and worse than documented** — splash reported `WinUI Desktop`, sign-in reported `Sign in` |
| Text dump identifies the page | **correct** — returned the failure dialog, then the sign-in text |
| Buttons appear in the text dump | **incorrect as written** — they are `ControlType.Button`; corrected above |
| `GetWindowPlacement` / `GetWindowRect` recipe | **correct** — returned `showCmd=1`, `760x460` (splash), `820x760` (sign-in) |
| `showCmd` 1/2/3 = normal/minimized/maximized | **consistent** (`1` on both windows) |
| Startup log staleness check | **superseded** — the file-based log path this row was recorded against has since been retired with the startup rebuild, so `startup_daily_*.jsonl` and `startup_forwarded_*.jsonl` are leftovers and their timestamps say nothing about the current build. The row is kept as the record of what was measured on those two dates; read `ops_startup_logs` instead |
| Close recipe leaves no orphan | **correct** — 0 instances afterwards |
| `Add-Type` fails on a duplicate type | **did not reproduce** — see the note in step 4; the real hazard here is the opposite (types do not survive between commands) |

**Not verified — as of 2026-09-11.** Step 5 (navigation with `SelectionItemPattern`) and the shell header text of step 3.
Both need the app past the **Sign in** gate, and no valid account credential was used for that test
run, so the shell was never reached.

**Both are now verified (2026-09-20), and the blocker was not a credential at all.** With both
connection overrides set against the local `mtm_waitlist`, the app auto-signed-in and reached the
shell, so the two open items were walked on the current Debug build:

| Claim | Result (2026-09-20) |
|---|---|
| Step 3 — the text dump identifies the shell page | **correct** — `Waitlist for "Expo Drive"`, the `johnk` badge, the nav labels and 21 request cards, with no `Sign in` window anywhere in the run |
| Step 5 — `NavigationViewItem` → `ControlType.ListItem` + `SelectionItemPattern` | **correct** — the nav item reported `ControlType.ListItem`, `Select()` succeeded, and the header changed to `Work Center Setup — Select Work Station` with the wizard's steps 1–7 rendered |
| `showCmd` on a maximized window | **consistent** — `3`, with `GetWindowRect` `3456x1408` and a `760x500` restore rectangle |
| Reaching the shell needs credentials | **incorrect as written** — it needs no sign-in; corrected in step 1 |
| The recipe needs no app change to work | **correct** — the run used the shipped artifact unchanged |
