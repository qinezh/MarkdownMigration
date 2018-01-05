using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using HtmlCompare;
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
            ServicePointManager.DefaultConnectionLimit = 64;
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
                            List<string> allFiles;
                            List<DiffResult> differentResult;
                            HtmlCompare.HtmlCompare.CompareHtmlFromFolder(jsonfolders[0] + "-html", jsonfolders[1] + "-html", out differentResult, out allFiles);
                            
                            //Update report.json
                            var docsetReport = new DocsetReport();
                            if (File.Exists(opt.JsonReportFile))
                            {
                                docsetReport = JsonConvert.DeserializeObject<DocsetReport>(File.ReadAllText(opt.JsonReportFile));
                            }
                            UpdateMigrationReportWithDiffResult(differentResult, allFiles, docsetReport, opt.JsonReportFile, opt.BasePath);
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
                            var excelGenerater = new ExcelGenerater(repoReport, Path.Combine(Path.GetDirectoryName(opt.JsonReportFile), reportName), opt.GitRepoUrl);
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

        private static void UpdateMigrationReportWithDiffResult(List<DiffResult> differentResult, List<string> allFiles, DocsetReport migrationReport, string output, string basePath)
        {
            var sameFiles = allFiles.Except(differentResult.Select(d => d.FileName));
            Dictionary<string, DiffResult> fileResultMapping = differentResult.ToDictionary(key => key.FileName, value => value);

            if (migrationReport == null)
            {
                migrationReport = new DocsetReport { Files = new Dictionary<string, ReportItem>() };
            }

            if (migrationReport.Files == null)
            {
                migrationReport.Files = new Dictionary<string, ReportItem>();
            }

            foreach (var reportItem in migrationReport.Files.Where(f => fileResultMapping.ContainsKey(f.Key)))
            {
                var singleResult = fileResultMapping[reportItem.Key];
                reportItem.Value.DiffTagName = fileResultMapping[reportItem.Key].DiffTagName;
                reportItem.Value.MarkdigHtml = singleResult.MarkdigHtml;
                reportItem.Value.DFMHtml = singleResult.DFMHtml;
                reportItem.Value.SourceStart = singleResult.SourceDiffSpan.Start;
                reportItem.Value.SourceEnd = singleResult.SourceDiffSpan.End;
                reportItem.Value.SourceMarkDown = ReadSourceMarkdown(Path.Combine(basePath, reportItem.Key), singleResult.SourceDiffSpan);
            }

            foreach (var file in sameFiles.Except(migrationReport.Files.Select(f => f.Key)))
            {
                migrationReport.Files[file] = new ReportItem {Migrated = false };
            }
            var filesInReport = migrationReport.Files.Select(f => f.Key).ToList();
            foreach (var result in differentResult.Where(dr => !filesInReport.Contains(dr.FileName)))
            {
                migrationReport.Files[result.FileName] = new ReportItem
                {
                    DiffTagName = result.DiffTagName,
                    Migrated = false,
                    MarkdigHtml = result.MarkdigHtml,
                    DFMHtml = result.DFMHtml,
                    SourceEnd = result.SourceDiffSpan.End,
                    SourceStart = result.SourceDiffSpan.Start,
                    SourceMarkDown = ReadSourceMarkdown(Path.Combine(basePath, result.FileName), result.SourceDiffSpan)
                };
            }

            File.WriteAllText(output, JsonConvert.SerializeObject(migrationReport, Formatting.Indented));
        }

        private static string ReadSourceMarkdown(string path, Span sourceLineSpan)
        {
            var lines = File.ReadAllLines(path);

            if (sourceLineSpan.Start <= 0 || sourceLineSpan.End <= 0)
                return string.Empty;
            if (sourceLineSpan.Start >= lines.Length || sourceLineSpan.End >= lines.Length)
                return string.Empty;

            var result = new StringBuilder();
            for(int index = sourceLineSpan.Start - 1; index < sourceLineSpan.End; index++)
            {
                result.AppendLine(lines[index]);
            }

            return result.ToString();
        }
    }
}
