@echo off
chcp 65001 >nul
echo ========================================
echo  Spotify Downloader - Build Script
echo ========================================
echo.

echo [1/4] Restoring packages...
dotnet restore SpotifyDownloader.sln -p:Platform=x64
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo [2/4] Building Core...
dotnet build src\SpotifyDownloader.Core\SpotifyDownloader.Core.csproj -c Release -p:Platform=x64 --no-restore
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo [3/4] Building App (WinUI 3)...
dotnet build src\SpotifyDownloader.App\SpotifyDownloader.App.csproj -c Release -p:Platform=x64 --no-restore
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo [4/4] Creating Release folder...
if exist Release rmdir /s /q Release
mkdir Release >nul

xcopy /e /i /y "bin\Release\net8.0-windows10.0.19041.0\*" Release\

echo.
echo ========================================
echo  Build complete!
echo  Release folder: %CD%\Release
echo ========================================
pause
