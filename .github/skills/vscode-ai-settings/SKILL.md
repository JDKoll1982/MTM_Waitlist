---
name: vscode-ai-settings
description: Look up, validate, and recommend VS Code AI and Copilot settings with correct names, defaults, and types. Use when configuring chat, agents, approvals, sandboxing, MCP, or customization discovery, when checking whether a setting exists, or when a settings.json value is being ignored.
argument-hint: '[setting name or goal]'
---

# VS Code AI settings

Settings drift between releases. Confirm the name and default before asserting behavior, and quote the default so the user can tell a change from a restatement.

## Never guess a setting name

* Settings editor (`Ctrl+,`) → search `chat.` or `github.copilot.chat.`
* `Preferences: Open User Settings (JSON)` for the raw file
* `python scripts/check_settings.py --settings "%APPDATA%\Code\User\settings.json"` to validate keys against 100+ known names, flag deprecated entries, and catch wrong value types and invalid enum choices
* The full grouped catalog, extracted from the live registry, is in [settings-reference.md](./settings-reference.md)

## The settings that change behavior most

| Setting | Default | Use it to |
| --- | --- | --- |
| `chat.agent.enabled` | `true` | Turn agent mode off entirely |
| `chat.agent.maxRequests` | `50` | Cap tool round-trips per turn (Agents window defaults to `1000`) |
| `chat.requestQueuing.defaultAction` | `"steer"` | Already steers; set `"queue"` to stack follow-ups instead |
| `chat.contextUsage.enabled` | `true` | Show context-window usage |
| `chat.tools.terminal.autoApprove` | `{}` | Add per-command allow/deny rules |
| `chat.tools.terminal.enableAutoApprove` | `true` | `false` forces approval for every command |
| `chat.tools.terminal.blockDetectedFileWrites` | `"outsideWorkspace"` | `never` \| `outsideWorkspace` \| `all` |
| `chat.tools.global.autoApprove` | `false` | One switch for every tool (experimental) |
| `chat.agent.sandbox.enabled` | `"off"` | Sandbox agent terminal commands |
| `chat.agent.sandbox.network.allowLocalNetwork` | `false` | Required on Windows for proxy/hostname rules |
| `chat.mcp.access` | `"all"` | **String enum** `none` \| `registry` \| `all` |
| `chat.mcp.autostart` | `"newAndOutdated"` | `never` \| `onlyNew` \| `newAndOutdated` |
| `chat.subagents.allowInvocationsFromSubagents` | `false` | Let subagents spawn subagents |
| `chat.checkpoints.enabled` | `true` | Restore-to-checkpoint |
| `chat.instructionsFilesLocations` | 4 native folders | Extra instruction locations (prefer native folders) |
| `chat.useAgentsMdFile` | `true` | Attach workspace-root `AGENTS.md` |
| `chat.useNestedAgentsMdFiles` | `false` | Discover nested `AGENTS.md` |
| `chat.useClaudeMdFile` | `true` | Use `CLAUDE.md` as always-on instructions |
| `chat.useAgentSkills` | `true` | Enable skills |
| `chat.useHooks` | `true` | Enable hooks (Local harness) |
| `chat.useCustomizationsInParentRepositories` | `false` | Monorepo parent discovery |
| `chat.plugins.enabled` | `true` | Enable agent plugins |
| `github.copilot.chat.skillTool.enabled` | `false` | Needed for `context: fork` skills |
| `github.copilot.chat.otel.enabled` | `false` | Export Copilot telemetry over OTLP |

## Rules of thumb

* **Prefer a customization file over a setting** when the content is guidance. Settings toggles belong in settings.json.
* **Sandboxing is not a network block.** `chat.agent.sandbox.network.allowNetwork` defaults to `true`; restrict with `allowedDomains` / `deniedDomains`.
* **Approval is not a security boundary.** Auto-approve rules are best effort; sandbox or a container for real isolation.
* **On Windows**, a proxy, hostname restriction, or credential masking needs `chat.agent.sandbox.network.allowLocalNetwork: true`, because the sandbox reaches a local listener over loopback. Without it, sandboxed commands fail to start.
* **Several settings are enums, not booleans.** `chat.mcp.access` is `"all"`, not `true`. `chat.agent.sandbox.enabled` is `"off"`/`"on"`. Passing a boolean silently does nothing useful.
* Defaults can be entitlement-dependent: `chat.agent.maxRequests` resolves through a policy and falls back to `50`.
* Experimental and Preview settings are not contract. State the VS Code version when relying on one.
* Enterprise-managed settings override user settings and can disable harnesses entirely.

## Deprecated — do not recommend

| Deprecated | Replacement |
| --- | --- |
| `chat.modeFilesLocations` | `chat.agentFilesLocations` |
| `chat.promptFilesLocations` | Skills, or profile prompt storage |
| `chat.agent.sandbox.fileSystem.<platform>` | `chat.agent.sandbox.fileSystem.userConfiguredPaths` |
| `chat.agent.sandbox.advanced.runtime`, `.retryWithAllowNetworkRequests` | remove; configure network settings directly |
| `chat.tools.terminal.shellIntegrationTimeout` | `terminal.integrated.shellIntegration.timeout` |
| `chat.tools.terminal.backgroundNotifications` | remove |
| `chat.experimental.detectParticipant.enabled` | `chat.detectParticipant.enabled` |
| `github.copilot.chat.anthropic.thinking.effort` | Model picker thinking effort |
| `github.copilot.chat.responsesApiReasoningEffort` | Model picker thinking effort |
| `github.copilot.chat.codeGeneration.instructions` / `.testGeneration.instructions` | File-based instructions |
| `github.copilot.chat.cli.*.enabled` (13 of 14) | CLI-side features; only `cli.terminalLinks.enabled` remains valid |

## Reference files

* [scripts/check_settings.py](./scripts/check_settings.py) - validates a settings.json against 100+ known names, defaults, and types; reports wrong types, invalid enum choices, deprecated keys, unknown AI keys, and cross-setting advisories
* [scripts/dump_settings.py](./scripts/dump_settings.py) - reads the shipping registry so you can re-verify every default after a VS Code update
* [settings-reference.md](./settings-reference.md) - grouped defaults, with the provenance caveat for harness-forwarded settings

## Re-verify after an update

Documentation lags the build. When a default matters:

```bash
python scripts/dump_settings.py --check chat.agent.maxRequests chat.mcp.access
python scripts/dump_settings.py --deprecated
python scripts/dump_settings.py --tagged experimental
```

If a key returns `NOT FOUND`, it is not declared in the local registry — it is read by name and
owned by the Copilot harness, so the published docs are its only source.
