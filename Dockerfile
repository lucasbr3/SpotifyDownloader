FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .

RUN apt-get update -qq && apt-get install -y -qq python3

RUN dotnet publish src/SpotifyDownloader.Shared/SpotifyDownloader.Shared.csproj -c Release -o /app/shared
RUN dotnet publish src/SpotifyDownloader.Wasm/SpotifyDownloader.Wasm.csproj -c Release -o /app/wasm
RUN dotnet publish src/SpotifyDownloader.Api/SpotifyDownloader.Api.csproj -c Release -o /app/api

# Copy WASM wwwroot to API wwwroot
RUN mkdir -p /app/api/wwwroot && \
    if [ -d /app/wasm/wwwroot ]; then \
        cp -r /app/wasm/wwwroot/. /app/api/wwwroot/ && echo "Copied from wasm/wwwroot"; \
    elif [ -f /app/wasm/index.html ]; then \
        cp -r /app/wasm/. /app/api/wwwroot/ && rm -f /app/api/wwwroot/*.dll /app/api/wwwroot/*.pdb && echo "Copied from wasm"; \
    else \
        echo "ERROR: WASM wwwroot not found"; exit 1; \
    fi

# Remove ICU entries from boot.json (files not available without wasm-tools)
RUN if [ -f /app/api/wwwroot/_framework/blazor.boot.json ]; then \
        python3 -c "
import json
with open('/app/api/wwwroot/_framework/blazor.boot.json') as f:
    boot = json.load(f)
if 'icu' in boot.get('resources', {}):
    del boot['resources']['icu']
with open('/app/api/wwwroot/_framework/blazor.boot.json', 'w') as f:
    json.dump(boot, f, indent=2)
print('ICU removed from boot.json')
"; \
    else echo "No boot.json found"; fi

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/api .
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "SpotifyDownloader.Api.dll"]