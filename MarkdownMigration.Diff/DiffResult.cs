using MarkdownMigration.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCompare
{
    public class DiffResult
    {
        public string FileName { get; set; }
        public string DFMHtml { get; set; }
        public string MarkdigHtml { get; set; }
        public Span SourceDiffSpan { get; set; }
        public DiffStatus Status { get; set; }
    }
}
