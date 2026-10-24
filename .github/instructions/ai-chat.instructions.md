---
name: 'Chat mechanics'
description: 'Where to chat in VS Code, how to add context, steer a running request, review changes, and which settings change chat behavior. Use for questions about the chat UI, slash commands, context references, or chat display/notification settings.'
applyTo: '**'
---
# Chat in VS Code

Verified on VS Code 1.143.1.

## Pick the right surface

| Surface | Use it for |
| --- | --- |
| Chat view | Multi-turn work, agent mode, reviewing diffs |
| Inline chat | A change scoped to the current selection |
| Quick chat | A one-off question without leaving the editor |
| Terminal chat | Asking about terminal output or generating a command |
| Agents window | Launching and monitoring long-running agent sessions |

Open these from the Command Palette (`Ctrl+Shift+P`) using the `Chat: ...` commands, so the binding always matches the current build. Do not hardcode a shortcut you have not confirmed.

## Add context explicitly

* Type `#` in the input to reference explicit context: `#file`, a folder, a symbol, `#codebase`, `#terminalSelection`, and tool references such as `#fetch`.
* Implicit context (active file, selection) depends on the surface, the harness, and the context settings. Never assume an open file was sent — attach it.
* Images and screenshots can be attached as context; the integrated browser can contribute HTML, CSS, and element context.
* Prefer a few precise references over bulk-adding many files.

## Steer while a request is running

* Sending mid-request either **queues** the message or **steers** the in-flight request. Controlled by `chat.requestQueuing.defaultAction` — the default is `"steer"`, which hands the message to the provider immediately and cannot be replaced or cancelled.
* Queued messages can be reordered before they are processed.
* Use steering to correct direction early; use queueing (`"queue"`) to stack follow-ups without disturbing the current turn.

## Review before accepting

* Every edit arrives as a reviewable diff. Open changed files from the response rather than trusting the summary.
* Restore a checkpoint to roll back a turn instead of hand-reverting files.
* Report what you actually ran to validate a change; "looks correct" is not validation.

## Slash commands

Type `/` to list everything currently available — built-ins plus anything contributed by skills, agents, and extensions. Verified examples: `/explain`, `/init`, `/create-instructions`, `/prompts`.

Availability is contextual: a command only appears when the active harness or agent supports it. If a documented command is missing, check the selected harness before assuming a bug.

## Settings that change chat behavior

| Setting | Default | Effect |
| --- | --- | --- |
| `chat.commandCenter.enabled` | `true` | Chat entry point in the title bar |
| `chat.disableAIFeatures` | `false` | Disable chat and inline suggestions entirely |
| `chat.verbose` | `true` | Request/completion timestamps |
| `chat.math.enabled` | `false` | KaTeX math rendering |
| `chat.stickyScroll.enabled` | `true` | Pin the current prompt while scrolling |
| `chat.viewTitle.enabled` | `true` | Session title in the chat header |
| `chat.notifyWindowOnResponseReceived` | `"windowNotFocused"` | OS notification when a reply arrives |
| `chat.notifyWindowOnConfirmation` | `"windowNotFocused"` | OS notification when input is needed |
| `chat.requestQueuing.defaultAction` | `"steer"` | `queue` vs `steer` (steer is the default) |
| `github.copilot.chat.localeOverride` | `"auto"` | Force response language |
| `imageCarousel.chat.enabled` | `false` | Media carousel for tool-result images |
| `chat.contextUsage.enabled` | `true` | Context-window usage indicator |
| `chat.checkpoints.enabled` | `true` | Restore a turn's checkpoint |
| `chat.implicitContext.suggestedContext` | `true` | Suggest attaching the active file |
| `chat.editor.wordWrap` | `"off"` | Wrap code blocks in chat |
| `chat.math.enabled` | `true` | KaTeX math rendering |

## Ground rules

* Verify a setting exists before claiming it does. Settings editor → search `chat.` — the full list is the AI settings reference.
* Custom instructions are **not** applied to inline ghost-text suggestions.
* Prefer a fresh, focused session over letting one thread grow indefinitely.
* When a claim depends on a specific build, state the build.
