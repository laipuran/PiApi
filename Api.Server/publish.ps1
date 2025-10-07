rm -Path ./publish -Recurse -Force
dotnet publish -c Release -r linux-arm64  -o $PSScriptRoot/publish