using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MarkdownMigration.Common;
using MarkdownMigration.Convert;
using MarkdownMigration.GenerateExcel;
using Newtonsoft.Json;

namespace MarkdownMigration.ConsoleApp
{
    public class Program
    {
        private static void Main(string[] args)
        {
            TestSample();
            return;

            var opt = new CommandLineOptions();

            try
            {
                var repoReport = new RepoReport();

                if (opt.Parse(args))
                {
                    switch (opt.RunMode)
                    {
                        case CommandLineOptions.Mode.Migration:
                            var tool = new MarkdownMigrateTool(opt.WorkingFolder);
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

                            ReportUtility.Save(opt.WorkingFolder, "report.json");
                            break;
                        case CommandLineOptions.Mode.Diff:
                            //ExtractHtml
                            var jsonfolders = opt.JsonFolders.Split(',');
                            ExtractHtml.ExtractHtml.ExtractHtmlFromJson(jsonfolders);

                            //Diff html
                            List<string> sameFiles, allFiles;
                            HtmlCompare.HtmlCompare.CompareHtmlFromFolder(jsonfolders[0] + "-html", jsonfolders[1] + "-html", opt.JsonReportFile, opt.CompareResultPath,out sameFiles, out allFiles);
                            
                            //Update report.json
                            var docsetReport = new DocsetReport();
                            if (File.Exists(opt.JsonReportFile))
                            {
                                docsetReport = JsonConvert.DeserializeObject<DocsetReport>(File.ReadAllText(opt.JsonReportFile));
                            }
                            UpdateMigrationReportWithDiffResult(sameFiles, allFiles, docsetReport, opt.JsonReportFile);
                            break;
                        case CommandLineOptions.Mode.GenerateExcel:
                            try
                            {
                                repoReport = JsonConvert.DeserializeObject<RepoReport>(File.ReadAllText(opt.JsonReportFile));
                            }
                            catch (Exception)
                            {
                                throw new Exception("json file is not valid.");
                            }
                            var reportName = string.IsNullOrEmpty(repoReport.RepoName) ? "repo_report.xlsx" : repoReport.RepoName + ".xlsx";
                            var excelGenerater = new ExcelGenerater(repoReport, Path.Combine(Path.GetDirectoryName(opt.JsonReportFile), reportName));
                            excelGenerater.GenerateExcel();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void UpdateMigrationReportWithDiffResult(List<string> sameFiles, List<string> allFiles, DocsetReport migrationReport, string output)
        {
            var differentFiles = allFiles.Except(sameFiles);

            if (migrationReport == null)
            {
                migrationReport = new DocsetReport { Files = new Dictionary<string, ReportItem>() };
            }

            if (migrationReport.Files == null)
            {
                migrationReport.Files = new Dictionary<string, ReportItem>();
            }

            foreach (var reportItem in migrationReport.Files.Where(f => sameFiles.Contains(f.Key)))
            {
                reportItem.Value.DiffStatus = DiffStatus.OK;
            }

            foreach (var reportItem in migrationReport.Files.Where(f => differentFiles.Contains(f.Key)))
            {
                reportItem.Value.DiffStatus = DiffStatus.BAD;
            }

            foreach (var file in sameFiles.Except(migrationReport.Files.Select(f => f.Key)))
            {
                migrationReport.Files[file] = new ReportItem { DiffStatus = DiffStatus.OK, Migrated = false };
            }

            foreach (var file in differentFiles.Except(migrationReport.Files.Select(f => f.Key)))
            {
                migrationReport.Files[file] = new ReportItem { DiffStatus = DiffStatus.BAD, Migrated = false };
            }

            File.WriteAllText(output, JsonConvert.SerializeObject(migrationReport, Formatting.Indented));
        }

        private static void TestSample()
        {
            var content = File.ReadAllText("sample.md");
            var tool = new MarkdownMigrateTool();
            var migratedContent = tool.Convert(content, "sample.md");

            Console.WriteLine(migratedContent);
        }
    }
}
