@echo off
rem Starts the installer. Double-click this file.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0data\install.ps1"
echo.
pause
