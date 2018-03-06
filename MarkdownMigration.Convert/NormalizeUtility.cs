using System.Text.RegularExpressions;
using Microsoft.DocAsCode.MarkdownLite;

namespace MarkdownMigration.Convert
{
    public class NormalizeUtility
    {

        public static readonly Regex NormalizeNewLine = new Regex(@"\r\n|\r", RegexOptions.Compiled);
        public static readonly Regex WhiteSpaceLine = new Regex(@"^ +$", RegexOptions.Compiled);
        public static readonly Regex NewLine = new Regex(@"\r\n|\r|\n", RegexOptions.Compiled);
        public static readonly Regex Html = new Regex(@"(?<=\n|^) *(?:<!--(?:[^-]|-(?!->))*-->|<((?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:)(?!:\/|[^\w\s@]*@)\b)[\s\S]+?<\/\1>|<(?!(?:a|em|strong|small|s|cite|q|dfn|abbr|data|time|code|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo|span|br|wbr|ins|del|img)\b)\w+(?!:\/|[^\w\s@]*@)\b(?!:)(?:""[^""]*""|'[^']*'|[^'"">])*?>) *\n*", RegexOptions.Compiled);

        private static readonly string[] Spaces = { "    ", "   ", "  ", " " };
        private static readonly char[] NewLineOrTab = { '\n', '\t' };

        public static string Normalize(string line)
        {
            var result = line
                .ReplaceRegex(NormalizeNewLine, "\n")
                .Replace("\u00a0", " ")
                .Replace("\u2424", "\n")
                .Replace("\u200b", " ");
            result = Regex.Replace(result, "\\t", m =>
            {
                if (m.Index == 0)
                {
                    return Spaces[0];
                }
                var index = result.LastIndexOfAny(NewLineOrTab, m.Index - 1);
                return Spaces[(m.Index - index - 1) % 4];
            });

            result = Html.Replace(result, m =>
            {
                var value = m.ToString();
                if (value.EndsWith("\n\n") || !value.EndsWith("\n"))
                    return value;

                return value + '\n';
            });

            return WhiteSpaceLine.Replace(result, string.Empty);
        }
    }
}
