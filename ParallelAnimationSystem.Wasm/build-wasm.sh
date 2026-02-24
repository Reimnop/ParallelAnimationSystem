#!/usr/bin/env bash
set -euo pipefail

dotnet publish -c Release

rm -rf ./ts/src/wasm
mkdir -p ./ts/src/wasm
cp -r ./bin/Release/net10.0/browser-wasm/native/* ./ts/src/wasm

echo "Copied native files to ./ts/src/wasm"