using MarkdownMigration.Common;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MarkdownMigration.GenerateExcel
{
    public class ExcelGenerater
    {
        private const string HyperLinkStyleName = "HyperLink";
        private const string DateFormat = "yyyy-mm-dd HH:mm:ss";
        private const int MinColumnWidth = 5;
        private const int MaxColumnWidth = 40;

        public RepoReport Report { get; set; }

        public string OutputFilename { get; set; }

        public ExcelGenerater(RepoReport report, string outputFile)
        {
            this.Report = report;
            this.OutputFilename = outputFile;
        }

        public void GenerateExcel()
        {
            if (File.Exists(OutputFilename))
            {
                File.Delete(OutputFilename);
            }

            if (Report == null || Report.Docsets == null)
            {
                return;
            }

            using (var package = new ExcelPackage())
            {
                InitStyle(package);

                var overviewSheet = package.Workbook.Worksheets.Add("Overview");
                WriteOverviewSheet(overviewSheet);

                var migrationDetailSheet = package.Workbook.Worksheets.Add("MigrationDetail");
                WriteMigrationDetailSheet(migrationDetailSheet);

                var differenceAfterMigrateSheet = package.Workbook.Worksheets.Add("DifferenceAfterMigrate");
                WriteDifferenceAfterMigrateSheet(differenceAfterMigrateSheet);

                // save to file
                var file = new FileInfo(OutputFilename);
                package.SaveAs(file);
            }
        }

        private void WriteDifferenceAfterMigrateSheet(ExcelWorksheet sheet)
        {
            var contentTable = new List<IReadOnlyList<object>>();
            var title = new List<string>() {
                "Docset Name",
                "File Name",
                "Migrated",
                "SourceMarkdown",
                "DFMHtml",
                "MarkdigHtml",
                "SourceSpan",
                "Migration History(Token/ChangedLineNumber)"
            };

            contentTable.Add(title);
            foreach (var docset in Report.Docsets)
            {
                if (docset == null || docset.Files == null) continue;

                foreach (var file in docset.Files)
                {
                    if (file.Value.DiffStatus == DiffStatus.OK) continue;

                    var list = new List<object>();
                    list.Add(docset.DocsetName);
                    list.Add(file.Key);
                    list.Add(file.Value.Migrated);
                    list.Add(file.Value.SourceMarkDown);
                    list.Add(file.Value.DFMHtml);
                    list.Add(file.Value.MarkdigHtml);
                    list.Add(file.Value.SourceStart + "-" + file.Value.SourceEnd);

                    if(file.Value.Tokens != null)
                    {
                        list.Add(string.Join("\n", file.Value.Tokens.Select(token =>
                            token.Name + "/" + token.Line
                        )));
                    }

                    contentTable.Add(list);
                }
            }

            WriteToSheet(sheet, contentTable, true);
        }

        private void WriteMigrationDetailSheet(ExcelWorksheet sheet)
        {
            var contentTable = new List<IReadOnlyList<object>>();
            var title = new List<string>() {
                "Docset Name",
                "File Name",
                "Rule Name",
                "Changed LineNumber"
            };

            contentTable.Add(title);
            foreach (var docset in Report.Docsets)
            {
                if (docset == null || docset.Files == null) continue;

                foreach (var file in docset.Files)
                {
                    if (file.Value.Tokens == null) continue;

                    foreach (var token in file.Value.Tokens)
                    {
                        var list = new List<object>();
                        list.Add(docset.DocsetName);
                        list.Add(file.Key);
                        list.Add(token.Name);
                        list.Add(token.Line);

                        contentTable.Add(list);
                    }
                }
            }

            WriteToSheet(sheet, contentTable, true);
        }

        private void WriteOverviewSheet(ExcelWorksheet overviewSheet)
        {
            var validDocsets = Report.Docsets.Where(d => d.Files != null).ToList();
            var allRules = validDocsets.SelectMany(d => d.Files.Where(f => f.Value.Tokens != null).SelectMany(f => f.Value.Tokens.Select(t => t.Name))).Distinct();

            var contentTable = new List<IReadOnlyList<object>>();
            contentTable.Add(new List<string>() { "TotalFiles", validDocsets
                .Sum(d => d.Files.Count())
                .ToString() });
            contentTable.Add(new List<string>() { "TotalChangedFiles", validDocsets
                .Sum(d => d.Files.Where(f => f.Value.Migrated)
                .Count())
                .ToString() });
            contentTable.Add(new List<object>() { "TotalDifferentFiles", validDocsets.
                Sum(d => d.Files.Count(f => f.Value.DiffStatus == DiffStatus.BAD))
                .ToString() });
            contentTable.Add(new List<string>() { "TotalUsedRules", allRules.Count().ToString() });

            WriteToSheet(overviewSheet, contentTable, false);
        }

        private static void WriteToSheet(ExcelWorksheet sheet, IReadOnlyList<IReadOnlyList<object>> contentTable, bool isWithHeader)
        {
            var row = 1;
            var maxCol = 0;
            // write the body line by line
            foreach (var contentLine in contentTable)
            {
                var col = 1;
                foreach (var item in contentLine)
                {
                    if (item is HyperLinkItem)
                    {
                        var linkItem = item as HyperLinkItem;
                        try
                        {
                            sheet.Cells[row, col].Hyperlink = new ExcelHyperLink(linkItem.Link) { Display = linkItem.Display };
                            sheet.Cells[row, col].StyleName = HyperLinkStyleName;
                        }
                        catch (UriFormatException)
                        {
                            // if the hyperlink is not a valid one, fallback to normal text and lose link
                            sheet.Cells[row, col].Value = linkItem.Display;
                        }
                    }
                    else if (item is DateTime)
                    {
                        var dateItem = (DateTime)item;
                        sheet.Cells[row, col].Style.Numberformat.Format = DateFormat;
                        sheet.Cells[row, col].Value = dateItem.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        // normal text
                        sheet.Cells[row, col].Value = item == null ? string.Empty : item.ToString();
                    }
                    col++;
                }
                maxCol = col > maxCol ? col : maxCol;
                row++;
            }

            if (isWithHeader && row > 1)
            {
                sheet.Tables.Add(new ExcelAddressBase(1, 1, row - 1, maxCol - 1), sheet.Name);
            }

            // auto fit column width
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns(MinColumnWidth, MaxColumnWidth);
        }

        private void InitStyle(ExcelPackage package)
        {
            var namedStyle = package.Workbook.Styles.CreateNamedStyle(HyperLinkStyleName);
            namedStyle.Style.Font.UnderLine = true;
            namedStyle.Style.Font.Color.SetColor(Color.CornflowerBlue);
        }

        private class HyperLinkItem
        {
            internal readonly string Display;
            internal readonly string Link;

            internal HyperLinkItem(string display, string link)
            {
                this.Display = display ?? string.Empty;
                this.Link = link ?? string.Empty;
            }
        }
    }
}
