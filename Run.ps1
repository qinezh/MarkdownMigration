param (
    [string]$repoRoot
)
if (-not $repoRoot)
{
    $repoRoot = $pwd.Path
}
$repoRoot = Resolve-Path $repoRoot
$tempFolder = Join-Path $repoRoot "_temp"
New-Item $tempFolder -type directory -Force
Push-Location $repoRoot

$migrationToolUrl = "https://github.com/qinezh/MarkdownMigration/releases/download/0.1/Migration.zip"
$migrationToolZipPath = Join-Path $repoRoot "_tools/Migration.zip"
$migrationToolPath = Join-Path $repoRoot "_tools/Migration"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$nugetPath = "_tools/nuget.exe"


Add-Type -AssemblyName System.IO.Compression.FileSystem
function Unzip
{
    param([string]$zipfile, [string]$outpath)

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

New-Item _tools -type directory -Force
if (-not (Test-Path $nugetPath))
{
    Invoke-WebRequest -Uri $nugetUrl -OutFile $nugetPath
}
if (Test-path $migrationToolZipPath)
{
    Remove-Item $migrationToolZipPath
}
if (Test-Path $migrationToolPath)
{
    #Remove-Item $migrationToolPath -Recurse
}
#Invoke-WebRequest -Uri $migrationToolUrl -OutFile $migrationToolZipPath
New-Item $migrationToolPath -type directory
#Unzip $migrationToolZipPath (Join-Path $repoRoot "_tools")
$migrationExePath = Join-Path $migrationToolPath "MarkdownMigration.Convert.exe"
$htmlExtractExePath = Join-Path $migrationToolPath "ExtractHtml.exe"
$htmlCompareExePath = Join-Path $migrationToolPath "HtmlCompare.exe"

& $nugetPath install Microsoft.DocAsCode.MarkdigEngine -Version 1.0.127-alpha -Source https://www.myget.org/F/op-dev/api/v2 -OutputDirectory _tools
& $nugetPath install docfx.console -Version 2.28.3 -Source https://www.myget.org/F/docfx/api/v3/index.json -OutputDirectory _tools
$markdigPackPath = Join-Path @(get-childitem -path .\_tools\Microsoft.DocAsCode.MarkdigEngine*)[0] "content/plugins"
$docfxFolder = Join-Path @(get-childitem -path .\_tools\docfx.console*)[0] "tools"
$docfxExePath = Join-Path $docfxFolder "docfx.exe"
Copy-Item $markdigPackPath $docfxFolder -Recurse -Force

$repoConfig = Get-Content -Raw -Path .openpublishing.publish.config.json | ConvertFrom-Json
if ($repoConfig -and $repoConfig.dependent_repositories)
{
    foreach ($r in $repoConfig.dependent_repositories) {
       if (-not (Test-Path $r.path_to_root) -and -not $r.path_to_root.StartsWith("_themes"))
       {
            & git clone $r.url $r.path_to_root
       } 
    }
}

if ($repoConfig.docsets_to_publish)
{
    foreach ($docset in $repoConfig.docsets_to_publish)
    {
        $docsetName = $docset.docset_name
        $docsetFolder = Join-Path $repoRoot "$($docset.build_source_folder)"  

        $htmlBaseFolder = Join-Path $tempFolder $docsetName
        $dfmOutput = Join-Path $htmlBaseFolder "dfm"
        $markdigOutput = Join-Path $htmlBaseFolder "markdig"

        $docfxJsonPath = Join-Path $docsetFolder "docfx.json"
        $docfxJson = Get-Content -Raw -Path $docfxJsonPath | ConvertFrom-Json
        $dest = "_site"
        if ($docfxJson.build.dest)
        {
            $dest = Join-Path $docsetFolder $docfxJson.build.dest
        }

        & $docfxExePath $docfxJsonPath --exportRawModel --dryRun --force --markdownEngineName dfm 
        Copy-Item -Path $dest -Destination $dfmOutput -recurse -Force
        Remove-Item -path $dest -recurse
        Remove-Item -path "$docsetFolder\obj" -recurse

        & $migrationExePath -c $docsetFolder -p "**.md"

        $reportPath = Join-Path $docsetFolder "report.json"
        $reportDestPath = Join-Path $dfmOutput "report.json"
        Copy-Item -Path $reportPath -Destination $reportDestPath -recurse -Force

        & $docfxExePath $docfxJsonPath --exportRawModel --dryRun --force --markdownEngineName markdig
        Copy-Item -Path $dest -Destination $markdigOutput -recurse -Force
        Remove-Item -path $dest -recurse
        Remove-Item -path "$docsetFolder\obj" -recurse

        & $htmlExtractExePath "$dfmOutput,$markdigOutput" $($docset.build_source_folder)
        $dfmHtmlOutput = $dfmOutput + "-html"
        $markdigHtmlOutput =$markdigOutput + "-html"

        if ((Test-Path $dfmHtmlOutput) -and (Test-Path $markdigHtmlOutput))
        {
           & $htmlCompareExePath $dfmHtmlOutput $markdigHtmlOutput $htmlBaseFolder "$htmlBaseFolder\Compare"
        }
    }
}

Pop-Location $repoRoot
