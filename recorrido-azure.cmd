@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\recorrido-azure.ps1" %*
exit /b %errorlevel%
