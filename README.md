# Spotify Downloader

Baixe músicas, álbuns e playlists do Spotify sem necessidade de autenticação.

Powered by .NET 8 + WinUI 3 + FFmpeg.

## Features

- Pesquisa em tempo real com AutoSuggest
- Cards com álbum, artista, duração
- Player completo com equalizador, letras e mini player
- Espectro visualizador animado
- Tema escuro, claro e 7 cores (AMOLED, Azul, Verde, Roxo, Vermelho, Laranja)
- Multi-idioma: Português, English, Español
- Histórico de downloads com estatísticas
- Auto-update via GitHub Releases

## Como usar

1. **Baixe a última versão** na [página de releases](https://github.com/anomalyco/SpotifyDownloader/releases)
2. **Instale o FFmpeg** (necessário para conversão de áudio)
   - Baixe de: https://ffmpeg.org/download.html
   - Coloque `ffmpeg.exe` na mesma pasta do executável ou adicione ao PATH
3. Execute `SpotifyDownloader.exe`

## Compilando do código-fonte

### Pré-requisitos
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (carga: "Desenvolvimento da Plataforma Universal do Windows")
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)

### Passos

```powershell
git clone https://github.com/anomalyco/SpotifyDownloader.git
cd SpotifyDownloader
dotnet restore -p:Platform=x64
dotnet build -c Release -p:Platform=x64
```

O executável estará em:
`src\SpotifyDownloader.App\bin\x64\Release\net8.0-windows10.0.19041.0\SpotifyDownloader.exe`

### Instaladores

- **Inno Setup**: `ISCC.exe installer.iss` (requer [Inno Setup 6](https://jrsoftware.org/isdl.php))
- **MSIX**: compile com `/p:AppxPackage=true` no MSBuild, ou use o Visual Studio

## Licença

Este projeto é fornecido apenas para fins educacionais.
