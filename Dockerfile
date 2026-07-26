FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .

RUN apt-get update -qq && apt-get install -y -qq python3 && ln -sf /usr/bin/python3 /usr/bin/python

RUN dotnet workload install wasm-tools 2>/dev/null || echo "wasm-tools optional"

RUN dotnet publish src/SpotifyDownloader.Shared/SpotifyDownloader.Shared.csproj -c Release -o /app/shared
RUN dotnet publish src/SpotifyDownloader.Wasm/SpotifyDownloader.Wasm.csproj -c Release -o /app/wasm
RUN dotnet publish src/SpotifyDownloader.Api/SpotifyDownloader.Api.csproj -c Release -o /app/api

# Copy WASM wwwroot to API wwwroot
RUN mkdir -p /app/api/wwwroot && \
    if [ -d /app/src/SpotifyDownloader.Wasm/bin/Release/net8.0/wwwroot ]; then \
        cp -r /app/src/SpotifyDownloader.Wasm/bin/Release/net8.0/wwwroot/. /app/api/wwwroot/; \
    elif [ -d /app/wasm/wwwroot ]; then \
        cp -r /app/wasm/wwwroot/. /app/api/wwwroot/; \
    else \
        cp -r /app/wasm/. /app/api/wwwroot/ && \
        rm -f /app/api/wwwroot/*.dll /app/api/wwwroot/*.pdb /app/api/wwwroot/*.json /app/api/wwwroot/*.config; \
    fi

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/api .
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "SpotifyDownloader.Api.dll"]