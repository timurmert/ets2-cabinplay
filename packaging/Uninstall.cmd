@echo off
rem Removes CabinPlay. Double-click this file.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0data\install.ps1" -Uninstall
echo.
pause
