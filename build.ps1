param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [switch]$NoInstallers
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

function Write-Step($msg) {
    Write-Host "`n=== $msg ===" -ForegroundColor Cyan
}

Write-Step "Building Spotify Downloader ($Configuration)"

# Restore
Write-Step "Restoring packages"
dotnet restore "$root\SpotifyDownloader.sln" -p:Platform=x64
if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

# Build Core
Write-Step "Building Core project"
dotnet build "$root\src\SpotifyDownloader.Core\SpotifyDownloader.Core.csproj" `
    -c $Configuration -p:Platform=x64 --no-restore
if ($LASTEXITCODE -ne 0) { throw "Core build failed" }

# Build App
Write-Step "Building App project (WinUI 3)"
dotnet build "$root\src\SpotifyDownloader.App\SpotifyDownloader.App.csproj" `
    -c $Configuration -p:Platform=x64 --no-restore
if ($LASTEXITCODE -ne 0) { throw "App build failed" }

$outputDir = "$root\bin\$Configuration\net8.0-windows10.0.19041.0\"
$releaseDir = "$root\Release"

# Create Release folder
Write-Step "Creating Release folder"
if (Test-Path $releaseDir) { Remove-Item $releaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

# Copy output files
Write-Step "Copying output files"
Copy-Item "$outputDir\*" $releaseDir -Recurse -Force -ErrorAction SilentlyContinue

# Copy FFmpeg notice
@"
FFmpeg is required for audio conversion.
Download from: https://ffmpeg.org/download.html
Place ffmpeg.exe in the same folder as SpotifyDownloader.exe or add to PATH.
"@ | Out-File "$releaseDir\FFMPEG_README.txt" -Encoding utf8

# Clean up debug symbols and unnecessary files
Remove-Item "$releaseDir\*.pdb" -Force -ErrorAction SilentlyContinue
Remove-Item "$releaseDir\*.xml" -Force -ErrorAction SilentlyContinue
Remove-Item "$releaseDir\*.config" -Force -ErrorAction SilentlyContinue

Write-Host "`n✔ Release folder created at: $releaseDir" -ForegroundColor Green

# Inno Setup installer
if (-not $NoInstallers) {
    $innoPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    if (Test-Path $innoPath) {
        Write-Step "Building Inno Setup installer"
        & $innoPath "$root\installer.iss"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✔ Inno Setup installer created" -ForegroundColor Green
        } else {
            Write-Warning "Inno Setup compilation failed (exit code: $LASTEXITCODE)"
        }
    } else {
        Write-Warning "Inno Setup not found at $innoPath. Skipping installer."
        Write-Warning "Install Inno Setup 6 from: https://jrsoftware.org/isdl.php"
    }
}

Write-Step "Done!"
Write-Host "Release files: $releaseDir" -ForegroundColor Green
Write-Host "Run SpotifyDownloader.exe to start the app." -ForegroundColor Yellow
Write-Host "Make sure FFmpeg is installed on the target machine." -ForegroundColor Yellow
