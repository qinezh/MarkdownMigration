using System;
using System.IO;
using MarkdownMigration.Convert;
using MarkdownMigration.Common;

namespace MarkdownMigration.ConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var content = File.ReadAllText("sample.md");
            var report = new MigrationReport();
            var tool = new MarkdownMigrateTool(report);
            var migratedContent = tool.Convert(content, "sample.md");

            Console.WriteLine(migratedContent);
        }
    }
}
