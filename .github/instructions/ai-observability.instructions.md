---
name: 'AI observability and debugging'
description: 'Inspect and export what the agent actually did: agent debug logs, session logs, OpenTelemetry traces and metrics, and the CLI diagnose commands. Use when a run behaved unexpectedly, when you need cost or latency evidence, or when wiring Copilot telemetry into an OTLP backend.'
---
# AI observability and debugging

Verified on VS Code 1.143.1 and Copilot CLI 1.0.98.

Two different questions, two different tools:

| Question | Use |
| --- | --- |
| "What did the agent do in this run?" | Agent debug logs, session logs, `/diagnose` |
| "How long / how much did it cost, at scale?" | OpenTelemetry via OTLP |

## Where the evidence lives

| Source | Location / command |
| --- | --- |
| VS Code agent debug logs | `chat.agentHost.debugLogs.defaultExportLocation` (default `""`) picks the export folder |
| Extension-host file logging | `github.copilot.chat.agentDebugLog.fileLogging.enabled` (default `false`, experimental) |
| Retained session logs | `...fileLogging.maxRetainedSessionLogs` (default `50`), `...maxSessionLogSizeMB` (default `100`) |
| CLI logs | `--log-dir` (default `~/.copilot/logs/`), `--log-level none\|error\|warning\|info\|debug\|all` |
| CLI session analysis | `/diagnose` in a session, optionally with a custom prompt |
| CLI debug bundle | `/collect-debug-logs` produces a local `.tgz` |
| CLI environment dump | `/env` lists instructions, MCP servers, skills, agents, hooks, plugins, LSPs, extensions |

Read the log before theorising. In this environment the Bash/PowerShell sandbox denials, unresolved
MCP placeholders, and discovery gaps were all visible in the log or a discovery command first.

## OpenTelemetry

VS Code can emit Copilot spans and metrics over OTLP. Off by default.

| Setting | Default | Notes |
| --- | --- | --- |
| `github.copilot.chat.otel.enabled` | `false` | Master switch |
| `github.copilot.chat.otel.exporterType` | `"otlp-http"` | `otlp-grpc` \| `otlp-http` \| `console` \| `file` |
| `github.copilot.chat.otel.otlpEndpoint` | `"http://localhost:4318"` | Collector endpoint |
| `github.copilot.chat.otel.protocol` | `""` | `http/json` \| `http/protobuf` \| `grpc` |
| `github.copilot.chat.otel.serviceName` | `""` | Set this, or traces are hard to attribute |
| `github.copilot.chat.otel.headers` | `{}` | Auth headers for a hosted backend |
| `github.copilot.chat.otel.resourceAttributes` | `{}` | Deployment/environment attributes |
| `github.copilot.chat.otel.outfile` | `""` | Used when exporter type is `file` |
| `github.copilot.chat.otel.maxAttributeSizeChars` | `0` | `0` = no truncation |
| `github.copilot.chat.otel.captureContent` | `false` | **Prompt/completion text** — leave off unless your policy allows it |
| `github.copilot.chat.otel.captureIdentity` | unset | User identity |
| `github.copilot.chat.otel.dbSpanExporter.enabled` | `false` | Local DB span export |
| `chat.agentHost.otel.captureIdentity` | `false` | Agent-host identity capture |

Local-first workflow: set `exporterType: "file"`, `outfile` to a path, and inspect the output. Use a
Collector (`localhost:4318`) only once you know you want a pipeline.

## Rules

* `captureContent` is a privacy decision, not a debugging convenience. Do not enable it to "see what
  happened" on a machine that handles other people's code.
* Correlation beats volume: set `serviceName` and a deployment attribute before enabling exporters,
  otherwise the traces are unattributable.
* `console` exporter floods the terminal. Prefer `file` for local inspection.
* Log files and exports can contain prompts and file contents. Treat them as sensitive, and do not
  paste them into a chat or commit them.
* `/diagnose` reads the session log for you — try it before writing a custom parser.
