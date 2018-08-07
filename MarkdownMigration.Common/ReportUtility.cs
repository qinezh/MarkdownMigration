using System.Collections.Generic;
using System.IO;

using Microsoft.DocAsCode.Common;
using Newtonsoft.Json;

namespace MarkdownMigration.Common
{
    public static class ReportUtility
    {
        private static DocsetReport _report;
        private static object _sync = new object();

        static ReportUtility()
        {
            _report = new DocsetReport
            {
                Files = new Dictionary<string, ReportItem>()
            };
        }

        public static void Add(string file, MigratedTokenInfo tokenInfo)
        {
            lock(_sync)
            {
                if (_report.Files.TryGetValue(file, out ReportItem item))
                {
                    item.Tokens.Add(tokenInfo);
                }
                else
                {
                    var reportItem = new ReportItem(tokenInfo);
                    _report.Files.Add(file, reportItem);
                }
            }
        }

        public static void Save(string workingFolder, string file, string docsetFolder = "")
        {
            var newReport = new DocsetReport
            {
                Files = new Dictionary<string, ReportItem>()
            };

            foreach (var entry in _report.Files)
            {
                var relativePath = Path.Combine(docsetFolder, entry.Key);

                newReport.Files.Add(relativePath, entry.Value);
            }
            var filePath = Path.Combine(workingFolder, file);
            var content = JsonConvert.SerializeObject(newReport, Formatting.Indented);
            File.WriteAllText(filePath, content);
        }
    }
}
