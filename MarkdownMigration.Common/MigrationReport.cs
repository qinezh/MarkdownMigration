using System.Collections.Generic;
using Newtonsoft.Json;

namespace MarkdownMigration.Common
{
    public class MigrationReport
    {
        [JsonProperty("files")]
        public Dictionary<string, MigrationReportItem> Files { get; set; }
    }
}
