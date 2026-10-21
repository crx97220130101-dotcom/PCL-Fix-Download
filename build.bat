@echo off
setlocal DisableDelayedExpansion
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
set "result=%errorlevel%"
echo.
pause
exit /b %result%
