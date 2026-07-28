FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .

RUN dotnet publish src/SpotifyDownloader.Shared/SpotifyDownloader.Shared.csproj -c Release -o /app/shared && \
    dotnet publish src/SpotifyDownloader.Wasm/SpotifyDownloader.Wasm.csproj -c Release -o /app/wasm && \
    dotnet publish src/SpotifyDownloader.Api/SpotifyDownloader.Api.csproj -c Release -o /app/api

RUN mkdir -p /app/api/wwwroot && cp -r /app/wasm/wwwroot/. /app/api/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update -qq && apt-get install -y -qq ffmpeg python3 python3-pip && rm -rf /var/lib/apt/lists/*
RUN pip3 install yt-dlp -q && which yt-dlp
WORKDIR /app
COPY --from=build /app/api .
RUN rm -f ffmpeg.exe
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "SpotifyDownloader.Api.dll"]
