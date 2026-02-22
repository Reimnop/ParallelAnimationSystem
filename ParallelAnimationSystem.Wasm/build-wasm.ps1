$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

dotnet publish -c Release

Remove-Item -Recurse -Force ./ts/mod -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path ./ts/mod | Out-Null
Copy-Item -Recurse ./bin/Release/net10.0/browser-wasm/native/* ./ts/mod

"Copied native files to ./ts/mod"
