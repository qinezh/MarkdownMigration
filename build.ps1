$ErrorActionPreference = 'Stop'
$scriptHome = Split-Path $MyInvocation.MyCommand.Path
$releaseFolder = "$scriptHome/release"
$proj = "MarkdownMigration.ConsoleApp/MarkdownMigration.ConsoleApp.csproj"

& dotnet restore $proj
& dotnet publish $proj -c Release -f net461 -o $releaseFolder
