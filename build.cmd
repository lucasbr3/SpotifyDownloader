@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" -Configuration Release %*
if %errorlevel% neq 0 exit /b %errorlevel%
echo.
echo Build complete. Run installer or copy Release folder.
