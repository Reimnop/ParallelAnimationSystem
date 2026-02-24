$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

dotnet publish -c Release

Remove-Item -Recurse -Force ./ts/src/wasm -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path ./ts/src/wasm | Out-Null
Copy-Item -Recurse ./bin/Release/net10.0/browser-wasm/native/* ./ts/src/wasm

"Copied native files to ./ts/src/wasm"
