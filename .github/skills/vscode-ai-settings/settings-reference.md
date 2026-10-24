# Settings, grouped

Extracted from the live VS Code 1.143.1 registry on this machine — defaults below were read from
the shipping `workbench.desktop.main.js` registration or the built-in `copilot` extension manifest,
not from documentation. 455 AI-related settings exist; the high-signal ones are here.

**Caveat you should know.** Eight customization settings are *read by key* but not declared in the
local registration table: `chat.useAgentsMdFile`, `chat.useNestedAgentsMdFiles`, `chat.useClaudeMdFile`,
`chat.useAgentSkills`, `chat.instructionsFilesLocations`, `chat.agentFilesLocations`,
`chat.hookFilesLocations`, `chat.promptFilesRecommendations`. They are forwarded to the Copilot
harness, which owns their schema, so `dump_settings.py` reports them as `NOT FOUND`. They work — their
defaults below come from the VS Code documentation and are the only values not machine-verified here.

Legend: `experimental` / `preview` come from the setting's `tags`. `DEPRECATED` means the
registration carries a deprecation message — do not set it.

## Core chat

`chat.commandCenter.enabled` `true` · `chat.disableAIFeatures` `false` ·
`chat.verbose` `true` · `chat.fontSize` `13` · `chat.math.enabled` **`true`** ·
`chat.stickyScroll.enabled` `true` · `chat.viewTitle.enabled` `true` ·
`chat.contextUsage.enabled` `true` · `chat.defaultModel` `""`

## Requests

| Setting | Default | Notes |
| --- | --- | --- |
| `chat.requestQueuing.defaultAction` | **`"steer"`** | `queue` \| `steer` — steer is now the default |
| `chat.notifyWindowOnResponseReceived` | `"windowNotFocused"` | `off` \| `windowNotFocused` \| `always` |
| `chat.notifyWindowOnConfirmation` | `"windowNotFocused"` | |
| `chat.experimental.persistentProgress` | `"draw"` | experimental; `off`/`draw`/`drawMonochrome`/`drawMonochromeNoIcon` |
| `chat.experimental.persistentProgressVerbosity` | `"compact"` | experimental |
| `chat.emptyState.history.enabled` | `false` | experimental |

## Agents

| Setting | Default | Notes |
| --- | --- | --- |
| `chat.agent.enabled` | `true` | |
| `chat.agent.maxRequests` | **`50`** | dynamic via entitlement; Agents window default `1000` |
| `chat.agent.networkFilter` | `false` | |
| `chat.agent.collapseCompletedResponses` | `true` | |
| `chat.agent.thinkingStyle` | `"fixedScrolling"` | experimental; `collapsed`/`collapsedPreview`/`fixedScrolling` |
| `chat.agent.thinking.collapsedTools` | `"always"` | experimental; `off`/`withThinking`/`always` |
| `chat.agent.thinking.terminalTools` | `true` | experimental |
| `chat.agent.codeBlockProgress` | `true` | experimental |
| `chat.autopilot.advanced.enabled` | `false` | experimental |

## Subagents, sessions, context

`chat.subagents.allowInvocationsFromSubagents` `false` · `chat.subagents.showCreditUsage` `false` ·
`chat.checkpoints.enabled` `true` · `chat.checkpoints.showFileChanges` `false` ·
`chat.implicitContext.suggestedContext` `true` ·
`chat.implicitContext.includeActiveEditor` `true` (experimental) ·
`chat.experimental.workspaceSnapshot` `false` (experimental)

## Tools

| Setting | Default | Notes |
| --- | --- | --- |
| `chat.tools.terminal.autoApprove` | `{}` | command → bool or `/regex/` |
| `chat.tools.terminal.enableAutoApprove` | **`true`** | |
| `chat.tools.terminal.blockDetectedFileWrites` | `"outsideWorkspace"` | experimental; `never`/`outsideWorkspace`/`all` |
| `chat.tools.terminal.preventShellHistory` | `true` | |
| `chat.tools.terminal.idlePollInterval` | `1000` | |
| `chat.tools.terminal.outputLocation` | `"chat"` | experimental; `chat`/`terminal` |
| `chat.tools.terminal.outputReflow` | `true` | |
| `chat.tools.terminal.enforceTimeoutFromModel` | `true` | experimental |
| `chat.tools.terminal.ignoreDefaultAutoApproveRules` | `false` | experimental |
| `chat.tools.terminal.detachBackgroundProcesses` | `false` | experimental |
| `chat.tools.global.autoApprove` | `false` | experimental |
| `chat.tools.edits.autoApprove` | `{}` | per-path edit approval |
| `chat.tools.urls.autoApprove` | `{}` | per-domain URL approval |
| `chat.tools.eligibleForAutoApproval` | `{}` | |
| `chat.tools.autoExpandFailures` | `true` | |
| `chat.tools.confirmationCarousel.enabled` | `true` | |
| `chat.tools.riskAssessment.enabled` | `true` | experimental |
| `chat.tools.riskAssessment.model` | `"copilot-utility-small"` | experimental |
| `chat.tools.compressOutput.enabled` | `false` | preview |
| `chat.tools.todos.showWidget` | `true` | |

## Sandboxing

`chat.agent.sandbox.enabled` `"off"` (`off`/`on`) ·
`network.allowNetwork` `true` · `network.allowLocalNetwork` `false` ·
`network.allowedDomains` `[]` · `network.deniedDomains` `[]` ·
`fileSystem.allowDevToolAccess` `true` · `fileSystem.userConfiguredPaths` `[]` ·
`mcpServers` `true` · `lspServers` `true` ·
`credentials.authenticategit` `true` · `credentials.authenticategh` `true` ·
`allowUnsandboxedCommands` `true`

DEPRECATED: `sandbox.advanced.runtime`, `sandbox.retryWithAllowNetworkRequests`,
`sandbox.fileSystem.linux`, `sandbox.fileSystem.mac`.

## MCP

| Setting | Default | Notes |
| --- | --- | --- |
| `chat.mcp.access` | **`"all"`** | **string enum** `none` \| `registry` \| `all` — not a boolean |
| `chat.mcp.discovery.enabled` | `false` | |
| `chat.mcp.autostart` | `"newAndOutdated"` | experimental; `never`/`onlyNew`/`newAndOutdated` |
| `chat.mcp.serverSampling` | `{}` | per-server `allowedDuringChat` (true), `allowedOutsideChat` (false), `allowedModels` |
| `chat.mcp.apps.enabled` | `true` | experimental |
| `chat.mcp.collisionBehavior` | `"disable"` | `disable` \| `suffix` |
| `chat.mcp.allowManagedServersOnly` | `false` | |
| `chat.mcp.allowedServers` / `deniedServers` | unset | |
| `chat.mcp.workspaceRootConfig.enabled` | `true` | experimental |
| `chat.mcp.gallery.enabled` | `false` | preview |

## Customization discovery

Defaults here are documentation-sourced (not in the local registry — see the caveat above).

`chat.instructionsFilesLocations` `{".github/instructions",".claude/rules","~/.copilot/instructions","~/.claude/rules"}` ·
`chat.agentFilesLocations` `{".github/agents",".claude/agents","~/.copilot/agents"}` ·
`chat.useAgentsMdFile` `true` · `chat.useNestedAgentsMdFiles` `false` ·
`chat.useClaudeMdFile` `true` · `chat.useAgentSkills` `true` · `chat.useHooks` `true` ·
`chat.useClaudeHooks` `false` · `chat.hookFilesLocations` `{}` ·
`chat.useCustomizationsInParentRepositories` `false` ·
`chat.promptFilesRecommendations` `[]` · `chat.promptFilesLocations` DEPRECATED

Plugins: `chat.plugins.enabled` `true` · `chat.plugins.enabledPlugins` unset ·
`chat.plugins.extraMarketplaces` `{}` · `chat.plugins.marketplaces` `[]` (experimental) ·
`chat.pluginLocations` `{}` (experimental)

Marketplace/migration (all experimental): `chat.customizations.marketplace.enabled` `false` ·
`chat.customizations.migration.enabled` `true` · `chat.customizations.promptMigration.enabled` `true` ·
`chat.customizations.strictPluginOnlyCustomization` `false`

## Observability

| Setting | Default |
| --- | --- |
| `github.copilot.chat.otel.enabled` | `false` |
| `github.copilot.chat.otel.exporterType` | `"otlp-http"` (`otlp-grpc`/`otlp-http`/`console`/`file`) |
| `github.copilot.chat.otel.otlpEndpoint` | `"http://localhost:4318"` |
| `github.copilot.chat.otel.protocol` | `""` (`http/json`/`http/protobuf`/`grpc`) |
| `github.copilot.chat.otel.serviceName` | `""` |
| `github.copilot.chat.otel.headers` / `resourceAttributes` | `{}` |
| `github.copilot.chat.otel.captureContent` / `captureIdentity` | `false` / unset |
| `github.copilot.chat.otel.outfile` | `""` |
| `github.copilot.chat.otel.dbSpanExporter.enabled` | `false` |
| `chat.agentHost.otel.captureIdentity` | `false` |
| `chat.agentHost.debugLogs.defaultExportLocation` | `""` |
| `github.copilot.chat.agentDebugLog.fileLogging.enabled` | `false` (experimental) |

## Copilot extension

`github.copilot.enable` `{"*": true, "plaintext": false, "markdown": false, "scminput": false}` ·
`github.copilot.editor.enableCodeActions` `true` ·
`github.copilot.nextEditSuggestions.enabled` `true` · `.fixes` `true` ·
`github.copilot.renameSuggestions.triggerAutomatically` `true` ·
`github.copilot.chat.cloudAgent.enabled` `true` ·
`github.copilot.chat.organizationCustomAgents.enabled` `true` ·
`github.copilot.chat.organizationInstructions.enabled` `true` ·
`github.copilot.chat.codeGeneration.useInstructionFiles` `true` ·
`github.copilot.chat.customInstructionsInSystemMessage` `true` ·
`github.copilot.chat.imageUpload.enabled` `true` ·
`github.copilot.chat.reviewAgent.enabled` `true` · `reviewSelection.enabled` `true` ·
`github.copilot.chat.scopeSelection` `false` ·
`github.copilot.chat.localIndex.enabled` `true` ·
`github.copilot.chat.terminalChatLocation` `"chatView"` (`chatView`/`quickChat`/`terminal`) ·
`github.copilot.chat.localeOverride` `"auto"` ·
`github.copilot.chat.modelCapabilityOverrides` `{}` ·
`github.copilot.chat.reasoningEffortOverride` unset ·
`github.copilot.chat.rateLimitAutoSwitchToAuto` `false` ·
`github.copilot.chat.chatCompletionsTokenParameter` `"max_tokens"` ·
`github.copilot.chat.agent.temperature` unset (experimental) ·
`github.copilot.chat.agent.largeToolResultsToDisk.enabled` `true` (experimental) ·
`github.copilot.chat.agent.largeToolResultsToDisk.thresholdBytes` `8192`

Generated-text instruction sources (arrays of `{text}` or `{file}`):
`reviewSelection.instructions`, `commitMessageGeneration.instructions`,
`pullRequestDescriptionGeneration.instructions`.

DEPRECATED: all 14 `github.copilot.chat.cli.*` toggles except
`cli.terminalLinks.enabled`; `chat.codeGeneration.instructions`;
`chat.testGeneration.instructions`; `chat.agent.modelDetails.enabled`;
`chat.backgroundAgent.enabled`.

## Copilot harness extras (mostly experimental)

`chat.copilot.memory.enabled` `false` (preview) · `chat.copilot.memory.local.enabled` `false` ·
`chat.copilot.localModel.enabled` `false` · `chat.copilot.tgrep.enabled` `false` ·
`chat.copilot.searchSubagent.enabled` `false` · `chat.copilot.skillCharBudget` `30000` ·
`chat.copilot.claudeAdvisor.enabled` `false` · `chat.copilot.ensemble.enabled` `false` ·
`chat.agentHost.byokModels.enabled` `true` · `chat.agentHost.allowSignedOutWhenUsable` `false`

## Editor and inline

`chat.editor.fontFamily` `"default"` · `chat.editor.fontWeight` `"default"` ·
`chat.editor.lineHeight` `0` · `chat.editor.wordWrap` `"off"` ·
`editor.inlineSuggest.showToolbar` `"onHover"` ·
`editor.inlineSuggest.edits.allowCodeShifting` `"always"` ·
`editor.inlineSuggest.edits.renderSideBySide` `"auto"` ·
`imageCarousel.chat.enabled` `false` (experimental)
