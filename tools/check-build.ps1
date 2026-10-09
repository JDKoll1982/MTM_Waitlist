# Builds the solution and prints only the lines that decide whether it passed.
$ErrorActionPreference = 'Continue'
$env:MSBUILDDISABLENODEREUSE = '1'

$log = '.specify\dunnage-build.txt'
& 'C:\Program Files\dotnet\dotnet.exe' build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false *> $log
$exit = $LASTEXITCODE

$lines = Get-Content -LiteralPath $log -ErrorAction SilentlyContinue

$lines |
    Where-Object { $_ -match 'error|Build succeeded|Build FAILED|Warning\(s\)|Error\(s\)' } |
    Select-Object -First 40

Write-Output "EXIT=$exit"
