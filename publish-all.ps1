param(
    [string]$Runtime = "linux-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== Building WASM ==="
dotnet publish "$root\src\SpotifyDownloader.Wasm\SpotifyDownloader.Wasm.csproj" `
    -c $Configuration -o "$root\build\wasm"

Write-Host "=== Building API ==="
dotnet publish "$root\src\SpotifyDownloader.Api\SpotifyDownloader.Api.csproj" `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true -o "$root\build\api"

Write-Host "=== Copying WASM into API ==="
if (Test-Path "$root\build\api\wwwroot") {
    Remove-Item "$root\build\api\wwwroot" -Recurse -Force
}
New-Item -ItemType Directory -Path "$root\build\api\wwwroot" -Force | Out-Null
$wasmOutput = "$root\build\wasm\wwwroot"
if (-not (Test-Path $wasmOutput)) { $wasmOutput = "$root\build\wasm" }
Copy-Item "$wasmOutput\*" "$root\build\api\wwwroot\" -Recurse -Force

Write-Host "=== Creating run.sh ==="
@'
#!/bin/bash
PORT=${PORT:-80}
HOST=${HOST:-0.0.0.0}
export ASPNETCORE_URLS="http://$HOST:$PORT"
export ASPNETCORE_ENVIRONMENT="Production"
cd "$(dirname "$0")"
./SpotifyDownloader.Api
'@ | Set-Content -Path "$root\build\api\run.sh" -Encoding utf8

Write-Host "=== Done ==="
Write-Host "Output: $root\build\api"
Write-Host "Run: cd build/api && chmod +x run.sh && ./run.sh"