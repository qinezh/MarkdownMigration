using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCompare
{
    public static class MarkdownMigrateDiffUtility
    {
        public static bool ComapreHtml(string htmlA, string htmlB, bool enableAllRules = false)
        {
            string migratedA, migratedB;
            return HtmlCompare.CompareMigratedHtml("", htmlA, htmlB, out migratedA, out migratedB, false, enableAllRules);
        }
    }
}
