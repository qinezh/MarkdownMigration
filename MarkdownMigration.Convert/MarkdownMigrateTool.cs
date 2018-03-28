// Copyright (c) Microsoft. All rights reserved.
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

    public class MarkdownMigrateTool
    {
        private readonly DfmEngineBuilder _builder;
        private readonly string _workingFolder;

        public MarkdownMigrateTool(string workingFolder = ".")
        {
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = true;
            _builder = new DfmEngineBuilder(option);
            _workingFolder = workingFolder;
        }

        public void MigrateFromPattern(string cwd, List<string> patterns, List<string> excludePatterns, string outputFolder)
        {
            var files = FileGlob.GetFiles(cwd, patterns, excludePatterns).ToList();
            if (files.Count == 0)
            {
                Console.WriteLine("No file found from the glob pattern provided.");
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                Parallel.ForEach(files, file => MigrateFile(file, file));
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
                var result = Convert(File.ReadAllText(inputFile), inputFile);
                var dir = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(outputFile, result);
                Console.WriteLine($"{inputFile} has been migrated to {outputFile}.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{inputFile} migration failed.");
                throw e;
            }
        }

        public string Convert(string markdown, string inputFile)
        {
            var engine = _builder.CreateDfmEngine(new MarkdigMarkdownRendererProxy(_workingFolder));

            var normalized = TrimNewlineBeforeYamlHeader(markdown);
            normalized = RenderHTMLBlock(normalized, inputFile);

            var result = engine.Markup(normalized, inputFile);

            result = RevertNormalizedPart(result, markdown);

            return result;
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
                    { LineNumberExtension.EnableSourceInfo, false }
                }
            };

            var markdigService = new MarkdigMarkdownService(parameter);
            var markdigToken = markdigService.Parse(markdown, filepath);

            if (markdigToken == null) return markdown;

            var htmlBlockTokens = markdigToken.Where(token => token is HtmlBlock).ToList();

            if (htmlBlockTokens.Count == 0) return markdown;

            var lines = markdown.Split('\n');

            var lineIndex = 0;
            var result = new StringBuilder();

            foreach(HtmlBlock block in htmlBlockTokens)
            {
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

                //add <p></p> before content, make sure root P is always added
                var tempResult = dfmengine.Markup("<p></p>" + tempMarkdown.ToString(), filepath).TrimEnd('\n');

                if (tempResult.Length > "<p><p></p></p>".Length)
                {
                    tempResult = tempResult.Substring("<p><p></p>".Length, tempResult.Length - "<p><p></p></p>".Length);
                }
                else
                {
                    tempResult = tempResult.Substring("<p></p>".Length);
                }

                result.Append(tempResult);
            }

            while (lineIndex < lines.Count())
            {
                result.Append('\n');
                result.Append(lines[lineIndex]);
                lineIndex++;
            }

            return result.ToString();
        }

        private string TrimNewlineBeforeYamlHeader(string markdown)
        {
            markdown = NormalizeUtility.Normalize(markdown);
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

        private string RevertNormalizedPart(string result, string source)
        {
            var resultLines = NormalizeUtility.NewLine.Split(result);
            var sourceLines = NormalizeUtility.NewLine.Split(source);

            if (resultLines.Length == sourceLines.Length)
            {
                for (var index = 0; index < resultLines.Length; index++)
                {
                    var resultLine = resultLines[index];
                    var sourceLine = sourceLines[index];
                    if (string.Equals(NormalizeUtility.Normalize(sourceLine), resultLine))
                    {
                        resultLines[index] = sourceLine;
                    }
                }

                return string.Join("\n", resultLines);
            }

            return result;
        }
    }
}
