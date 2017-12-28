using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace HtmlCompare
{
    public static class MarkdownMigrateDiffUtility
    {
        public static string LocalNomalize(this string source)
        {
            var result = source.Replace("&amp;", "&");
            result = HttpUtility.HtmlDecode(result);
            result = HttpUtility.UrlDecode(result);
            result = result.Replace(' ', ' ')
                .Replace('\t', ' ');
            result = Regex.Replace(result, "[ \n]+", m =>
            {
                if (m.Value.Contains('\n'))
                {
                    return "\n";
                }
                else
                {
                    return " ";
                }
            });

            return result.Trim();
        }
    }
}
