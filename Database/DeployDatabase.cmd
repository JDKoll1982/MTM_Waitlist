@echo off
rem Starts the MTM database deployment tool: builds it for this machine, then opens the wizard.
rem This replaces the retired install_local_database.bat / .vbs pair. The tool shows what it will change
rem before it changes anything, and copies the database into {database}_backup before its first statement.
setlocal

set "REPO_ROOT=%~dp0.."
set "PROJECT=%REPO_ROOT%\MTM_Database_Deployment\MTM_Database_Deployment.csproj"
set "EXE=%REPO_ROOT%\MTM_Database_Deployment\bin\x64\Release\net10.0-windows10.0.22621.0\MTM_Database_Deployment.exe"

echo Building the database tool...
dotnet build "%PROJECT%" -c Release -p:Platform=x64 -v:minimal
if errorlevel 1 (
    echo.
    echo The tool could not be built. Show the messages above to IT.
    pause
    exit /b 1
)

if not exist "%EXE%" (
    echo.
    echo The tool was built, but it is not where this launcher expects it: %EXE%
    echo Show this message to IT.
    pause
    exit /b 1
)

start "" "%EXE%"
exit /b 0
