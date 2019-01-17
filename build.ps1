$ErrorActionPreference = 'Stop'
$scriptHome = Split-Path $MyInvocation.MyCommand.Path
$releaseFolder = "$scriptHome/artifacts"
$proj = "MarkdownMigration.ConsoleApp/MarkdownMigration.ConsoleApp.csproj"
$pluginProj = "DfmPlugins/DfmPlugins.csproj"

& dotnet restore $proj
& dotnet publish $proj -c Release -f net462 -o $releaseFolder
& dotnet restore $pluginProj
& dotnet publish $pluginProj -c Release -f net462 -o $releaseFolder/plugins
