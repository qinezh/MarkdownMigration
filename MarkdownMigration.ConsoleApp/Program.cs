﻿using MarkdownMigration.Common;
using MarkdownMigration.Convert;
using MarkdownMigration.GenerateExcel;
using Microsoft.DocAsCode.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MarkdownMigration.ConsoleApp
{
    public class Program
    {
        private static void Main(string[] args)
        {
            var opt = new CommandLineOptions();

            try
            {
                var docsetReport = new DocsetReport();
                var repoReport = new RepoReport();

                if (opt.Parse(args))
                {
                    switch (opt.RunMode)
                    {
                        case CommandLineOptions.Mode.Migration:
                            var tool = new MarkdownMigrateTool(docsetReport, opt.WorkingFolder);
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

                            SaveMigrationReport(docsetReport, opt.WorkingFolder, "report.json");
                            break;
                        case CommandLineOptions.Mode.Diff:
                            //ExtractHtml
                            var jsonfolders = opt.JsonFolders.Split(',');
                            ExtractHtml.ExtractHtml.ExtractHtmlFromJson(jsonfolders);

                            //Diff html
                            List<string> sameFiles, allFiles;
                            HtmlCompare.HtmlCompare.CompareHtmlFromFolder(jsonfolders[0] + "-html", jsonfolders[1] + "-html", opt.JsonReportFile, opt.CompareResultPath,out sameFiles, out allFiles);
                            
                            //Update report.json
                            docsetReport = new DocsetReport();
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

        private static void SaveMigrationReport(DocsetReport report, string workingFolder, string file)
        {
            var newReport = new DocsetReport
            {
                Files = new Dictionary<string, MigrationReportItem>()
            };

            foreach (var entry in report.Files)
            {
                var relativePath = PathUtility.MakeRelativePath(workingFolder, entry.Key);
                newReport.Files.Add(relativePath, entry.Value);
            }
            var filePath = Path.Combine(workingFolder, file);
            var content = JsonConvert.SerializeObject(newReport, Formatting.Indented);
            File.WriteAllText(filePath, content);
        }

        private static void UpdateMigrationReportWithDiffResult(List<string> sameFiles, List<string> allFiles, DocsetReport migrationReport, string output)
        {
            var differentFiles = allFiles.Except(sameFiles);

            if (migrationReport == null)
            {
                migrationReport = new DocsetReport { Files = new Dictionary<string, MigrationReportItem>() };
            }

            if (migrationReport.Files == null)
            {
                migrationReport.Files = new Dictionary<string, MigrationReportItem>();
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
                migrationReport.Files[file] = new MigrationReportItem { DiffStatus = DiffStatus.OK, Migrated = false };
            }

            foreach (var file in differentFiles.Except(migrationReport.Files.Select(f => f.Key)))
            {
                migrationReport.Files[file] = new MigrationReportItem { DiffStatus = DiffStatus.BAD, Migrated = false };
            }

            File.WriteAllText(output, JsonConvert.SerializeObject(migrationReport, Formatting.Indented));
        }
    }
}
