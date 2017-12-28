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
        public Func<HtmlNode, bool> IsIgnore { get; set; } = (node) => string.IsNullOrEmpty(node.InnerHtml.LocalNomalize());
        public Action<HtmlNode> Process { get; set; }
        public string[] CompareAttributes { get; set; }
    }
}
