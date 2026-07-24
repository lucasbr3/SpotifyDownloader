FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .

RUN apt-get update -qq && apt-get install -y -qq python3 && ln -s /usr/bin/python3 /usr/bin/python

RUN dotnet publish src/SpotifyDownloader.Shared/SpotifyDownloader.Shared.csproj -c Release -o /app/shared
RUN dotnet publish src/SpotifyDownloader.Wasm/SpotifyDownloader.Wasm.csproj -c Release -o /app/wasm

# Debug: show WASM output structure
RUN ls -la /app/wasm/ && ls -la /app/wasm/wwwroot/ 2>/dev/null || true

RUN dotnet publish src/SpotifyDownloader.Api/SpotifyDownloader.Api.csproj -c Release -o /app/api

# Copy WASM files into API's wwwroot
RUN mkdir -p /app/api/wwwroot
RUN if [ -d /app/wasm/wwwroot ]; then \
      cp -r /app/wasm/wwwroot/. /app/api/wwwroot/; \
    elif [ -f /app/wasm/index.html ]; then \
      cp -r /app/wasm/. /app/api/wwwroot/; \
      rm -f /app/api/wwwroot/*.json /app/api/wwwroot/*.config; \
    fi

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/api .
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "SpotifyDownloader.Api.dll"]