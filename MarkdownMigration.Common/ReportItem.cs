﻿using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarkdownMigration.Common
{
    public class ReportItem
    {
        public ReportItem()
        {
        }

        public ReportItem(MigratedTokenInfo tokenInfo)
        {
            Migrated = true;
            Tokens = new List<MigratedTokenInfo> { tokenInfo };
        }

        [JsonProperty("diff_tag_name")]
        public string DiffTagName { get; set; }

        [JsonProperty("migrated")]
        public bool Migrated { get; set; }

        [JsonProperty("tokens")]
        public List<MigratedTokenInfo> Tokens { get; set; }

        [JsonProperty("source_start")]
        public int SourceStart { get; set; }

        [JsonProperty("source_end")]
        public int SourceEnd { get; set; }

        [JsonProperty("source_md")]
        public string SourceMarkDown { get; set; }

        [JsonProperty("dfm_html")]
        public string DFMHtml { get; set; }

        [JsonProperty("markdig_html")]
        public string MarkdigHtml { get; set; }
    }
}