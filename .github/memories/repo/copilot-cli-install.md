# GitHub Copilot CLI — how it is installed on this workstation

Recorded 2026-10-16 after the SpecKit Companion warned "GitHub Copilot CLI is not installed".

## The symptom

- VS Code notification (source: **SpecKit Companion**, `alfredoperez.speckit-companion`):
  *"GitHub Copilot CLI is not installed. Install it with: `gh extension install github/gh-copilot`"*.
- Running that suggested command fails:

  ```text
  gh extension install github/gh-copilot
  "copilot" matches the name of a built-in command or alias
  ```

## Why the suggested command can never work

`gh` 2.102.0 (installed here 2026-09-30) **ships `copilot` as a built-in command** — `gh copilot`
runs the Copilot CLI, downloading it to `%LOCALAPPDATA%\GitHub CLI\copilot` if it is not already on
`PATH` (`gh copilot --help`; docs: <https://gh.io/copilot-cli>). An extension may not take a name that
collides with a built-in command or alias, so the legacy `github/gh-copilot` extension is
uninstallable on this gh version. `gh alias list` here contains only `co: pr checkout`, so the
collision is with the built-in command, not a user alias.

The Companion's install hint is stale — the fix is **not** the hinted command.

## What the Companion actually checks

Its Copilot provider (`CopilotCliProvider`, `installHint.installCommand` = the stale gh command above,
`cliBinary` = `copilot`) decides "installed" purely by running:

```text
copilot --version
```

There is no `gh` involvement in the check. So the CLI must exist as `copilot` on `PATH`.

## The fix that worked

```powershell
npm install -g @github/copilot
```

The real CLI is the npm global package **`@github/copilot`** (`node` `v24.18.0`, `npm` `11.16.0`
here; global bin is `C:\Users\jkoll\AppData\Roaming\npm`).

Do **not** use `gh extension install github/gh-copilot`, and do not add a hand-rolled `copilot`
shim: the GitHub Copilot Chat extension already ships one at
`%APPDATA%\Code\User\globalStorage\github.copilot-chat\copilotCli\{copilot,copilot.bat,copilot.ps1}`.
That bootstrapper locates the real binary by temporarily removing its own directory from `PATH`, so
once the npm package exists the shim resolves correctly.

Verified after install — both entry points return promptly with no prompt:

```text
copilot --version                                  -> GitHub Copilot CLI 1.0.98
...\copilot-chat\copilotCli\copilot.bat --version  -> GitHub Copilot CLI 1.0.98
```

## Hazard worth remembering

Before the npm package existed, `copilot --version` hit the bootstrapper's
`Read-Host "Install GitHub Copilot CLI? (y/N)"` and **blocked**, which killed the invoking shell in an
automated terminal. Any agent-run check of `copilot --version` should therefore be wrapped in a
timeout (e.g. a PowerShell job with `Wait-Job -Timeout`) rather than run bare.

## Related setting

The Companion's provider comes from the user setting `speckit.aiProvider`, currently
`"copilot"` in `%APPDATA%\Code\User\settings.json` (no `speckit.*` entry in the workspace
`.vscode/settings.json`). Switching that value to another provider is the only alternative to having
the CLI on `PATH` — the owner's configuration of record is `copilot`, so the CLI stays installed.
