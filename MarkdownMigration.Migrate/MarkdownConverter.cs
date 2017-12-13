using System;
using System.IO;

using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;

namespace MarkdownMigration.Convert
{
    public class MarkdownConverter
    {
        private readonly DfmEngineBuilder _builder;
        private readonly MarkdownRenderer _render;

        private MarkdownConverter()
        {
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = true;
            _builder = new DfmEngineBuilder(option);
            _render = new MarkdigMarkdownRenderer();
        }

        private void MigrateFile(string inputFile, string outputFile)
        {
            var result = this.Convert(inputFile, File.ReadAllText(inputFile));
            var dir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(outputFile, result);
            Console.WriteLine($"{inputFile} has been migrated to {outputFile}.");
        }

        private string Convert(string inputFile, string markdown)
        {
            var engine = _builder.CreateDfmEngine(_render);
            return engine.Markup(markdown, inputFile);
        }
    }
}
