@echo off
rem =====================================================================
rem  One-click dev setup for this repository.
rem  Double-click this file (or run it from a terminal).
rem  What it does: enables the pre-commit hook and Unity SmartMerge.
rem =====================================================================
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup-dev.ps1"
echo.
pause
