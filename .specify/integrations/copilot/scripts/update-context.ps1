# update-context.ps1 -- Copilot integration: create/update .github/copilot-instructions.md
#
# This is the copilot-specific implementation that produces the GitHub
# Copilot instructions file. The shared dispatcher reads
# .specify/integration.json and calls this script.
#
# NOTE: This script is not yet active. It will be activated in Stage 7
# when the shared update-agent-context.ps1 replaces its switch statement
# with integration.json-based dispatch. The shared script must also be
# refactored to support SPECKIT_SOURCE_ONLY (guard the Main call) before
# dot-sourcing will work.
#
# Until then, this delegates to the shared script as a subprocess.

$ErrorActionPreference = 'Stop'

# Derive repo root from script location (walks up to find .specify/)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = try { git rev-parse --show-toplevel 2>$null } catch { $null }
# If git did not return a repo root, or the git root does not contain .specify,
# fall back to walking up from the script directory to find the initialized project root.
if (-not $repoRoot -or -not (Test-Path (Join-Path $repoRoot '.specify'))) {
    $repoRoot = $scriptDir
    $fsRoot = [System.IO.Path]::GetPathRoot($repoRoot)
    while ($repoRoot -and $repoRoot -ne $fsRoot -and -not (Test-Path (Join-Path $repoRoot '.specify'))) {
        $repoRoot = Split-Path -Parent $repoRoot
    }
}

# Invoke shared update-agent-context script as a separate process.
# Dot-sourcing is unsafe until that script guards its Main call.
$dispatcher = Join-Path $repoRoot '.specify/scripts/powershell/update-agent-context.ps1'
if (-not (Test-Path -LiteralPath $dispatcher -PathType Leaf)) {
    # This script is a thin delegate; the shared implementation is installed by
    # 'specify init'. Report the missing path explicitly instead of letting the
    # call fail with an opaque "term is not recognized" message.
    [Console]::Error.WriteLine("ERROR: shared dispatcher not found: $dispatcher")
    [Console]::Error.WriteLine("Run 'specify init' to restore .specify/scripts/powershell/update-agent-context.ps1.")
    exit 1
}

& $dispatcher -AgentType copilot
exit $LASTEXITCODE
