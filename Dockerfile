FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .

RUN apt-get update -qq && apt-get install -y -qq python3 && ln -sf /usr/bin/python3 /usr/bin/python

RUN dotnet workload install wasm-tools 2>/dev/null || echo "wasm-tools optional"

RUN dotnet publish src/SpotifyDownloader.Shared/SpotifyDownloader.Shared.csproj -c Release -o /app/shared
RUN dotnet publish src/SpotifyDownloader.Wasm/SpotifyDownloader.Wasm.csproj -c Release -o /app/wasm
RUN dotnet publish src/SpotifyDownloader.Api/SpotifyDownloader.Api.csproj -c Release -o /app/api

# Debug: show WASM output structure
RUN find /app -name "index.html" 2>/dev/null | head -10; find /app -name "_framework" -type d 2>/dev/null | head -5

# Copy WASM wwwroot to API wwwroot
RUN mkdir -p /app/api/wwwroot && \
    if [ -d /app/src/SpotifyDownloader.Wasm/bin/Release/net8.0/wwwroot ] && [ -f /app/src/SpotifyDownloader.Wasm/bin/Release/net8.0/wwwroot/index.html ]; then \
        cp -r /app/src/SpotifyDownloader.Wasm/bin/Release/net8.0/wwwroot/. /app/api/wwwroot/ && echo "OK: project bin"; \
    elif [ -d /app/wasm/wwwroot ] && [ -f /app/wasm/wwwroot/index.html ]; then \
        cp -r /app/wasm/wwwroot/. /app/api/wwwroot/ && echo "OK: wasm/wwwroot"; \
    elif [ -f /app/wasm/index.html ]; then \
        cp -r /app/wasm/. /app/api/wwwroot/ && rm -f /app/api/wwwroot/*.dll /app/api/wwwroot/*.pdb && echo "OK: wasm root"; \
    else \
        echo "ERROR: WASM wwwroot not found - build failed"; exit 1; \
    fi

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/api .
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "SpotifyDownloader.Api.dll"]