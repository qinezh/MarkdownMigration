using System;
using Newtonsoft.Json;

namespace MarkdownMigration.Common
{
    public class MigratedTokenInfo
    {
        public MigratedTokenInfo(string name, int line)
        {
            Name = name;
            Line = line;
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("line")]
        public int Line { get; set; }
    }
}