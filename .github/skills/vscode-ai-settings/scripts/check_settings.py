#!/usr/bin/env python3
"""Validate VS Code AI settings against known names, defaults, and types.

Reads a settings.json (JSONC: comments and trailing commas are tolerated) and
reports unknown AI settings, deprecated settings, and values of the wrong type.

Only AI-related keys are checked; every other VS Code setting is ignored.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys

# name -> (default, expected python type(s), note)
CATALOG: dict[str, tuple[object, tuple[type, ...], str]] = {
    "chat.commandCenter.enabled": (True, (bool,), ""),
    "chat.disableAIFeatures": (False, (bool,), ""),
    "chat.agent.enabled": (True, (bool,), ""),
    "chat.agent.maxRequests": (50, (int,), "Agents window default is 1000"),
    "chat.agent.networkFilter": (False, (bool,), ""),
    "chat.agent.collapseCompletedResponses": (True, (bool,), ""),
    "chat.verbose": (True, (bool,), ""),
    "chat.fontSize": (13, (int,), ""),
    "chat.editor.fontSize": (14, (int,), ""),
    "chat.stickyScroll.enabled": (True, (bool,), ""),
    "chat.math.enabled": (True, (bool,), ""),
    "chat.viewTitle.enabled": (True, (bool,), ""),
    "chat.emptyState.history.enabled": (False, (bool,), ""),
    "chat.requestQueuing.defaultAction": ("steer", (str,), "queue | steer"),
    "chat.notifyWindowOnResponseReceived": ("windowNotFocused", (str,), "off | windowNotFocused | always"),
    "chat.notifyWindowOnConfirmation": ("windowNotFocused", (str,), "off | windowNotFocused | always"),
    "chat.experimental.persistentProgress": ("draw", (str,), "off | draw | drawMonochrome | drawMonochromeNoIcon"),
    "chat.experimental.persistentProgressVerbosity": ("compact", (str,), "compact | verbose"),
    "chat.tools.terminal.autoApprove": ({}, (dict,), "command -> bool or /regex/"),
    "chat.tools.terminal.enableAutoApprove": (True, (bool,), "false = always require approval"),
    "chat.tools.terminal.blockDetectedFileWrites": ("outsideWorkspace", (str,), "never | outsideWorkspace | all"),
    "chat.tools.terminal.autoReplyToPrompts": (False, (bool,), ""),
    "chat.tools.terminal.preventShellHistory": (True, (bool,), ""),
    "chat.tools.terminal.idlePollInterval": (1000, (int,), ""),
    "chat.tools.terminal.outputLocation": ("chat", (str,), "chat | terminal"),
    "chat.tools.terminal.outputReflow": (True, (bool,), ""),
    "chat.tools.terminal.enforceTimeoutFromModel": (True, (bool,), ""),
    "chat.tools.terminal.ignoreDefaultAutoApproveRules": (False, (bool,), "experimental"),
    "chat.tools.autoExpandFailures": (True, (bool,), ""),
    "chat.tools.confirmationCarousel.enabled": (True, (bool,), ""),
    "chat.tools.riskAssessment.enabled": (True, (bool,), "experimental"),
    "chat.tools.riskAssessment.model": ("copilot-utility-small", (str,), "experimental"),
    "chat.tools.compressOutput.enabled": (False, (bool,), "preview"),
    "chat.tools.global.autoApprove": (False, (bool,), "experimental"),
    "chat.tools.edits.autoApprove": ({}, (dict,), ""),
    "chat.tools.urls.autoApprove": ({}, (dict,), ""),
    "chat.tools.eligibleForAutoApproval": ({}, (dict,), ""),
    "chat.tools.todos.showWidget": (True, (bool,), ""),
    "chat.agent.sandbox.enabled": ("off", (str, bool), "off | on"),
    "chat.agent.sandbox.network.allowNetwork": (True, (bool,), ""),
    "chat.agent.sandbox.network.allowLocalNetwork": (False, (bool,), "required on Windows for proxy/masking"),
    "chat.agent.sandbox.network.allowedDomains": ([], (list,), ""),
    "chat.agent.sandbox.network.deniedDomains": ([], (list,), ""),
    "chat.agent.sandbox.fileSystem.userConfiguredPaths": ({}, (dict,), ""),
    "chat.agent.sandbox.fileSystem.allowDevToolAccess": (True, (bool,), ""),
    "chat.agent.sandbox.mcpServers": (True, (bool,), ""),
    "chat.agent.sandbox.lspServers": (True, (bool,), ""),
    "chat.agent.sandbox.credentials.authenticategit": (True, (bool,), ""),
    "chat.agent.sandbox.credentials.authenticategh": (True, (bool,), ""),
    "chat.agent.sandbox.allowUnsandboxedCommands": (True, (bool,), ""),
    "chat.mcp.access": ("all", (str,), "none | registry | all"),
    "chat.mcp.discovery.enabled": (False, (bool,), ""),
    "chat.mcp.autostart": ("newAndOutdated", (str,), "never | onlyNew | newAndOutdated"),
    "chat.mcp.serverSampling": ({}, (dict,), ""),
    "chat.mcp.apps.enabled": (True, (bool,), ""),
    "chat.mcp.collisionBehavior": ("disable", (str,), "disable | suffix"),
    "chat.mcp.allowManagedServersOnly": (False, (bool,), ""),
    "chat.mcp.allowedServers": (None, (dict, type(None)), ""),
    "chat.mcp.deniedServers": (None, (dict, type(None)), ""),
    "chat.mcp.workspaceRootConfig.enabled": (True, (bool,), "experimental"),
    "chat.subagents.allowInvocationsFromSubagents": (False, (bool,), ""),
    "chat.subagents.showCreditUsage": (False, (bool,), ""),
    "chat.checkpoints.enabled": (True, (bool,), ""),
    "chat.checkpoints.showFileChanges": (False, (bool,), ""),
    "chat.contextUsage.enabled": (True, (bool,), ""),
    "chat.implicitContext.suggestedContext": (True, (bool,), ""),
    "chat.implicitContext.includeActiveEditor": (True, (bool,), "experimental"),
    "chat.instructionsFilesLocations": ({}, (dict,), ""),
    "chat.agentFilesLocations": ({}, (dict,), ""),
    "chat.promptFilesRecommendations": ([], (list,), ""),
    "chat.useAgentsMdFile": (True, (bool,), ""),
    "chat.useNestedAgentsMdFiles": (False, (bool,), "requires chat.useAgentsMdFile"),
    "chat.useClaudeMdFile": (True, (bool,), ""),
    "chat.useCustomizationsInParentRepositories": (False, (bool,), ""),
    "chat.useHooks": (True, (bool,), "local harness"),
    "chat.useClaudeHooks": (False, (bool,), ""),
    "chat.hookFilesLocations": ({}, (dict,), "preview"),
    "chat.plugins.enabled": (True, (bool,), ""),
    "chat.plugins.marketplaces": ([], (list,), "experimental"),
    "chat.pluginLocations": ({}, (dict,), "experimental"),
    "chat.customizations.marketplace.enabled": (True, (bool,), ""),
    "chat.agentHost.byokModels.enabled": (False, (bool,), ""),
    "github.copilot.chat.skillTool.enabled": (False, (bool,), "needed for context: fork"),
    "github.copilot.chat.organizationInstructions.enabled": (True, (bool,), ""),
    "github.copilot.chat.agent.autoFix": (True, (bool,), ""),
    "github.copilot.chat.localeOverride": ("auto", (str,), ""),
    "github.copilot.chat.useProjectTemplates": (True, (bool,), ""),
    "github.copilot.chat.terminalChatLocation": ("chatView", (str,), ""),
    "github.copilot.chat.reviewSelection.instructions": ([], (list,), ""),
    "github.copilot.chat.commitMessageGeneration.instructions": ([], (list,), ""),
    "github.copilot.chat.pullRequestDescriptionGeneration.instructions": ([], (list,), ""),
    "github.copilot.enable": ({}, (dict,), ""),
    "github.copilot.editor.enableCodeActions": (True, (bool,), ""),
    "github.copilot.nextEditSuggestions.enabled": (True, (bool,), ""),
    "github.copilot.nextEditSuggestions.fixes": (True, (bool,), ""),
    "github.copilot.renameSuggestions.triggerAutomatically": (True, (bool,), ""),
    "imageCarousel.chat.enabled": (False, (bool,), "experimental"),
    "workbench.settings.showAISearchToggle": (True, (bool,), ""),
    "chat.defaultModel": ("", (str,), ""),
    "chat.defaultConfiguration": ({}, (dict,), ""),
    "chat.useAgentSkills": (True, (bool,), ""),
    "chat.plugins.enabledPlugins": (None, (dict, type(None)), ""),
    "chat.plugins.extraMarketplaces": ({}, (dict,), ""),
    "chat.customizations.strictPluginOnlyCustomization": (False, (bool,), ""),
    "chat.customizations.marketplace.enabled": (False, (bool,), "experimental"),
    "chat.customizations.migration.enabled": (True, (bool,), "experimental"),
    "chat.artifacts.enabled": (False, (bool,), "experimental"),
    "chat.agentHost.otel.captureIdentity": (False, (bool,), ""),
    "chat.agentHost.debugLogs.defaultExportLocation": ("", (str,), ""),
    "chat.agentHost.byokModels.enabled": (True, (bool,), "experimental"),
    "chat.agentHost.allowSignedOutWhenUsable": (False, (bool,), "experimental"),
    "chat.copilot.memory.enabled": (False, (bool,), "preview"),
    "chat.copilot.localModel.enabled": (False, (bool,), "experimental"),
    "chat.copilot.skillCharBudget": (30000, (int,), "experimental"),
    "github.copilot.chat.otel.enabled": (False, (bool,), ""),
    "github.copilot.chat.otel.exporterType": ("otlp-http", (str,), "otlp-grpc | otlp-http | console | file"),
    "github.copilot.chat.otel.otlpEndpoint": ("http://localhost:4318", (str,), ""),
    "github.copilot.chat.otel.captureContent": (False, (bool,), ""),
    "github.copilot.chat.otel.captureIdentity": (None, (bool, type(None)), ""),
    "github.copilot.chat.otel.serviceName": ("", (str,), ""),
    "github.copilot.chat.otel.headers": ({}, (dict,), ""),
    "github.copilot.chat.otel.resourceAttributes": ({}, (dict,), ""),
    "github.copilot.chat.otel.dbSpanExporter.enabled": (False, (bool,), ""),
    "github.copilot.chat.otel.outfile": ("", (str,), ""),
    "github.copilot.chat.cloudAgent.enabled": (True, (bool,), ""),
    "github.copilot.chat.organizationCustomAgents.enabled": (True, (bool,), ""),
    "github.copilot.chat.codeGeneration.useInstructionFiles": (True, (bool,), ""),
    "github.copilot.chat.customInstructionsInSystemMessage": (True, (bool,), ""),
    "github.copilot.chat.imageUpload.enabled": (True, (bool,), ""),
    "github.copilot.chat.reviewAgent.enabled": (True, (bool,), ""),
    "github.copilot.chat.reviewSelection.enabled": (True, (bool,), ""),
    "github.copilot.chat.scopeSelection": (False, (bool,), ""),
    "github.copilot.chat.localIndex.enabled": (True, (bool,), ""),
    "github.copilot.chat.modelCapabilityOverrides": ({}, (dict,), "advanced"),
    "github.copilot.chat.reasoningEffortOverride": (None, (str, type(None)), "advanced"),
    "github.copilot.chat.rateLimitAutoSwitchToAuto": (False, (bool,), ""),
    "github.copilot.chat.chatCompletionsTokenParameter": ("max_tokens", (str,), "max_completion_tokens | max_tokens"),
    "github.copilot.chat.cli.terminalLinks.enabled": (True, (bool,), ""),
    "github.copilot.chat.cli.mcp.enabled": (True, (bool,), "deprecated"),
    "github.copilot.chat.agentDebugLog.fileLogging.enabled": (False, (bool,), "experimental"),
    "github.copilot.chat.copilotDebugCommand.enabled": (True, (bool,), "preview"),
}

DEPRECATED = {
    "chat.modeFilesLocations": "chat.agentFilesLocations",
    "chat.promptFilesLocations": "skills, or profile prompt storage",
    "chat.agent.sandbox.fileSystem.linux": "chat.agent.sandbox.fileSystem.userConfiguredPaths",
    "chat.agent.sandbox.fileSystem.mac": "chat.agent.sandbox.fileSystem.userConfiguredPaths",
    "chat.agent.sandbox.fileSystem.windows": "chat.agent.sandbox.fileSystem.userConfiguredPaths",
    "chat.agent.sandbox.advanced.runtime": "no replacement; remove it",
    "chat.agent.sandbox.retryWithAllowNetworkRequests": "configure the network settings directly",
    "chat.tools.terminal.shellIntegrationTimeout": "terminal.integrated.shellIntegration.timeout",
    "chat.tools.terminal.backgroundNotifications": "no replacement; remove it",
    "chat.experimental.detectParticipant.enabled": "chat.detectParticipant.enabled",
    "github.copilot.chat.anthropic.thinking.effort": "model picker thinking effort",
    "github.copilot.chat.responsesApiReasoningEffort": "model picker thinking effort",
    "github.copilot.chat.codeGeneration.instructions": "file-based instructions (*.instructions.md)",
    "github.copilot.chat.testGeneration.instructions": "file-based instructions (*.instructions.md)",
}

ENUMS = {
    "chat.requestQueuing.defaultAction": {"queue", "steer"},
    "chat.notifyWindowOnResponseReceived": {"off", "windowNotFocused", "always"},
    "chat.notifyWindowOnConfirmation": {"off", "windowNotFocused", "always"},
    "chat.tools.terminal.outputLocation": {"chat", "terminal"},
    "chat.tools.terminal.blockDetectedFileWrites": {"never", "outsideWorkspace", "all"},
    "chat.agent.sandbox.enabled": {"off", "on"},
    "chat.mcp.access": {"none", "registry", "all"},
    "chat.mcp.autostart": {"never", "onlyNew", "newAndOutdated"},
    "chat.mcp.collisionBehavior": {"disable", "suffix"},
    "chat.experimental.persistentProgress": {"off", "draw", "drawMonochrome", "drawMonochromeNoIcon"},
    "chat.experimental.persistentProgressVerbosity": {"compact", "verbose"},
    "github.copilot.chat.otel.exporterType": {"otlp-grpc", "otlp-http", "console", "file"},
}


def strip_jsonc(text: str) -> str:
    out = []
    i, n = 0, len(text)
    in_str = False
    while i < n:
        ch = text[i]
        if in_str:
            out.append(ch)
            if ch == "\\" and i + 1 < n:
                out.append(text[i + 1])
                i += 2
                continue
            if ch == '"':
                in_str = False
            i += 1
            continue
        if ch == '"':
            in_str = True
            out.append(ch)
            i += 1
            continue
        if ch == "/" and i + 1 < n and text[i + 1] == "/":
            while i < n and text[i] != "\n":
                i += 1
            continue
        if ch == "/" and i + 1 < n and text[i + 1] == "*":
            i += 2
            while i + 1 < n and not (text[i] == "*" and text[i + 1] == "/"):
                i += 1
            i += 2
            continue
        out.append(ch)
        i += 1
    cleaned = "".join(out)
    # remove trailing commas before } or ]
    return re.sub(r",(\s*[}\]])", r"\1", cleaned)


def is_ai_key(key: str) -> bool:
    return (key.startswith("chat.") or key.startswith("github.copilot")
            or key.startswith("imageCarousel.chat") or key in CATALOG)


def type_name(spec: tuple[type, ...]) -> str:
    return " | ".join({getattr(t, "__name__", str(t)) for t in spec})


def check(path: str) -> int:
    if not os.path.exists(path):
        print("missing settings file: " + path)
        return 2
    raw = open(path, encoding="utf-8-sig").read()
    try:
        data = json.loads(strip_jsonc(raw))
    except Exception as exc:
        print("could not parse " + path + ": " + str(exc))
        return 2
    if not isinstance(data, dict):
        print("settings root is not an object")
        return 2

    unknown, deprecated, badtype, badenum = [], [], [], []

    for key, value in data.items():
        if key in DEPRECATED:
            deprecated.append((key, value, DEPRECATED[key]))
            continue
        if not is_ai_key(key):
            continue
        if key not in CATALOG:
            unknown.append((key, value))
            continue
        default, types, _ = CATALOG[key]
        if isinstance(value, bool) and bool not in types:
            badtype.append((key, value, type_name(types)))
        elif not isinstance(value, types) or (isinstance(value, bool) and bool not in types):
            badtype.append((key, value, type_name(types)))
        if key in ENUMS and isinstance(value, str) and value not in ENUMS[key]:
            badenum.append((key, value, sorted(ENUMS[key])))

    print("checked " + path)
    print("AI settings found: " + str(sum(1 for k in data if is_ai_key(k))))

    if badtype:
        print("\nWRONG TYPE")
        for k, v, want in badtype:
            print("  %s = %r  (expected %s)" % (k, v, want))
    if badenum:
        print("\nINVALID CHOICE")
        for k, v, allowed in badenum:
            print("  %s = %r  (allowed: %s)" % (k, v, ", ".join(allowed)))
    if deprecated:
        print("\nDEPRECATED")
        for k, v, repl in deprecated:
            print("  %s = %r  -> use %s" % (k, v, repl))
    if unknown:
        print("\nUNKNOWN AI SETTING (verify the name; it may be misspelled or removed)")
        for k, v in unknown:
            print("  %s = %r" % (k, v))

    if not (badtype or badenum or deprecated or unknown):
        print("\nno problems found")

    # High-value combinations worth flagging
    notes = []
    if data.get("chat.agent.sandbox.enabled") in ("on", True):
        if os.name == "nt" and not data.get("chat.agent.sandbox.network.allowLocalNetwork", False):
            notes.append("sandbox is on and this is Windows: set "
                         "chat.agent.sandbox.network.allowLocalNetwork=true or sandboxed "
                         "commands needing the proxy will fail to start")
        if data.get("chat.tools.terminal.enableAutoApprove") is False:
            notes.append("sandbox is on and auto-approve is disabled: expect a prompt "
                         "for nearly every command")
    if data.get("chat.useNestedAgentsMdFiles") and not data.get("chat.useAgentsMdFile", True):
        notes.append("chat.useNestedAgentsMdFiles has no effect while "
                     "chat.useAgentsMdFile is false")
    if data.get("chat.instructionsFilesLocations"):
        notes.append("chat.instructionsFilesLocations is supported but other Copilot "
                     "clients do not inherit it; prefer ~/.copilot/instructions")
    if data.get("chat.promptFilesRecommendations") and "chat.promptFilesLocations" in data:
        notes.append("chat.promptFilesLocations is deprecated; prompt files live in "
                     "profile storage")
    if notes:
        print("\nADVISORY")
        for note in notes:
            print("  - " + note)

    return 1 if (badtype or badenum) else 0


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--settings", default=os.path.join(
        os.environ.get("APPDATA", ""), "Code", "User", "settings.json"))
    args = p.parse_args()
    return check(args.settings)


if __name__ == "__main__":
    raise SystemExit(main())
