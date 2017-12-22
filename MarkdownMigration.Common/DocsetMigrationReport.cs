using System.Collections.Generic;
using Newtonsoft.Json;

namespace MarkdownMigration.Common
{
    public class DocsetMigrationReport
    {
        [JsonProperty("docset_name")]
        public string DocsetName { get; set; }

        [JsonProperty("files")]
        public Dictionary<string, MigrationReportItem> Files { get; set; }
    }
}
