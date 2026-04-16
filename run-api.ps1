$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$env:TEMP = Join-Path $root ".tmp"
$env:TMP = $env:TEMP
$env:NUGET_PACKAGES = Join-Path $root ".nuget"

New-Item -ItemType Directory -Force $env:TEMP, $env:NUGET_PACKAGES | Out-Null
dotnet run --no-launch-profile --project (Join-Path $root "RecipeBook.Api\RecipeBook.Api.csproj")
