# Capture the debug output channel that XAML binding failures are reported on.
#
# WHY THIS EXISTS
#   A binding that cannot resolve - the card binding `Cells[3]` on a row that filled two cells, say - does not
#   throw, does not change what is drawn, and does not reach the console logger. It is written to the debug
#   output channel, which is why the failures only ever appeared in the editor's debug console while the app was
#   running under F5. That made them invisible to the test suite and impossible to check from a script, so a fix
#   for them could not be shown to work.
#
#   This listens on the same channel the debugger does (the shared DBWIN_BUFFER the platform writes
#   OutputDebugString to) and writes every line to a file with the id of the process that wrote it. Run it
#   before the app, drive the app, then read the file.
#
# USAGE
#   # in one session - capture for two minutes
#   pwsh -File tools/capture-debug-output.ps1 -OutFile "$env:TEMP\dbg.tsv" -Seconds 120
#
#   # in another - prove the channel is live, which a capture that found nothing cannot do on its own
#   pwsh -File tools/capture-debug-output.ps1 -Emit 'probe'
#
#   # then, with the app closed
#   pwsh -File tools/capture-debug-output.ps1 -Summarise "$env:TEMP\dbg.tsv"
#
#   A capture that reports zero binding failures is only evidence if the channel is known to be live, so
#   -Emit exists to check it: emit first, confirm the line arrived, then capture the app.

[CmdletBinding(DefaultParameterSetName = 'Capture')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Capture', Position = 0)]
    [string]$OutFile,

    [Parameter(ParameterSetName = 'Capture')]
    [int]$Seconds = 120,

    # Keep only lines written by this process. 0 keeps every process, which is what you want when the app's id is
    # not known until it starts.
    [Parameter(ParameterSetName = 'Capture')]
    [int]$OnlyProcessId = 0,

    # Write one line to the channel and exit, so a capture can be checked before it is trusted.
    [Parameter(Mandatory = $true, ParameterSetName = 'Emit')]
    [string]$Emit,

    # Read a finished capture back and report what is in it.
    [Parameter(Mandatory = $true, ParameterSetName = 'Summarise')]
    [string]$Summarise
)

$ErrorActionPreference = 'Stop'

# The channel is anonymous shared memory plus two events, as the platform's debug-output protocol defines them.
# The buffer is 4096 bytes: a 4-byte process id and an ANSI string.
$BufferName = 'DBWIN_BUFFER'
$BufferReadyName = 'DBWIN_BUFFER_READY'
$DataReadyName = 'DBWIN_DATA_READY'
$BufferSize = 4096

$EmitSignature = @'
using System;
using System.Runtime.InteropServices;

public static class DebugOutputProbe
{
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern void OutputDebugStringA(string text);

    public static void Write(string text) => OutputDebugStringA(text);
}
'@

function Write-ToDebugChannel {
    param([string]$Text)

    if (-not ('DebugOutputProbe' -as [type])) {
        Add-Type -TypeDefinition $EmitSignature -Language CSharp
    }

    [DebugOutputProbe]::Write($Text)
}

function Get-CapturedLineCount {
    param([string]$Path)

    if (-not (Test-Path $Path)) { return 0 }

    @(Get-Content $Path | Where-Object { $_ -ne '' }).Count
}

if ($PSCmdlet.ParameterSetName -eq 'Emit') {
    Write-ToDebugChannel -Text $Emit
    Write-Host "emitted   : $Emit" -ForegroundColor Green
    return
}

if ($PSCmdlet.ParameterSetName -eq 'Summarise') {
    if (-not (Test-Path $Summarise)) { throw "No capture at $Summarise." }

    $lines = @(Get-Content $Summarise | Where-Object { $_ -ne '' })
    $binding = @($lines | Where-Object { $_ -match 'Failed to connect|BindingExpression|Cannot find a resource' })
    $failures = @($lines | Where-Object { $_ -match '\bError:' })

    Write-Host ''
    Write-Host "capture   : $Summarise"
    Write-Host "lines     : $($lines.Count)"
    Write-Host "errors    : $($failures.Count)"
    Write-Host "binding   : $($binding.Count)"

    foreach ($line in ($binding | Select-Object -First 10)) {
        $text = $line.Substring([Math]::Min($line.IndexOf("`t") + 1, $line.Length))
        Write-Host "   $($text.Substring(0, [Math]::Min(180, $text.Length)))" -ForegroundColor Yellow
    }

    if ($lines.Count -eq 0) {
        Write-Host 'nothing was captured - the channel was not live, so this proves nothing' -ForegroundColor Red
    }

    return
}

# The debugger's end of the protocol. BufferReady starts signalled so the first writer can proceed; the writer
# then waits for DataReady, which this sets after each line is taken.
$bufferReady = New-Object System.Threading.EventWaitHandle(
    $true, [System.Threading.EventResetMode]::AutoReset, $BufferReadyName)

$dataReady = New-Object System.Threading.EventWaitHandle(
    $false, [System.Threading.EventResetMode]::AutoReset, $DataReadyName)

$mapped = [System.IO.MemoryMappedFiles.MemoryMappedFile]::CreateOrOpen(
    $BufferName, $BufferSize, [System.IO.MemoryMappedFiles.MemoryMappedFileAccess]::ReadWrite)

$view = $mapped.CreateViewAccessor(0, $BufferSize, [System.IO.MemoryMappedFiles.MemoryMappedFileAccess]::ReadWrite)

# Latin-1 maps every byte to one character, which is what the channel's ANSI text needs: no byte is dropped or
# folded, so a line is reported exactly as it was written.
$encoding = [System.Text.Encoding]::Latin1

$writer = [System.IO.StreamWriter]::new($OutFile, $false, [System.Text.UTF8Encoding]::new($false))
$writer.AutoFlush = $true

$bytes = New-Object byte[] ($BufferSize - 4)
$kept = 0
$seen = 0
$deadline = (Get-Date).AddSeconds($Seconds)

Write-Host "listening : debug output channel for $Seconds s -> $OutFile" -ForegroundColor Green

try {
    while ((Get-Date) -lt $deadline) {
        if (-not $dataReady.WaitOne(500)) { continue }

        $writerId = $view.ReadInt32(0)
        [void]$view.ReadArray(4, $bytes, 0, $bytes.Length)

        $end = [Array]::IndexOf($bytes, [byte]0)

        if ($end -lt 0) { $end = $bytes.Length }

        $text = $encoding.GetString($bytes, 0, $end)

        # Release the writer before doing anything with the line, or the platform stalls while this parses.
        [void]$bufferReady.Set()

        $seen++

        if ($OnlyProcessId -ne 0 -and $writerId -ne $OnlyProcessId) { continue }

        [void]$writer.WriteLine("$writerId`t$text")
        $kept++
    }
}
finally {
    $writer.Dispose()
    $view.Dispose()
    $mapped.Dispose()
    $dataReady.Dispose()
    $bufferReady.Dispose()
}

Write-Host "captured  : $kept line(s) kept of $seen seen" -ForegroundColor Green
