// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdownMigration.Convert
{
    using System;
    using System.Collections.Generic;

    using Mono.Options;

    public class CommandLineOptions
    {
        public enum Mode
        {
            Migration,
            Diff
        }


        public string RendererName { get; private set; } = "Markdig";
        public string Output { get; private set; }
        public List<string> Patterns { get; private set; } = new List<string>();
        public List<string> ExcludePatterns { get; private set; } = new List<string>();
        public string FilePath { get; private set; }
        public string WorkingFolder { get; private set; }
        public Mode RunMode { get; set; }
        public string JsonFolders { get; set; }
        public string JsonReportFile { get; set; }
        public string CompareResultPath { get; set; }

        OptionSet _options = null;

        public CommandLineOptions()
        {
            _options = new OptionSet {
                { "r|rendererName=", "the renderer name to migrate markdown", r => RendererName = r },
                { "o|output=", "the output file or folder to save migrated markdown contents", o => Output = o },
                { "f|file=", "the path of file that needed to be migrated", f => FilePath = f },
                { "p|patterns=", "the glob pattern to find markdown files", p => Patterns.Add(p)},
                { "e|excludePatterns=", "the glob pattern to exclude markdown files", e => ExcludePatterns.Add(e)},
                { "c|cwd=", "the root path using for glob pattern searching", c => WorkingFolder = c },
                { "m|migration", "run migration mode", (m) => RunMode = Mode.Migration },
                { "d|diff", "run diff mode", (d) => RunMode = Mode.Diff },
                { "j|jsonfolders=", "difffolders, split compare json folders with comma", (j) => JsonFolders = j },
                { "rpf|reportFile=", "json report file path", (rpf) => JsonReportFile = rpf },
                { "crp|compareResultPath=", "this path is used to store diff result", (crp) => CompareResultPath = crp },
            };
        }

        public bool Parse(string[] args)
        {
            _options.Parse(args);

            if (Patterns.Count > 0)
            {
                if (string.IsNullOrEmpty(WorkingFolder))
                {
                    Console.WriteLine("The root path using for glob pattern searching need to be provided with `-c` option");
                    return false;
                }
            }

            return true;
        }

        private void PrintUsage()
        {
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} <Options>");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }
}
