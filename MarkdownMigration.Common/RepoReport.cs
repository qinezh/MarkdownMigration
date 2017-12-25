using System.Collections.Generic;
using Newtonsoft.Json;

namespace MarkdownMigration.Common
{
    public class RepoReport
    {
        [JsonProperty("repo_name")]
        public string RepoName { get; set; }

        [JsonProperty("docsets")]
        public List<DocsetReport> Docsets { get; set; }
    }
}
