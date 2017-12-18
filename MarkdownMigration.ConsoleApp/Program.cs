using System;
using System.IO;
using MarkdownMigration.Convert;

namespace MarkdownMigration.ConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var content = File.ReadAllText("sample.md");
            var migratedContent = MarkdownMigrateUtility.Migrate(content, "sample.md");

            Console.WriteLine(migratedContent);
        }
    }
}
