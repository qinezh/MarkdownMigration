using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCompare
{

    public class DiffRule
    {
        public Func<HtmlNode, bool> CompareChildrenOnly { get; set; }
        public Func<HtmlNode, bool> IsIgnore { get; set; } = (node) => node.InnerHtml.Trim() == string.Empty;
        public Action<HtmlNode> Process { get; set; }
        public string[] CompareAttributes { get; set; }
    }
}
