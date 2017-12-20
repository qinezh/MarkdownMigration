using System.Collections.Generic;

namespace MarkdownMigration.Convert
{
    public static class MarkdownMigrateUtility
    {
        private static MarkdownMigrateTool _tool = new MarkdownMigrateTool(null);

        public static string Migrate(string markdown, string inputFile)
        {
            return _tool.Convert(markdown, inputFile);
        }

        public static void MigrateFile(string inputFile, string outputFile)
        {
            _tool.MigrateFile(inputFile, outputFile);
        }

        public static void MigrateFiles(IEnumerable<string> files)
        {
            foreach(var file in files)
            {
                MigrateFile(file, null);
            }
        }
    }
}
