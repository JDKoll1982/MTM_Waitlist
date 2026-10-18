@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0dump-wer.ps1"
echo done=%ERRORLEVEL%
