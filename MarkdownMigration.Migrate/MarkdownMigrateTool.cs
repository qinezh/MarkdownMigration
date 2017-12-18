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
    using Microsoft.DocAsCode.MarkdownLite;

    public class MarkdownMigrateTool
    {
        private readonly DfmEngineBuilder _builder;
        private readonly MarkdownRenderer _render;

        public MarkdownMigrateTool()
        {
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = true;
            _builder = new DfmEngineBuilder(option);
            _render = new MarkdigMarkdownRendererProxy();
        }

        public static void Migrate(CommandLineOptions opt)
        {
            var tool = new MarkdownMigrateTool();
            if (!string.IsNullOrEmpty(opt.FilePath))
            {
                var input = opt.FilePath;
                var output = opt.Output;
                if (string.IsNullOrEmpty(output))
                {
                    output = input;
                }
                tool.MigrateFile(input, output);
            }
            else if (opt.Patterns.Count > 0)
            {
                tool.MigrateFromPattern(opt.WorkingFolder, opt.Patterns, opt.ExcludePatterns, opt.Output);
            }
        }

        private void MigrateFromPattern(string cwd, List<string> patterns, List<string> excludePatterns, string outputFolder)
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

            var result = Convert(inputFile, File.ReadAllText(inputFile));
            var dir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(outputFile, result);
            Console.WriteLine($"{inputFile} has been migrated to {outputFile}.");
        }

        public string Convert(string inputFile, string markdown)
        {
            var engine = _builder.CreateDfmEngine(_render);
            var result = engine.Markup(markdown, inputFile);

            result = RevertNormalizedPart(result, markdown);

            return result;
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
