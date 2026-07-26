FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .

RUN dotnet publish src/SpotifyDownloader.Shared/SpotifyDownloader.Shared.csproj -c Release -o /app/shared
RUN dotnet publish src/SpotifyDownloader.Wasm/SpotifyDownloader.Wasm.csproj -c Release -o /app/wasm
RUN dotnet publish src/SpotifyDownloader.Api/SpotifyDownloader.Api.csproj -c Release -o /app/api

# Find WASM wwwroot directory anywhere and copy into API wwwroot
RUN WASM_DIR=$(find /app -name "_framework" -type d -path "*/wwwroot/_framework" | head -1) && \
    SRC=$(dirname "$WASM_DIR") && \
    echo "Source: $SRC" && \
    mkdir -p /app/api/wwwroot && \
    cp -r "$SRC/." /app/api/wwwroot/ && \
    echo "OK: $(ls /app/api/wwwroot/_framework/icudt_no_CJK.dat 2>/dev/null && echo 'ICU FOUND' || echo 'ICU MISSING')"

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/api .
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "SpotifyDownloader.Api.dll"]