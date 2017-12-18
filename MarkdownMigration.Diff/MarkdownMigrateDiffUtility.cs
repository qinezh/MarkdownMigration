using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCompare
{
    public static class MarkdownMigrateDiffUtility
    {
        public static bool ComapreHtml(string htmlA, string htmlB)
        {
            string migratedA, migratedB;
            return Program.CompareMigratedHtml("", htmlA, htmlB, out migratedA, out migratedB, false);
        }
    }
}
