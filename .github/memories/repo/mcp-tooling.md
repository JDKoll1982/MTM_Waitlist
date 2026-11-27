# MCP tooling — troubleshooting playbook (verified 2026-11-26, `johnk`)

Read this when an MCP tool is missing, reports itself disabled, or throws unexpectedly. It exists so the
fix is not re-derived from scratch each time.

## The one failure that looks like a broken server

A tool the server genuinely provides reports **"currently disabled by the user"** even though it is
checked and enabled in the Configure Tools dialog. Nothing is wrong with the server or its
configuration.

The cause is the **running session's tool snapshot**. A Copilot session fixes its tool list when it
starts, so a session that began before a server was registered or enabled keeps the old list for its
whole life and reports every tool from that server as disabled.

Fix, in order:

1. Start a **new chat session**. That is the whole fix in almost every case.
2. Confirm the server shows as connected in the MCP panel of the new session.
3. Only if it still fails, treat it as a real server fault.

There is no settings-file gate to look for. Neither user nor workspace `settings.json` carries an MCP
allowlist or blacklist, so hunting one wastes time.

## Prove the server, not the client

Before blaming a server, ask it to initialize. A healthy server answers this:

```powershell
$h = @{ "Accept" = "application/json, text/event-stream" }
Invoke-WebRequest -Uri "https://learn.microsoft.com/api/mcp" -Method Post -Headers $h `
  -ContentType "application/json" -TimeoutSec 25 `
  -Body '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"probe","version":"1.0"}}}'
```

The `Accept` header is required. Streamable HTTP answers `406 Not Acceptable` without it, which looks
like a server fault and is not.

Meanwhile, do the lookup anyway. "Disabled" is not licence to answer from memory; fetch the same page
another way and say which source was used.

## What this repository registers

Two documentation servers, declared in `.mcp.json` by `024-dependency-modernization` (AI-003) so every clone
has them: `microsoft-learn` over HTTP at `https://learn.microsoft.com/api/mcp`, and `context7` over stdio
through `npx @upstash/context7-mcp`. The file records their prerequisites beside them — Node.js for `context7`,
outbound HTTPS to `learn.microsoft.com`, `registry.npmjs.org` and the Context7 service, and the fact that
neither server reads this repository. Both were probed on 2026-11-27 and answered a documentation query.

`.vscode/mcp.json` is gone. Microsoft Learn, Context7 and Playwright are also registered at user scope in
`%APPDATA%\Code\User\mcp.json` and `%USERPROFILE%\.copilot\mcp-config.json`, which is why a session can have a
working Learn tool before the repository declaration is loaded.

Two servers were retired on 2026-11-26, for different reasons:

| Server | Why it is gone | When it comes back |
| --- | --- | --- |
| `csv-mcp-server` 1.0.0 | `load_csv` fails on the repository's own schema exports (a progress notification sends text where a number is expected), and 1.0.0 is the only release. Read the exports with `Import-Csv -Delimiter "`t"` instead. | Only if a fixed release ships, together with a working `load_csv` on a real `docs/database` file |
| `xamlmcp` 1.0.0-preview.3 | The WinUI in-process agent it attaches to is absent, so it has nothing to serve and costs ~15 tool definitions per request | In the same change that adds the `XamlMcp.WinUI` package reference and its `#if DEBUG` attach |

Do not re-register either one on its own. A registration without the capability behind it is the exact
defect this note was written to stop.

## The NuGet MCP server cannot analyse this solution

Recorded so the dead end is not walked twice. Every one of the NuGet MCP server's solution-analysis
tools fails on this repository while it evaluates the target framework:

```text
The expression "[MSBuild]::GetTargetFrameworkIdentifier(net10.0-windows10.0.19041.0)" cannot be evaluated.
Could not load file or assembly 'NuGet.Frameworks, Version=7.9.0.0, Culture=neutral,
PublicKeyToken=31bf3856ad364e35'. The located assembly's manifest definition does not match the assembly
reference. (0x80131040)
  C:\Program Files\dotnet\sdk\10.0.404\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.TargetFrameworkInference.targets
```

Re-confirmed on 2026-11-26 against SDK `10.0.404`. The server's single-package lookup still works
(`get_latest_package_version` answers), so a plain "latest version of X" question is not affected; it is the
project and solution analysis that fails.

Use the SDK's own command instead. It works per project, and it is the evidence every version figure in this
repository's audit trail comes from:

```powershell
dotnet list MTM_Waitlist\MTM_Waitlist.csproj package --outdated
dotnet list MTM_Waitlist\MTM_Waitlist.csproj package --vulnerable --include-transitive
```

Run it per project. During the audit, solution scope (`dotnet list MTM_Waitlist.sln package --outdated`)
failed with "Sequence contains no matching element"; on 2026-11-26 it returned results, so it is not broken
so much as unreliable, and a per-project run is the form that can be trusted and compared. Package pages on
the nuget.org flat container answer anything the command does not.

## Related

- `.github/instructions/mcp-doc-research.instructions.md` — where the doc servers live and how to check
  the tool names.
- `.github/memories/repo/tool-budget.md` — the 2026-10-11 tool-group decisions.
- `.github/memories/repo/ui-automation-drive.md` — what actually works for driving the app window today.
