﻿using System.Text.RegularExpressions;
using Microsoft.DocAsCode.MarkdownLite;

namespace MarkdownMigration.Convert
{
    public class NormalizeUtility
    {

        public static readonly Regex NormalizeNewLine = new Regex(@"\r\n|\r", RegexOptions.Compiled);
        public static readonly Regex WhiteSpaceLine = new Regex(@"^ +$", RegexOptions.Compiled);
        public static readonly Regex NewLine = new Regex(@"\r\n|\r|\n", RegexOptions.Compiled);

        private static readonly string[] Spaces = { "    ", "   ", "  ", " " };
        private static readonly char[] NewLineOrTab = { '\n', '\t' };

        public static string Normalize(string line)
        {
            var result = line
                .ReplaceRegex(NormalizeNewLine, "\n")
                .Replace("\u00a0", " ")
                .Replace("\u2424", "\n");
            result = Regex.Replace(result, "\\t", m =>
            {
                if (m.Index == 0)
                {
                    return Spaces[0];
                }
                var index = result.LastIndexOfAny(NewLineOrTab, m.Index - 1);
                return Spaces[(m.Index - index - 1) % 4];
            });

            return WhiteSpaceLine.Replace(result, string.Empty);
        }

        public static string Restore(string markdown)
        {
            return markdown;
        }
    }
}
