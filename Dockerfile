FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .

RUN dotnet workload install wasm-tools

RUN dotnet publish src/SpotifyDownloader.Shared/SpotifyDownloader.Shared.csproj -c Release -o /app/shared
RUN dotnet publish src/SpotifyDownloader.Wasm/SpotifyDownloader.Wasm.csproj -c Release -o /app/wasm
RUN dotnet publish src/SpotifyDownloader.Api/SpotifyDownloader.Api.csproj -c Release -o /app/api

RUN mkdir -p /app/api/wwwroot && \
    if [ -d /app/wasm/wwwroot ]; then \
        cp -r /app/wasm/wwwroot/. /app/api/wwwroot/ && echo "OK: wasm/wwwroot"; \
    elif [ -f /app/wasm/index.html ]; then \
        cp -r /app/wasm/. /app/api/wwwroot/ && rm -f /app/api/wwwroot/*.dll /app/api/wwwroot/*.pdb && echo "OK: wasm root"; \
    else \
        echo "ERROR: WASM wwwroot not found"; exit 1; \
    fi

RUN ls -la /app/api/wwwroot/_framework/icudt_*.dat 2>/dev/null || echo "NO ICU FILES"

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/api .
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "SpotifyDownloader.Api.dll"]
