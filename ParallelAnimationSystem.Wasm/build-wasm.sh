#!/usr/bin/env bash
set -euo pipefail

dotnet restore
dotnet publish -c Release

rm -rf ./ts/mod
mkdir -p ./ts/mod
cp -r ./bin/Release/net10.0/browser-wasm/native/* ./ts/mod

echo "Copied native files to ./ts/mod"