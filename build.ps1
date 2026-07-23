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

# Find output directory
$baseOutput = "$root\src\SpotifyDownloader.App\bin\x64\$Configuration"
$outputDir = Get-ChildItem -Path $baseOutput -Directory | Where-Object { $_.Name -like "net8.0-windows*" } | Select-Object -First 1 -ExpandProperty FullName
if (-not $outputDir) {
    throw "Build output directory not found in $baseOutput"
}

$releaseDir = "$root\Release"

# Create Release folder
Write-Step "Creating Release folder"
if (Test-Path $releaseDir) { Remove-Item $releaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

# Copy output files
Write-Step "Copying output files"
Copy-Item "$outputDir\*" $releaseDir -Recurse -Force -ErrorAction SilentlyContinue

# Copy assets
if (Test-Path "$root\assets\icon.ico") {
    Copy-Item "$root\assets\icon.ico" "$releaseDir\Assets\" -Force -ErrorAction SilentlyContinue
}

# Clean up debug symbols and unnecessary files
Remove-Item "$releaseDir\*.pdb" -Force -ErrorAction SilentlyContinue
Remove-Item "$releaseDir\*.xml" -Force -ErrorAction SilentlyContinue
Remove-Item "$releaseDir\*.config" -Force -ErrorAction SilentlyContinue
Remove-Item "$releaseDir\*.manifest" -Force -ErrorAction SilentlyContinue

# Create version file
"1.0.0" | Out-File "$releaseDir\version.txt" -Encoding utf8

Write-Host "`n✔ Release folder created at: $releaseDir" -ForegroundColor Green
Write-Host "  Size: $((Get-ChildItem $releaseDir -Recurse | Measure-Object Length -Sum).Sum / 1MB) MB" -ForegroundColor Green

# Inno Setup installer
if (-not $NoInstallers) {
    $innoPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "ISCC.exe"
    )
    $iscc = $null
    foreach ($p in $innoPaths) {
        if (Get-Command $p -ErrorAction SilentlyContinue) { $iscc = $p; break }
    }

    if ($iscc) {
        Write-Step "Building Inno Setup installer"
        & $iscc "$root\installer.iss"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✔ Inno Setup installer created" -ForegroundColor Green
            $installerDir = "$root\Installer"
            if (Test-Path $installerDir) {
                $files = Get-ChildItem $installerDir -Filter "*.exe"
                foreach ($f in $files) {
                    Write-Host "  -> $($f.FullName) ($($f.Length / 1MB -as [int]) MB)" -ForegroundColor Green
                }
            }
        } else {
            Write-Warning "Inno Setup compilation failed (exit code: $LASTEXITCODE)"
        }
    } else {
        Write-Warning "Inno Setup not found. Skipping installer."
        Write-Warning "Install Inno Setup 6 from: https://jrsoftware.org/isdl.php"
    }
}

Write-Step "Done!"
Write-Host "Release files: $releaseDir" -ForegroundColor Green
Write-Host "Run the installer or copy Release\ folder to target machine." -ForegroundColor Yellow
Write-Host "FFmpeg will be downloaded automatically on first run." -ForegroundColor Yellow
