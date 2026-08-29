@echo off
rem Launch the MTM Waitlist local database installer.
rem wscript.exe is used (not cscript) because the VBS script uses InputBox/MsgBox dialogs.

wscript.exe "%~dp0install_local_database.vbs"

if errorlevel 1 (
    echo.
    echo Installer exited with error code %errorlevel%.
    pause
)
