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

$docFxVersion = "2.36.2"

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
$tempdfmfolderBase = Join-Path $outputFolder "tempdfm"
$tempdfmymlfolder = Join-Path $outputFolder "tempyml"

$repoConfig = Get-Content -Raw -Path .openpublishing.publish.config.json | ConvertFrom-Json

if ($repoConfig.docsets_to_publish)
{
    $repoReport.docsets = @()
    foreach ($docset in $repoConfig.docsets_to_publish)
    {
        $docsetName = $docset.docset_name
        $source_folder = $($docset.build_source_folder)
        $docsetFolder = Join-Path $repoRoot "$source_folder"  

        $htmlBaseFolder = Join-Path $outputFolder $docsetName
        $dfmOutput = Join-Path $htmlBaseFolder "dfm"
        $dfmHtmlOutput = Join-Path $htmlBaseFolder "dfm-html"
        $markdigOutput = Join-Path $htmlBaseFolder "md"
        $markdigHtmlOutput = Join-Path $htmlBaseFolder "md-html"
                
        $docfxJsonPath = Join-Path $docsetFolder "docfx.json"
        if (-not (Test-Path $docfxJsonPath))
        {
            $source_folder = $($docset.build_output_subfolder)
            $docsetFolder = Join-Path $repoRoot "$source_folder"
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

        $tempdfmfolder = Join-Path $tempdfmfolderBase $source_folder
        robocopy $docsetFolder $tempdfmfolder *.md /s

        robocopy $docsetFolder $tempdfmymlfolder *.yml /s
        Get-ChildItem $docsetFolder -recurse -include *.yml | del

        & $docfxExePath $docfxJsonPath --exportRawModel --dryRun --force
        CheckExitCode $lastexitcode "baseline build"

        Copy-Item -Path $dest -Destination $dfmOutput -recurse -Force
        Remove-Item -path $dest -recurse
        Remove-Item -path "$docsetFolder\obj" -recurse

        if ($docfxJson.build.markdownEngineName -ne "markdig")
        {
            if ($docfxJson.build.markdownEngineName -ne "dfm-latest")
            {
                & $migrationExePath -m -c $docsetFolder -p "**.md" -l -docsetfolder $source_folder
            }else
            {
                & $migrationExePath -m -c $docsetFolder -p "**.md" -docsetfolder $source_folder
            }
            CheckExitCode $lastexitcode "migration"

            $reportPath = Join-Path $docsetFolder "report.json"
            $reportDestPath = Join-Path $htmlBaseFolder "report.json"
            Copy-Item -Path $reportPath -Destination $reportDestPath -recurse -Force
            Remove-Item -path $reportPath
        }

        & $docfxExePath $docfxJsonPath --exportRawModel --dryRun --force --markdownEngineName markdig
        CheckExitCode $lastexitcode "markdig build"

        robocopy $tempdfmymlfolder $docsetFolder *.yml /s
        if (Test-Path $tempdfmymlfolder)
        {
            Remove-Item -path $tempdfmymlfolder -recurse
        }

        Write-Host "copy from $dest to $markdigOutput"
        Copy-Item -Path $dest -Destination $markdigOutput -recurse -Force
        Remove-Item -path $dest -recurse
        Remove-Item -path "$docsetFolder\obj" -recurse

        & $migrationExePath -d -j "$dfmOutput,$markdigOutput" -rpf $reportDestPath -bp $tempdfmfolderBase -docsetfolder $source_folder
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

    if (Test-Path $tempdfmfolderBase)
    {
        Remove-Item -path $tempdfmfolderBase -recurse
    }

    $repoReportPath = "$outputFolder\repoReport.json"
    $repoReport | ConvertTo-Json -Depth 100 | Out-File $repoReportPath
    $branch = & git symbolic-ref refs/remotes/origin/HEAD
    & $migrationExePath -ge -rpf $repoReportPath -repourl $repoUrl -branch $branch
}
Pop-Location
