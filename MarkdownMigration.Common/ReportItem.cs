using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace MarkdownMigration.Common
{
    public enum DiffStatus
    {
        OK,
        BAD
    }

    public class MigrationReportItem
    {
        public MigrationReportItem()
        {
        }

        [JsonProperty("diffstatus")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DiffStatus DiffStatus { get; set; }

        [JsonProperty("migrated")]
        public bool Migrated { get; set; }

        [JsonProperty("tokens")]
        public List<MigratedTokenInfo> Tokens { get; set; }
    }
}