# Repo Memory (pushable)

This folder is the **durable, git-committed home** for repository-scoped memory that should survive
`git push` and travel with the repo. VS Code's Copilot "memory tool" (`/memories/repo/...`) stores its
files in local workspace storage **outside** this repo
(`.../AppData/Roaming/Code/User/workspaceStorage/<ws>/GitHub.copilot-chat/memory-tool/memories/...`),
so those are machine-local and will NOT be committed when you push.

**Convention:**
- Keep canonical repo facts in committed files under `.github/memories/` (this folder) and/or
  `Documents/Development/**`. Anything here is saved on `git push`.
- When the Copilot memory tool is used in a session, prefer pointing its `/memories/repo/*` notes at
  these committed files rather than treating the ephemeral store as the source of truth.
- Sanitize credentials: reference dev accounts/env vars/appsettings rather than echoing passwords.
  **One owner-approved exception (2026-09-11):** `repo/workstation-elevation.md` records this workstation's
  local admin account, because an agent or operator working on this machine cannot elevate without it.
  The exception is scoped to that one host; do not carry the pattern anywhere else.

## Index
- `repo/infor-visual-disposition.md` — Infor Visual status codes + FG/WIP/Outside derivation +
  MTM WIP Application facts + tooling quirks (file 14 Phase 6). Canonical long-form note:
  `Documents/Development/InforVisual/Phase6-Disposition-StatusCodes-Research.md`.
- `repo/workstation-elevation.md` — which account to use when administrator rights are needed on this
  workstation, when elevation is *not* needed, and the owner-approved exception that lets it name a password.
