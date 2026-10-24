---
name: 'AI settings reference'
description: 'High-signal map of VS Code AI/agent/chat settings with defaults: chat display, agent limits, tool approvals, terminal, MCP, and customization discovery. Use when configuring AI behavior or checking whether a setting exists and what it defaults to.'
---
# AI settings reference

Verified on VS Code 1.143.1. The authoritative list is the AI settings reference page and the Settings editor; this file is the working subset.

## How to inspect instead of guessing

* Settings editor (`Ctrl+,`) → search `chat.` or `github.copilot.chat.`
* `Preferences: Open User Settings (JSON)` for the raw file
* Settings marked *(Experimental)* or *(Preview)* can change or disappear between releases

## Core

| Setting | Default |
| --- | --- |
| `chat.commandCenter.enabled` | `true` |
| `chat.disableAIFeatures` | `false` |
| `chat.agent.enabled` | `true` |
| `chat.agent.maxRequests` | `50` (Agents window: `1000`) |
| `chat.verbose` | `true` |
| `chat.fontSize` / `chat.editor.fontSize` | `13` / `14` |
| `chat.stickyScroll.enabled` | `true` |
| `chat.math.enabled` | `true` |

## Requests and notifications

| Setting | Default |
| --- | --- |
| `chat.requestQueuing.defaultAction` | `"steer"` |
| `chat.notifyWindowOnResponseReceived` | `"windowNotFocused"` |
| `chat.notifyWindowOnConfirmation` | `"windowNotFocused"` |
| `chat.experimental.persistentProgress` | `"draw"` |
| `chat.experimental.persistentProgressVerbosity` | `"compact"` |
| `chat.viewTitle.enabled` | `true` |
| `chat.emptyState.history.enabled` | `false` |
| `chat.contextUsage.enabled` | `true` |

## Tools and terminal

| Setting | Default |
| --- | --- |
| `chat.tools.terminal.autoApprove` | `{}` (built-in rules still apply) |
| `chat.tools.terminal.enableAutoApprove` | `true` |
| `chat.tools.terminal.blockDetectedFileWrites` | `"outsideWorkspace"` (`never`/`outsideWorkspace`/`all`) |
| `chat.tools.terminal.autoReplyToPrompts` | `false` |
| `chat.tools.terminal.preventShellHistory` | `true` |
| `chat.tools.terminal.idlePollInterval` | `1000` |
| `chat.tools.terminal.outputLocation` | `"chat"` |
| `chat.tools.terminal.outputReflow` | `true` |
| `chat.tools.terminal.enforceTimeoutFromModel` | `true` |
| `chat.tools.edits.autoApprove` / `chat.tools.urls.autoApprove` | `{}` |
| `chat.tools.global.autoApprove` | `false` (experimental) |
| `chat.tools.compressOutput.enabled` | `false` |
| `chat.tools.riskAssessment.enabled` | `true` |
| `chat.tools.todos.showWidget` | `true` |
| `chat.agent.sandbox.enabled` | `"off"` (`off`/`on`) |

`chat.agent.sandbox.fileSystem.<platform>` (`allowRead`, `allowWrite`, `denyRead`, `denyWrite`) is deprecated in favor of `chat.agent.sandbox.fileSystem.userConfiguredPaths`.

## MCP

| Setting | Default |
| --- | --- |
| `chat.mcp.access` | `"all"` — **string enum** `none` / `registry` / `all`, not a boolean |
| `chat.mcp.discovery.enabled` | `false` |
| `chat.mcp.autostart` | `"newAndOutdated"` (`never`/`onlyNew`/`newAndOutdated`) |
| `chat.mcp.serverSampling` | `{}` |
| `chat.mcp.apps.enabled` | `true` |
| `chat.mcp.collisionBehavior` | `"disable"` (`disable`/`suffix`) |
| `chat.mcp.allowManagedServersOnly` | `false` |
| `chat.mcp.workspaceRootConfig.enabled` | `true` (experimental) |

## Sessions, subagents, context

| Setting | Default |
| --- | --- |
| `chat.subagents.allowInvocationsFromSubagents` | `false` |
| `chat.subagents.showCreditUsage` | `false` |
| `chat.checkpoints.enabled` | `true` |
| `chat.checkpoints.showFileChanges` | `false` |
| `chat.implicitContext.suggestedContext` | `true` |
| `chat.implicitContext.includeActiveEditor` | `true` (experimental) |
| `chat.agentSessions.showExternal` | `"last7Days"` (experimental) |

## Observability

| Setting | Default |
| --- | --- |
| `github.copilot.chat.otel.enabled` | `false` |
| `github.copilot.chat.otel.exporterType` | `"otlp-http"` (`otlp-grpc`/`otlp-http`/`console`/`file`) |
| `github.copilot.chat.otel.otlpEndpoint` | `"http://localhost:4318"` |
| `github.copilot.chat.otel.captureContent` / `captureIdentity` | `false` / unset |
| `chat.agentHost.otel.captureIdentity` | `false` |
| `github.copilot.chat.agentDebugLog.fileLogging.enabled` | `false` (experimental) |

## Copilot extension

`github.copilot.chat.cloudAgent.enabled` `true` ·
`github.copilot.chat.organizationCustomAgents.enabled` `true` ·
`github.copilot.chat.codeGeneration.useInstructionFiles` `true` ·
`github.copilot.chat.reviewAgent.enabled` `true` ·
`github.copilot.chat.imageUpload.enabled` `true` ·
`github.copilot.chat.localIndex.enabled` `true` ·
`github.copilot.chat.scopeSelection` `false`

The full grouped catalog, extracted from the live registry, lives in the `vscode-ai-settings` skill
(`settings-reference.md`). Load it instead of guessing a name.

## Customization discovery

| Setting | Default |
| --- | --- |
| `chat.instructionsFilesLocations` | `{ ".github/instructions": true, ".claude/rules": true, "~/.copilot/instructions": true, "~/.claude/rules": true }` |
| `chat.agentFilesLocations` | `{ ".github/agents": true, ".claude/agents": true, "~/.copilot/agents": true }` |
| `chat.promptFilesLocations` *(deprecated)* | `{ ".github/prompts": true }` |
| `chat.promptFilesRecommendations` | `[]` |
| `chat.useAgentsMdFile` | `true` |
| `chat.useNestedAgentsMdFiles` | `false` |
| `chat.useClaudeMdFile` | `true` |
| `chat.useCustomizationsInParentRepositories` | `false` |
| `chat.hookFilesLocations` *(Preview)* | `{}` |
| `chat.useHooks` | `true` |
| `chat.useAgentSkills` | `true` |
| `github.copilot.chat.skillTool.enabled` | `false` |
| `chat.plugins.enabled` | `true` |
| `chat.plugins.marketplaces` *(Experimental)* | `[]` |
| `chat.customizations.marketplace.enabled` *(Experimental)* | `false` |
| `github.copilot.chat.organizationInstructions.enabled` | `true` |
| `github.copilot.chat.organizationCustomAgents.enabled` | `true` |

## Code editing

| Setting | Default |
| --- | --- |
| `github.copilot.enable` | `{ "*": true, "plaintext": false, "markdown": false, "scminput": false }` |
| `github.copilot.editor.enableCodeActions` | `true` |
| `github.copilot.nextEditSuggestions.enabled` | `true` |
| `github.copilot.nextEditSuggestions.fixes` | `true` |
| `github.copilot.renameSuggestions.triggerAutomatically` | `true` |
| `editor.inlineSuggest.showToolbar` | `"onHover"` |
| `editor.inlineSuggest.edits.allowCodeShifting` | `"always"` |
| `editor.inlineSuggest.edits.renderSideBySide` | `"auto"` |

## Instruction sources for generated text

`github.copilot.chat.reviewSelection.instructions`, `github.copilot.chat.commitMessageGeneration.instructions`, `github.copilot.chat.pullRequestDescriptionGeneration.instructions` accept `[{ "text": ... } | { "file": ... }]`.

Settings-based *code generation* and *test generation* instructions are deprecated since 1.102 — use file-based instructions.

## Rules

* Quote the default when recommending a change, so the user can tell a modification from a restatement.
* **Watch the type.** Several settings are enums, not booleans: `chat.mcp.access` is `"all"`
  (`none`/`registry`/`all`), `chat.agent.sandbox.enabled` is `"off"`/`"on"`, and
  `chat.tools.terminal.blockDetectedFileWrites` accepts `never`/`outsideWorkspace`/`all` — `"none"`
  is not a valid value. A wrong-typed value is silently ignored.
* **Defaults can be dynamic.** `chat.agent.maxRequests` resolves through an entitlement policy and
  falls back to `50`; the Agents window overrides it to `1000`.
* Do not set deprecated keys. Known deprecated: `chat.modeFilesLocations`,
  `chat.promptFilesLocations`, `chat.agent.sandbox.fileSystem.<platform>`,
  `chat.agent.sandbox.advanced.runtime`, `chat.agent.sandbox.retryWithAllowNetworkRequests`,
  `chat.tools.terminal.shellIntegrationTimeout`, `chat.tools.terminal.backgroundNotifications`,
  `chat.experimental.detectParticipant.enabled`, `github.copilot.chat.anthropic.thinking.effort`,
  `github.copilot.chat.responsesApiReasoningEffort`,
  `github.copilot.chat.codeGeneration.instructions`,
  `github.copilot.chat.testGeneration.instructions`, and 13 of the 14
  `github.copilot.chat.cli.*.enabled` toggles.
* Experimental/Preview settings are not contract: confirm the build before asserting behavior.
* Enterprise-managed settings can override or disable user settings, including which harnesses are available.
* Prefer a customization file over a setting whenever the guidance is content (conventions, rules) rather than a toggle.
* For the complete grouped catalog, use the `vscode-ai-settings` skill rather than inventing a name.
