param (
    [string]$repoRoot,
    [string]$repoUrl,
    [string]$outputFolder
)
$repoName = "TestRepo"
if ($repoUrl) {
    $repoName = ($repoUrl -split "/")[-1] -replace ".git",""
}
$repoReport = @{repo_name=$repoName}
$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
if (-Not $outputFolder) {
    $outputFolder = Join-Path $scriptPath "_output"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
function Zip
{
    param([string]$source, [string]$outzippath)

    [System.IO.Compression.ZipFile]::CreateFromDirectory($source, $outzippath)
}

New-Item $outputFolder -type directory -Force
Push-Location $repoRoot

$markDigVersion = "1.0.127-alpha"
$docFxVersion = "2.28.3"

$toolsPath = Join-Path $scriptPath "_tools"
New-Item $toolsPath -type directory -Force
$nugetPath = Join-Path $toolsPath "nuget.exe"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

if (-not (Test-Path $nugetPath))
{
    Invoke-WebRequest -Uri $nugetUrl -OutFile $nugetPath
}
$migrationExePath = Join-Path $scriptPath "MarkdownMigration.ConsoleApp.exe"

& $nugetPath install Microsoft.DocAsCode.MarkdigEngine -Version $markDigVersion -Source https://www.myget.org/F/op-dev/api/v2 -OutputDirectory $toolsPath
& $nugetPath install docfx.console -Version $docFxVersion -Source https://www.myget.org/F/docfx/api/v3/index.json -OutputDirectory $toolsPath
$markdigPackPath = Join-Path $toolsPath "Microsoft.DocAsCode.MarkdigEngine.$markDigVersion\content\plugins"
$docfxFolder = Join-Path $toolsPath "docfx.console.$docFxVersion\tools"
$docfxExePath = Join-Path $docfxFolder "docfx.exe"
Copy-Item $markdigPackPath $docfxFolder -Recurse -Force

$repoConfig = Get-Content -Raw -Path .openpublishing.publish.config.json | ConvertFrom-Json

if ($repoConfig.docsets_to_publish)
{
    $repoReport.docsets = @()
    foreach ($docset in $repoConfig.docsets_to_publish)
    {
        $docsetName = $docset.docset_name
        $docsetFolder = Join-Path $repoRoot "$($docset.build_source_folder)"  

        $htmlBaseFolder = Join-Path $outputFolder $docsetName
        $dfmOutput = Join-Path $htmlBaseFolder "dfm"
        $dfmHtmlOutput = Join-Path $htmlBaseFolder "dfm-html"
        $markdigOutput = Join-Path $htmlBaseFolder "markdig"
        $markdigHtmlOutput = Join-Path $htmlBaseFolder "markdig-html"

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

        & $migrationExePath -m -c $docsetFolder -p "**.md"
        $reportPath = Join-Path $docsetFolder "report.json"
        $reportDestPath = Join-Path $htmlBaseFolder "report.json"
        Copy-Item -Path $reportPath -Destination $reportDestPath -recurse -Force
        Remove-Item -path $reportPath

        & $docfxExePath $docfxJsonPath --exportRawModel --dryRun --force --markdownEngineName markdig
        Copy-Item -Path $dest -Destination $markdigOutput -recurse -Force
        Remove-Item -path $dest -recurse
        Remove-Item -path "$docsetFolder\obj" -recurse

        & $migrationExePath -d -j "$dfmOutput,$markdigOutput" -rpf $reportDestPath -crp "$htmlBaseFolder\Compare" -bp $docsetFolder

        Zip $dfmOutput "$dfmOutput.zip"
        Zip $markdigOutput "$markdigOutput.zip" 
        Zip $dfmHtmlOutput "$dfmHtmlOutput.zip" 
        Zip $markdigHtmlOutput "$markdigHtmlOutput.zip" 

        $docset = [IO.File]::ReadAllText($reportDestPath) | ConvertFrom-Json
        $docset.docset_name = $docsetName
        $repoReport.docsets += $docset
    }
    
    $repoReportPath = "$outputFolder\repoReport.json"
    $repoReport | ConvertTo-Json -Depth 100 | Out-File $repoReportPath
    & $migrationExePath -ge -rpf $repoReportPath -repourl $repoUrl
}
Pop-Location
