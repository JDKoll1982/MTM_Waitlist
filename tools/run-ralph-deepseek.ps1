# Run the Spec Kit Ralph loop against DeepSeek, using the Copilot CLI's BYOK (bring-your-own-model) mode.
#
# WHY THIS EXISTS
#   DeepSeek V4.1 Flash reaches VS Code Copilot Chat through the `vizards.deepseek-v4-for-copilot` extension.
#   That extension is a CHAT model provider only: it registers nothing with the Copilot CLI, and the CLI
#   validates `--model` against its own server-side catalog, which contains no DeepSeek entry — so
#   `--model "DeepSeek V4.1 Flash"` is rejected outright (verified 2026-09-20 across four spellings).
#
#   The CLI does support a custom OpenAI-compatible provider through environment variables. Also verified
#   2026-09-20, with an intentionally invalid key: the CLI ACCEPTED `deepseek-v4-flash` and reached
#   https://api.deepseek.com, failing only with HTTP 401. So the endpoint and model name are right, and the
#   only thing this script needs from a person is a valid DeepSeek API key.
#
# THE KEY IS YOURS TO SUPPLY. This script never prints it and never writes it anywhere:
#   * set the DEEPSEEK_API_KEY environment variable, or
#   * put the key on a single line in -KeyFile (default %USERPROFILE%\.deepseek\api-key.txt, outside the repo)
#
# USAGE
#   ./tools/run-ralph-deepseek.ps1 -MaxIterations 70
#
# The provider is probed with one throwaway turn BEFORE the loop starts, so a wrong key or a rejected model
# costs one request instead of seventy failed iterations.
#
# CAVEATS, stated rather than discovered later
#   * BYOK bypasses GitHub authentication and bills DeepSeek directly for every iteration.
#   * `deepseek-v4-flash` is not a "well-known" id to the CLI, so its token limits are NOT taken from a
#     built-in catalog and fall back to defaults. Pass -MaxPromptTokens / -MaxOutputTokens to set them.
#   * Whether the CLI's agent loop (tool calling across many turns) behaves as well on this endpoint as on a
#     catalog model is not something this script can prove. The probe shows the turn succeeds; it does not
#     show seventy iterations will.

[CmdletBinding()]
param(
    [int]$MaxIterations = 70,
    [string]$Model = 'deepseek-v4-flash',
    [string]$BaseUrl = 'https://api.deepseek.com',
    [string]$KeyFile = (Join-Path $env:USERPROFILE '.deepseek\api-key.txt'),
    [string]$FeatureName = '004-unified-card-item-picker',
    [string]$TasksPath = 'specs/004-unified-card-item-picker/tasks.md',
    [string]$SpecDir = 'specs/004-unified-card-item-picker',
    [int]$MaxPromptTokens = 0,
    [int]$MaxOutputTokens = 0
)

$ErrorActionPreference = 'Stop'
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force

# Resolve every path against the repository root (the folder containing this script's folder) so the launcher works
# from any working directory, not only from the repo root. Before this, running it by a bare name or from elsewhere
# resolved the loop script and tasks.md relative to wherever the shell happened to be, which failed confusingly.
$repoRoot   = Split-Path -Parent $PSScriptRoot
$loopScript = Join-Path $repoRoot '.specify/extensions/ralph/scripts/powershell/ralph-loop.ps1'
if (-not [System.IO.Path]::IsPathRooted($TasksPath)) { $TasksPath = Join-Path $repoRoot $TasksPath }
if (-not [System.IO.Path]::IsPathRooted($SpecDir)) { $SpecDir = Join-Path $repoRoot $SpecDir }

if (-not (Test-Path $loopScript)) {
    Write-Host "The Ralph loop script is not where this launcher expects it:" -ForegroundColor Red
    Write-Host "  $loopScript" -ForegroundColor Red
    Write-Host 'Install the ralph extension for this repository, or pass the correct repository root, then re-run.' -ForegroundColor Red
    exit 6
}

function Get-DeepSeekKey {
    param([string]$Path)

    if ($env:DEEPSEEK_API_KEY) { return $env:DEEPSEEK_API_KEY.Trim() }

    if (Test-Path $Path) {
        $fromFile = (Get-Content -Path $Path -TotalCount 1).Trim()
        if ($fromFile) { return $fromFile }
    }

    Write-Host ''
    Write-Host 'No DeepSeek API key found.' -ForegroundColor Red
    Write-Host ''
    Write-Host 'Supply it one of these two ways, then re-run:'
    Write-Host "  1. set the environment variable:  `$env:DEEPSEEK_API_KEY = 'sk-...'"
    Write-Host "  2. write it to a one-line file:    $Path"
    Write-Host ''
    Write-Host 'A key is issued at https://platform.deepseek.com. The existing extension already has one;'
    Write-Host 'VS Code command "DeepSeek: Get API Key" displays it if you need to copy it across.'
    Write-Host ''
    exit 2
}

$key = Get-DeepSeekKey -Path $KeyFile

$env:COPILOT_PROVIDER_BASE_URL = $BaseUrl
$env:COPILOT_PROVIDER_TYPE = 'openai'
$env:COPILOT_PROVIDER_API_KEY = $key
$env:COPILOT_MODEL = $Model
if ($MaxPromptTokens -gt 0) { $env:COPILOT_PROVIDER_MAX_PROMPT_TOKENS = "$MaxPromptTokens" }
if ($MaxOutputTokens -gt 0) { $env:COPILOT_PROVIDER_MAX_OUTPUT_TOKENS = "$MaxOutputTokens" }

Write-Host "Provider : $BaseUrl (openai-compatible, BYOK)" -ForegroundColor Cyan
Write-Host "Model    : $Model" -ForegroundColor Cyan
Write-Host 'Probing the provider with one throwaway turn...' -ForegroundColor Cyan

$probe = (& copilot -p 'Reply with the single word OK' --model $Model -s --allow-all-tools 2>&1 | Out-String)

if ($probe -match 'Authentication failed|HTTP 401|HTTP 403|invalid_api_key|Insufficient Balance') {
    Write-Host 'The provider rejected the request:' -ForegroundColor Red
    ($probe.Trim() -split "`n" | Select-Object -First 4) | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host 'The key is wrong, revoked, or out of credit. Nothing was launched.' -ForegroundColor Red
    exit 3
}

if ($probe -match 'is not available') {
    Write-Host "The CLI would not accept the model name '$Model' even with BYOK active:" -ForegroundColor Red
    ($probe.Trim() -split "`n" | Select-Object -First 4) | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host 'Nothing was launched.' -ForegroundColor Red
    exit 4
}

if ([string]::IsNullOrWhiteSpace($probe)) {
    Write-Host 'The probe returned nothing, which is not a success. Nothing was launched.' -ForegroundColor Red
    exit 5
}

Write-Host 'Probe succeeded - the model responds.' -ForegroundColor Green
Write-Host ''
Write-Host "Starting the Ralph loop: $MaxIterations iteration(s) over $FeatureName" -ForegroundColor Cyan
Write-Host ''

& $loopScript `
    -FeatureName $FeatureName `
    -TasksPath $TasksPath `
    -SpecDir $SpecDir `
    -MaxIterations $MaxIterations `
    -Model $Model `
    -AgentCli 'copilot' `
    -DetailedOutput
