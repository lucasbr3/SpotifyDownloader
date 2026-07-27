FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .

RUN apt-get update -qq && apt-get install -y -qq python3

RUN dotnet workload install wasm-tools

RUN dotnet publish src/SpotifyDownloader.Shared/SpotifyDownloader.Shared.csproj -c Release -o /app/shared
RUN dotnet publish src/SpotifyDownloader.Wasm/SpotifyDownloader.Wasm.csproj -c Release -o /app/wasm -p:BlazorIcuDataEnabled=false
RUN dotnet publish src/SpotifyDownloader.Api/SpotifyDownloader.Api.csproj -c Release -o /app/api

RUN find /app -name "index.html" 2>/dev/null; find /app -name "_framework" -type d 2>/dev/null

RUN mkdir -p /app/api/wwwroot && \
    if [ -d /app/wasm/wwwroot ]; then \
        cp -r /app/wasm/wwwroot/. /app/api/wwwroot/ && echo "OK: wasm/wwwroot"; \
    elif [ -f /app/wasm/index.html ]; then \
        cp -r /app/wasm/. /app/api/wwwroot/ && rm -f /app/api/wwwroot/*.dll /app/api/wwwroot/*.pdb && echo "OK: wasm root"; \
    else \
        echo "ERROR: WASM wwwroot not found"; exit 1; \
    fi

RUN python3 -c "b=json.load(open('/app/api/wwwroot/_framework/blazor.boot.json')); b.get('resources',{}).pop('icu',None); b['globalizationMode']='invariant'; json.dump(b,open('/app/api/wwwroot/_framework/blazor.boot.json','w'),indent=2); print('OK')"

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/api .
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "SpotifyDownloader.Api.dll"]
