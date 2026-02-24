#!/usr/bin/env bash
set -euo pipefail

dotnet publish -c Release

rm -rf ./ts/wasm
mkdir -p ./ts/wasm
cp -r ./bin/Release/net10.0/browser-wasm/native/* ./ts/wasm

echo "Copied native files to ./ts/wasm"