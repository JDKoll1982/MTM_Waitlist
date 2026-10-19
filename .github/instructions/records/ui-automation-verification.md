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

---

## Driven runs after the 2026-10-19 repairs (home workstation, `johnk`)

The recipes were re-exercised on the home workstation after six driver faults were fixed (each is recorded in
`.github/memories/repo/ui-automation-drive.md` and §11 of `docs/scripts/scripts-automation-ui-automation.md`):

| Claim | Result (2026-10-19) |
|---|---|
| `-Action launch` finds the exe with no `-Exe` argument | **correct after the fix** — it resolved `bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe`; before the fix it expanded to `C:\..\..\bin\...` and failed `Test-Path` |
| The app reaches its store with no `MTM_WAITLIST_DB_CONNECTION_STRING` | **correct** — the driver reports "store: from the app's own appsettings.json" and the run reaches the sign-in stage |
| Reaching the shell needs a credential | **still correct, and the 2026-09-20 row saying otherwise is superseded** — the shell was reached only by signing in as `JOHNK` with the PIN supplied per run. Nothing auto-signs-in any more |
| A toggle button can be driven | **correct after the fix** — `ShellPage_MyRequestsToggleDefault` answers `TogglePattern` and neither `Invoke` nor `SelectionItem` |
| `click-item` on a wizard category | **correct after the fix** — polls up to `-FindSeconds`; the single-look version reported `no ListItem matched [Pickup]` on a page that was still drawing |
| A typed value reaches the request | **correct after the fix** — `# of Skids` typed `4` against a `NumberBox` stored `{"# of Skids": "4"}` and drew the cell as `Skids = 4`; the `ValuePattern`-only version silently stored `1` |
| The full surface walk | **correct** — signed in, audited **25 of 29 surfaces**, **0 findings**, app closed itself |
| A password field's read-back | **masked (`●●●●`)** — accepted by length; treating the mask as a failed entry produced a false warning and a redundant `SetValue` |

Also measured on the same day while verifying feature 012 with these scripts:

- **Condition round-trip (SC-016):** a chosen condition reached `handler_status` in `mtm_waitlist.waitlist_requests_queue`,
  the card's status column moved from the overdue red `#B02418` to the handler-condition red `#C0392B` on both the word
  and the ring, a second choice replaced the first, and all four conditions were restored after a restart on the four
  rows they were set against.
- **Keyboard only:** focusing an operable status word and pressing Enter opened `Choose a condition`; Tab walked the
  four conditions in their settled order; Escape closed it without choosing.
