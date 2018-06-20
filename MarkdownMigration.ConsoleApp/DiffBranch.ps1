param (
    [string]$repoRoot="D:\\Works\\CI\\dynamics365hubpages",
    [string]$repoUrl="https://github.com/MicrosoftDocs/dynamics365hubpages",
    [string]$outputFolder="D:\Works\CI\_work\_output",
    [string]$dfmBranch="master",
    [string]$markdigBranch="markdigmigration"
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

Function Invoke-Git {
    [cmdletbinding()]
    param(
        [parameter(Position = 0,
                   ValueFromRemainingArguments = $true)]
        $Arguments,
        [validatescript({
            if(-not (Get-Command $_))
            {
                throw "Could not find command at GitPath $_"
            }
        })]
        $GitPath = 'git.exe'
    )

    $Path = (Resolve-Path $PWD.Path).Path
    $argsStr = $Arguments -join " "
    Write-Host "$Path> Git.exe $argsStr"
    $process = RunExeProcess($GitPath) ($argsStr) ($Path)
    if ($process.ExitCode -ne 0)
    {
        $processErrorMessage = $process.StandardError
        $errorMessage = "Run Git command $argsStr failed. Error: $processErrorMessage"
        ConsoleErrorAndExit ($errorMessage) ($process.ExitCode)
    }
    else
    {
        Write-Host $process.StandardOutput
    }
}

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
if (Test-Path $outputFolder)
{
    Remove-Item -Path $outputFolder -Recurse -Force
}
New-Item $outputFolder -type directory -Force
Push-Location $repoRoot

$docFxVersion = "2.32.0-alpha-0002-g57aea14"

$toolsPath = Join-Path $scriptPath "_tools"
New-Item $toolsPath -type directory -Force
$nugetPath = Join-Path $toolsPath "nuget.exe"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

if (-not (Test-Path $nugetPath))
{
    Invoke-WebRequest -Uri $nugetUrl -OutFile $nugetPath
}
$migrationExePath = Join-Path $scriptPath "MarkdownMigration.ConsoleApp.exe"

& $nugetPath install docfx.console -Version $docFxVersion -Source https://www.myget.org/F/docfx-dev/api/v3/index.json -OutputDirectory $toolsPath
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
        $markdigOutput = Join-Path $htmlBaseFolder "markdig"
        $markdigHtmlOutput = Join-Path $htmlBaseFolder "markdig-html"

        
        $docfxJsonPath = Join-Path $docsetFolder "docfx.json"
        if (-not (Test-Path $docfxJsonPath))
        {
            $docsetFolder = Join-Path $repoRoot "$($docset.build_output_subfolder)"
            $docfxJsonPath = Join-Path $docsetFolder "docfx.json"
        }

        $docfxJson = Get-Content -Raw -Path $docfxJsonPath | ConvertFrom-Json

        $dest = "_site"
        if ($docfxJson.build.dest)
        {
            $dest = Join-Path $docsetFolder $docfxJson.build.dest
        }

        robocopy $docsetFolder $tempdfmfolder *.md /s

		git checkout $dfmBranch -q
        & $docfxExePath $docfxJsonPath --exportRawModel --dryRun --force
        CheckExitCode $lastexitcode "baseline build"

        Copy-Item -Path $dest -Destination $dfmOutput -recurse -Force
        Remove-Item -path $dest -recurse
        Remove-Item -path "$docsetFolder\obj" -recurse

		git checkout $markdigBranch -q
        & $docfxExePath $docfxJsonPath --exportRawModel --dryRun --force --markdownEngineName markdig
        CheckExitCode $lastexitcode "markdig build"

        Copy-Item -Path $dest -Destination $markdigOutput -recurse -Force
        Remove-Item -path $dest -recurse
        Remove-Item -path "$docsetFolder\obj" -recurse

        & $migrationExePath -d -j "$dfmOutput,$markdigOutput" -rpf $reportDestPath -bp $tempdfmfolder
        CheckExitCode $lastexitcode "diff"

        Remove-Item -path $tempdfmfolder -recurse
        Remove-Item -path $dfmOutput -recurse
        Remove-Item -path $markdigOutput -recurse 

        $docset = [IO.File]::ReadAllText($reportDestPath) | ConvertFrom-Json
        $docset.docset_name = $docsetName
        $repoReport.docsets += $docset
    }
    
    $repoReportPath = "$outputFolder\repoReport.json"
    New-Item $reportDestPath -ItemType file
    $repoReport | ConvertTo-Json -Depth 100 | Out-File $repoReportPath
    & $migrationExePath -ge -rpf $repoReportPath -repourl $repoUrl
}
Pop-Location
