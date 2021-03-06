﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdownMigration.Convert
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.Glob;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.MarkdigEngine.Extensions;
    using Microsoft.DocAsCode.MarkdigEngine;
    using Markdig.Syntax;
    using System.Text;
    using System.Text.RegularExpressions;
    using HtmlCompare;
    using Microsoft.DocAsCode.MarkdownLite;

    public class MarkdownMigrateTool
    {
        private readonly DfmEngineBuilder _builder;
        private readonly string _workingFolder;
        private readonly bool _useLegacyMode;
        private readonly MigrationRule _rule;

        public MarkdownMigrateTool(string workingFolder = ".", bool useLegacyMode = true, MigrationRule rule = MigrationRule.All)
        {
            _useLegacyMode = useLegacyMode;
            _rule = rule;

            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = _useLegacyMode;
            _builder = new DfmEngineBuilder(option, workingFolder);
            _workingFolder = workingFolder.EndsWith("\\") || workingFolder.EndsWith("/") ? workingFolder : workingFolder + "\\";
        }

        public void MigrateFromPattern(string cwd, List<string> patterns, List<string> excludePatterns, string outputFolder)
        {
            var files = FileGlob.GetFiles(cwd, patterns, excludePatterns).ToList();
            if (files.Count == 0)
            {
                Console.WriteLine("No file found from the glob pattern provided.");
            }

            //var ph = ProgressHelper.CreateStartedInstance(files.Count(), "Migrating Files");

            if (string.IsNullOrEmpty(outputFolder))
            {
                Parallel.ForEach(files, file => 
                {
                    MigrateFile(file, file);
                    //ph.Increase();
                });
                return;
            }

            Parallel.ForEach(files, file =>
            {
                var name = Path.GetFileName(file);
                var outputFile = Path.Combine(outputFolder, name);
                MigrateFile(file, outputFile);
            });
        }

        public void MigrateFile(string inputFile, string outputFile)
        {
            if (inputFile == null)
            {
                throw new ArgumentNullException(nameof(inputFile));
            }
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"{inputFile} can't be found.");
            }

            try
            {
                var bom = new byte[3];
                using (var file = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                {
                    file.Read(bom, 0, 3);
                }

                var result = Convert(File.ReadAllText(inputFile), inputFile);
                var dir = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
                {
                    File.WriteAllBytes(outputFile, bom);
                    File.AppendAllText(outputFile, result);
                }
                else
                {
                    File.WriteAllText(outputFile, result);
                }

                Console.WriteLine($"{inputFile} has been migrated to {outputFile}.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{inputFile} migration failed.");
                throw e;
            }
        }

        public string GetReletivcePath(string filePath, string folderPath)
        {
            // Need absolute path, otherwise cause exception
            Uri file = new Uri(filePath);
            Uri folder = new Uri(folderPath);

            return Uri.UnescapeDataString(folder.MakeRelativeUri(file).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public string Convert(string markdown, string inputFile)
        {
            if (string.IsNullOrEmpty(markdown)) return markdown;

            try
            {
                var _renderer = new MarkdigMarkdownRenderer(new Stack<IMarkdownToken>(), _workingFolder, _useLegacyMode);
                if (_renderer.CompareMarkupResult(markdown, GetReletivcePath(inputFile, _workingFolder))) return markdown;
            }
            catch{ }

            var normalized = TrimNewlineBeforeYamlHeader(markdown);

            if (_rule.HasFlag(MigrationRule.Html))
            {
                normalized = RenderHTMLBlock(normalized, inputFile);
            }

            var engine = _builder.CreateDfmEngine(new MarkdigMarkdownRendererProxy(_workingFolder, _useLegacyMode, normalized.Split('\n').Count(), _rule));

            var result = engine.Markup(normalized, GetRelativePath(inputFile));

            if (_rule.HasFlag(MigrationRule.Normalize))
            {
                result = RevertNormalizedPart(result, markdown);
            }

            return result;
        }

        private string GetRelativePath(string path)
        {
            try
            {
                Uri basePath = new Uri(_workingFolder);
                Uri absolutePath = new Uri(path);

                Uri relativeUri = basePath.MakeRelativeUri(absolutePath);

                return relativeUri.ToString();
            }
            catch (Exception)
            {
                return path;
            }
        }

        private string RenderHTMLBlock(string markdown, string filepath)
        {
            //DFM engine
            var htmlRender = new DfmRenderer();
            var dfmengine = _builder.CreateDfmEngine(htmlRender);

            //Markdig engine
            var parameter = new MarkdownServiceParameters
            {
                BasePath = _workingFolder,
                Extensions = new Dictionary<string, object>
                {
                    { "EnableSourceInfo", false }
                }
            };

            var markdigService = new MarkdigMarkdownService(parameter);
            var markdigToken = markdigService.Parse(markdown, Path.GetFileName(filepath));

            if (markdigToken == null) return markdown;

            var htmlBlockTokens = markdigToken.Where(token => token is HtmlBlock).ToList();

            if (htmlBlockTokens.Count == 0) return markdown;

            var lines = markdown.Split('\n');

            var lineIndex = 0;
            var result = new StringBuilder();

            foreach(HtmlBlock block in htmlBlockTokens)
            {
                if (!_useLegacyMode && !string.IsNullOrEmpty(lines[block.Line])
                    && lines[block.Line].StartsWith("<!--"))
                {
                    continue;
                }

                var blockStart = block.Line;
                var blockEnd = block.Line + block.Lines.Count - 1;

                while(lineIndex < blockStart)
                {
                    if (lineIndex != 0) result.Append('\n');
                    result.Append(lines[lineIndex]);
                    lineIndex++;
                }

                var tempMarkdown = new StringBuilder();
                while(lineIndex <= blockEnd)
                {
                    if (lineIndex != 0)
                    {
                        if(lineIndex == blockStart)
                        {
                            result.Append('\n');
                        }
                        else
                        {
                            tempMarkdown.Append('\n');
                        }
                    }
                    tempMarkdown.Append(lines[lineIndex]);
                    lineIndex++;
                }

                // Wrap in <migratetemproot> to make sure DFM recognize it as a single Html Block
                var tempResult = dfmengine.Markup($"<migratetemproot>{tempMarkdown.ToString()}</migratetemproot>", Path.GetFileName(filepath)).TrimEnd('\n');

                if (tempResult.StartsWith("<migratetemproot>") && tempResult.EndsWith("</migratetemproot>"))
                {
                    tempResult = tempResult.Substring("<migratetemproot>".Length, tempResult.Length - "<migratetemproot></migratetemproot>".Length);
                }
                else
                {
                    tempResult = tempMarkdown.ToString();
                }

                result.Append(tempResult);
            }

            while (lineIndex < lines.Count())
            {
                if(lineIndex != 0) result.Append('\n');
                result.Append(lines[lineIndex]);
                lineIndex++;
            }

            return result.ToString();
        }

        private string TrimNewlineBeforeYamlHeader(string markdown)
        {
            if (_rule.HasFlag(MigrationRule.Normalize))
            {
                markdown = NormalizeUtility.Normalize(markdown);
            }

            if (!_rule.HasFlag(MigrationRule.YamlHeader)) return markdown;

            var lines = markdown.Split('\n');
            var index = 0;
            for(; index < lines.Count(); index++)
            {
                var line = lines[index];
                if (!string.Equals(line, string.Empty))
                {
                    break;
                }
            }

            if (index < lines.Count() && string.Equals(lines[index], "---"))
            {
                return string.Join("\n", lines.Skip(index));
            }
            else
            {
                return markdown;
            }
        }

        private string[] SplitLines(string source)
        {
            List<string> result = new List<string>();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < source.Length; i++)
            {
                sb.Append(source[i]);
                if (source[i] == '\r' && i + 1 < source.Length && source[i + 1] == '\n')
                {
                    i++;
                    sb.Append(source[i]);
                }
                if (source[i] == '\r' || source[i] == '\n')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
            }
            result.Add(sb.ToString());
            return result.ToArray();
        }

        private string NormalizeStartSpaces(string source)
        {
            StringBuilder sb = new StringBuilder();
            bool startFlag = true;
            foreach (char c in source)
            {
                if(c == ' ' || c == '\u00a0' || c == '\u200b')
                {
                    if (startFlag) sb.Append(' ');
                    else sb.Append(c);
                }
                else
                {
                    startFlag = false;
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private string RevertNormalizedPart(string result, string source)
        {
            var resultLines = new Regex("(?<=\n)").Split(result);
            var sourceLines = SplitLines(source);

            if (resultLines.Length == sourceLines.Length)
            {
                for (var index = 0; index < resultLines.Length; index++)
                {
                    var resultLine = resultLines[index];
                    var sourceLine = sourceLines[index];
                    if (string.Equals(NormalizeUtility.Normalize(sourceLine).Trim(), "```"))
                    {
                        // keep the Normalized string for the "```" line only, but use the original line ending.
                        if (sourceLine.EndsWith("\r\n") && resultLine.EndsWith("\n"))
                        {
                            resultLines[index] = resultLine.Replace("\n", "\r\n");
                        }
                    }
                    else if (string.Equals(NormalizeUtility.Normalize(sourceLine), resultLine))
                    {
                        resultLines[index] = NormalizeStartSpaces(sourceLine);
                    }
                }

                return string.Concat(resultLines);
            }

            return result;
        }
    }
}
