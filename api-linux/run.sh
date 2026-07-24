#!/bin/bash
# Spotify Downloader API - Linux Runner
# Usage: chmod +x run.sh && ./run.sh

PORT=${PORT:-5000}
HOST=${HOST:-0.0.0.0}

echo "Starting Spotify Downloader API on $HOST:$PORT..."
export ASPNETCORE_URLS="http://$HOST:$PORT"
export ASPNETCORE_ENVIRONMENT="Production"

./SpotifyDownloader.Api
