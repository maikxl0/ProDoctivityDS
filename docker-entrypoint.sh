#!/bin/sh
set -e

export APP_DATA_DIR="${APP_DATA_DIR:-/var/data}"
export ASPNETCORE_URLS="http://+:${PORT:-10000}"

mkdir -p "$APP_DATA_DIR"

exec dotnet ProDoctivityDS.dll
