# Spotify Downloader

A Windows desktop application for downloading music from Spotify without requiring authentication.

## Features

- Search tracks, albums, and playlists
- Download audio in multiple formats (MP3, FLAC, WAV, M4A, OGG)
- Built-in audio player with equalizer and lyrics
- Mini player with spectrum visualization
- 9 themes with Mica/Acrylic effects
- Multi-language support (Portuguese, English, Spanish)
- FFmpeg auto-download on first run (no manual installation needed)

## Requirements

- Windows 10 (build 19041+) or Windows 11
- No Spotify credentials needed
- No manual FFmpeg installation needed (auto-downloaded)

## Installation

### Download Installer

Download the latest `SpotifyDownloader_Setup_v*.exe` from [Releases](https://github.com/lucasbr3/SpotifyDownloader/releases).

Run the installer and follow the wizard. The app is ready to use immediately.

### Build from Source

1. Install Visual Studio 2022 with:
   - .NET 8 SDK
   - Windows App SDK
   - Windows 10 SDK (10.0.19041.0)

2. Run:
   ```
   build.cmd
   ```

3. Output: `Release\SpotifyDownloader.exe`

## How It Works

- **Metadata**: Extracted from public YouTube search results and Spotify page meta tags
- **Downloads**: YouTube audio extracted via FFmpeg
- **FFmpeg**: Downloaded automatically on first launch if not present

## Tech Stack

- C# .NET 8 + WinUI 3
- MVVM with CommunityToolkit.Mvvm
- FFmpeg for audio processing
- TagLibSharp for metadata embedding
- Serilog for logging

## Project Structure

```
SpotifyDownloader/
├── src/
│   ├── SpotifyDownloader.Core/     # Models, interfaces, services
│   └── SpotifyDownloader.App/      # WinUI 3 UI, views, viewmodels
├── assets/                         # Icons and resources
├── installer.iss                   # Inno Setup script
├── build.ps1 / build.cmd           # Build scripts
└── .github/workflows/build.yml     # CI/CD pipeline
```
