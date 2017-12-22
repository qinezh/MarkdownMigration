using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownMigration.Common
{
    public class RepoMigrationReport
    {
        [JsonProperty("repo_name")]
        public string RepoName { get; set; }

        [JsonProperty("docsets")]
        public List<DocsetMigrationReport> Docsets { get; set; }
    }
}
