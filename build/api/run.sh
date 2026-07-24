#!/bin/bash
PORT=${PORT:-80}
HOST=${HOST:-0.0.0.0}
export ASPNETCORE_URLS="http://$HOST:$PORT"
export ASPNETCORE_ENVIRONMENT="Production"
cd "$(dirname "$0")"
./SpotifyDownloader.Api
