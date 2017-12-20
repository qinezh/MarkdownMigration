using Newtonsoft.Json;
using System.Collections.Generic;

namespace MarkdownMigration.Common
{
    public class MigrationReportItem
    {
        public MigrationReportItem(MigratedTokenInfo tokenInfo)
        {
            Migrated = true;
            Tokens = new List<MigratedTokenInfo> { tokenInfo };
        }

        [JsonProperty("migrated")]
        public bool Migrated { get; set; }

        [JsonProperty("tokens")]
        public List<MigratedTokenInfo> Tokens { get; set; }
    }
}