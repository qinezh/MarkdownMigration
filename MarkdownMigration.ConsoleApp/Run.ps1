param (
    [string]$repoRoot,
    [string]$repoUrl,
    [string]$outputFolder
)

$ErrorActionPreference = 'Stop'

function CheckExitCode {
    param($exitCode, $msg)
    if ($exitCode -eq 0) {
        Write-Host "Success: $msg
        " -ForegroundColor Green
    }
    else {
        Write-Host "Error $($exitCode): $msg
        " -ForegroundColor Red
        Pop-Location
        Exit 1
    }
}

# Formats JSON in a nicer format than the built-in ConvertTo-Json does.
function Format-Json([Parameter(Mandatory, ValueFromPipeline)][String] $json) {
  $indent = 0;
  ($json -Split '\n' |
    % {
      if ($_ -match '[\}\]]') {
        # This line contains  ] or }, decrement the indentation level
        $indent--
      }
      $line = (' ' * $indent * 2) + $_.TrimStart().Replace(':  ', ': ')
      if ($_ -match '[\{\[]') {
        # This line contains [ or {, increment the indentation level
        $indent++
      }
      $line
  }) -Join "`n" -replace "`r?`n *`r?`n *",""
}

$repoName = "TestRepo"
if ($repoUrl) {
    $repoName = ($repoUrl -split "/")[-1] -replace ".git",""
}
$repoReport = @{repo_name=$repoName}
$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
if (-Not $outputFolder) {
    $outputFolder = Join-Path $scriptPath "_o"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
function Zip
{
    param([string]$source, [string]$outzippath)

    [System.IO.Compression.ZipFile]::CreateFromDirectory($source, $outzippath)
}
if (Test-Path $outputFolder)
{
    Remove-Item -Path $outputFolder -Recurse -Force
}
New-Item $outputFolder -type directory -Force
Push-Location $repoRoot

$docFxVersion = "2.34.0"

$toolsPath = Join-Path $scriptPath "_tools"
New-Item $toolsPath -type directory -Force
$nugetPath = Join-Path $toolsPath "nuget.exe"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

if (-not (Test-Path $nugetPath))
{
    Invoke-WebRequest -Uri $nugetUrl -OutFile $nugetPath
}
$migrationExePath = Join-Path $scriptPath "MarkdownMigration.ConsoleApp.exe"

& $nugetPath install docfx.console -Version $docFxVersion -Source https://www.myget.org/F/docfx/api/v3/index.json -OutputDirectory $toolsPath
$docfxFolder = Join-Path $toolsPath "docfx.console.$docFxVersion\tools"
$docfxExePath = Join-Path $docfxFolder "docfx.exe"
$tempdfmfolder = Join-Path $outputFolder "tempdfm"

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
        $markdigOutput = Join-Path $htmlBaseFolder "md"
        $markdigHtmlOutput = Join-Path $htmlBaseFolder "md-html"

        
        $docfxJsonPath = Join-Path $docsetFolder "docfx.json"
        if (-not (Test-Path $docfxJsonPath))
        {
            $docsetFolder = Join-Path $repoRoot "$($docset.build_output_subfolder)"
            $docfxJsonPath = Join-Path $docsetFolder "docfx.json"
        }

        $docfxJson = Get-Content -Raw -Path $docfxJsonPath | ConvertFrom-Json

        if ($docfxJson.build.markdownEngineName -eq "markdig")
        {
            Write-Host "Already markdig, skipping this docset..."
            continue;
        }

        $dest = "_site"
        if ($docfxJson.build.dest)
        {
            $dest = Join-Path $docsetFolder $docfxJson.build.dest
        }

        robocopy $docsetFolder $tempdfmfolder *.md /s

        & $docfxExePath $docfxJsonPath --exportRawModel --dryRun --force
        CheckExitCode $lastexitcode "baseline build"

        Copy-Item -Path $dest -Destination $dfmOutput -recurse -Force
        Remove-Item -path $dest -recurse
        Remove-Item -path "$docsetFolder\obj" -recurse

        if ($docfxJson.build.markdownEngineName -ne "markdig")
        {
			if ($docfxJson.build.markdownEngineName -ne "dfm-latest")
			{
				& $migrationExePath -m -c $docsetFolder -p "**.md" -l
			}else
			{
				& $migrationExePath -m -c $docsetFolder -p "**.md"
			}
            CheckExitCode $lastexitcode "migration"

            $reportPath = Join-Path $docsetFolder "report.json"
            $reportDestPath = Join-Path $htmlBaseFolder "report.json"
            Copy-Item -Path $reportPath -Destination $reportDestPath -recurse -Force
            Remove-Item -path $reportPath
        }

        & $docfxExePath $docfxJsonPath --exportRawModel --dryRun --force --markdownEngineName markdig
        CheckExitCode $lastexitcode "markdig build"

        Write-Host "copy from $dest to $markdigOutput"
        Copy-Item -Path $dest -Destination $markdigOutput -recurse -Force
        Remove-Item -path $dest -recurse
        Remove-Item -path "$docsetFolder\obj" -recurse

        & $migrationExePath -d -j "$dfmOutput,$markdigOutput" -rpf $reportDestPath -bp $tempdfmfolder
        CheckExitCode $lastexitcode "diff"

        Remove-Item -path $tempdfmfolder -recurse
        Remove-Item -path $dfmOutput -recurse
        Remove-Item -path $markdigOutput -recurse 
        Zip $dfmHtmlOutput "$dfmHtmlOutput.zip" 
        Zip $markdigHtmlOutput "$markdigHtmlOutput.zip" 
        Remove-Item -path $dfmHtmlOutput -recurse
        Remove-Item -path $markdigHtmlOutput -recurse 

        $docset = [IO.File]::ReadAllText($reportDestPath) | ConvertFrom-Json
        $docset.docset_name = $docsetName
        $repoReport.docsets += $docset

        $newtonsoft = Join-Path $scriptPath "Newtonsoft.Json.dll"
        [Reflection.Assembly]::LoadFile($newtonsoft)
        $content = Get-Content -Raw -Path $docfxJsonPath
        $obj = [Newtonsoft.Json.Linq.JObject]::Parse($content)
        $obj.build.markdownEngineName = "markdig"
        $obj.ToString() | Out-File -Encoding ascii $docfxJsonPath
    }
    
    $repoReportPath = "$outputFolder\repoReport.json"
    $repoReport | ConvertTo-Json -Depth 100 | Out-File $repoReportPath
    & $migrationExePath -ge -rpf $repoReportPath -repourl $repoUrl
}
Pop-Location
