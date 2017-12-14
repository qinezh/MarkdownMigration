$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$nugetPath = "_tools/nuget.exe"
$bathFolder = "F:\test"
New-Item _tools -type directory -Force
if (-not (Test-Path $nugetPath))
{
    Invoke-WebRequest -Uri $nugetUrl -OutFile $nugetPath
}

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
        $htmlBathFolder = Join-Path $bathFolder $docsetName
        $docfxJsonPath = Join-Path $pwd.Path "$($docset.build_source_folder)\docfx.json"
        $outPutBathPath = Split-Path -parent $docfxJsonPath        
        $dfmOutput = $docsetName + "_dfm"
        $dfmOutput = Join-Path $htmlBathFolder $dfmOutput
        $markdigOutput = $docsetName + "_markdig"
        $markdigOutput = Join-Path $htmlBathFolder $markdigOutput
        $docfxJson = Get-Content -Raw -Path $docfxJsonPath | ConvertFrom-Json
        $dest = "_site"
        if ($docfxJson.build.dest)
        {
            $dest = Join-Path $outPutBathPath $docfxJson.build.dest
        }

        & $docfxExePath $docfxJsonPath --exportRawModel --dryRun --force --markdownEngineName dfm 
        Copy-Item -Path $dest -Destination $dfmOutput -recurse -Force
        Remove-Item –path $dest –recurse
        Remove-Item –path "$outPutBathPath\obj" –recurse

        & "F:\MarkdownMigration\MarkdownMigration\MarkdownMigration.Migrate\bin\Debug\net461\MarkdownMigration.Convert.exe" -c $pwd -p "**.md"

        & $docfxExePath $docfxJsonPath --exportRawModel --dryRun --force --markdownEngineName markdig
        Copy-Item -Path $dest -Destination $markdigOutput -recurse -Force
        Remove-Item –path $dest –recurse
        Remove-Item –path "$outPutBathPath\obj" –recurse

        & "F:\MarkdownMigration\MarkdownMigration\MarkdownMigration.ExtractHtml\bin\Debug\ExtractHtml.exe" "$dfmOutput,$markdigOutput"
        $dfmOutput = $dfmOutput + "-html"
        $markdigOutput =$markdigOutput + "-html"

        if ((Test-Path $dfmOutput) -and (Test-Path $markdigOutput))
        {
           & "F:\MarkdownMigration\MarkdownMigration\MarkdownMigration.Diff\bin\Debug\HtmlCompare.exe" $dfmOutput $markdigOutput $htmlBathFolder "$htmlBathFolder\Compare"
        }
    }
}