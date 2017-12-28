using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;
using MarkdownMigration.Common;

namespace MarkdownMigration.Convert
{
    public class MarkdigMarkdownRenderer : DfmMarkdownRenderer
    {
        private static HttpClient _client = new HttpClient();
        private static readonly string _requestTemplate = "https://xref.docs.microsoft.com/query?uid={0}";
        private static DfmRenderer _dfmHtmlRender = new DfmRenderer();
        private static readonly Regex _headingRegex = new Regex(@"^(?<pre> *#{1,6}(?<whitespace> *))(?<text>[^\n]+?)(?<post>(?: +#*)? *(?:\n+|$))", RegexOptions.Compiled);
        private static readonly Regex _lheading = new Regex(@"^(?<text>[^\n]+)(?<post>\n *(?:=|-){2,} *(?:\n+|$))", RegexOptions.Compiled);
        private static readonly Regex _orderListStart = new Regex(@"^(?<start>\d+)\.", RegexOptions.Compiled);
        private static readonly Regex _unorderListStart = new Regex(@"^\s*(?<start>.)", RegexOptions.Compiled);
        private static readonly Regex _incRegex = new Regex(@"(?<=\()(?<path>.+?)(?=\)\])", RegexOptions.Compiled);

        #region override default renderer
        public override StringBuffer Render(IMarkdownRenderer render, IMarkdownToken token, IMarkdownContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            if (token.LinkType is MarkdownLinkType.AutoLink &&
                token.SourceInfo.Markdown.StartsWith("<mailto:", StringComparison.OrdinalIgnoreCase) &&
                token.Content.Length == 1)
            {
                var content = token.Content.First();
                return $"<{content.SourceInfo.Markdown}>";
            }

            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            const string BlockQuoteStartString = "> ";
            const string BlockQuoteJoinString = "\n" + BlockQuoteStartString;

            var source = token.SourceInfo.Markdown;

            var content = StringBuffer.Empty;
            for (var index = 0; index < token.Tokens.Length; index++)
            {
                var t = token.Tokens[index];
                if (index == token.Tokens.Length - 1 && t is DfmVideoBlockToken videoToken)
                {
                    content += render.Render(t).ToString().TrimEnd();
                }
                else
                {
                    content += render.Render(t);
                }
            }
            var contents = content.ToString().TrimEnd('\n').Split('\n');
            content = StringBuffer.Empty;
            foreach (var item in contents)
            {
                if (content == StringBuffer.Empty)
                {
                    content += BlockQuoteStartString;
                    content += item;
                }
                else
                {
                    content += BlockQuoteJoinString;
                    content += item;
                }
            }

            var newlinesCount = Helper.CountEndNewLine(source);
            return content + new string('\n', newlinesCount);
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownHrBlockToken token, MarkdownBlockContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownNonParagraphBlockToken token, MarkdownBlockContext context)
        {
            var source = token.SourceInfo.Markdown;
            if (source.EndsWith("\n"))
            {
                return RenderInlineTokens(token.Content.Tokens, render) + "\n";
            }

            return RenderInlineTokens(token.Content.Tokens, render);
        }

        public StringBuffer Render(IMarkdownRenderer render, MarkdownTagInlineToken token, IMarkdownContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownParagraphBlockToken token, MarkdownBlockContext context)
        {
            var source = token.SourceInfo.Markdown;
            if (source.EndsWith("\n"))
            {
                return RenderInlineTokens(token.InlineTokens.Tokens, render) + "\n";
            }

            return RenderInlineTokens(token.InlineTokens.Tokens, render);
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownTableBlockToken token, MarkdownBlockContext context)
        {
            var markdown = token.SourceInfo.Markdown;
            var newLineCount = Helper.CountEndNewLine(markdown);
            if (newLineCount < 2)
            {
                return markdown + '\n';
            }
            return markdown;
        }

        #endregion

        public StringBuffer Render(IMarkdownRenderer render, MarkdownNewLineBlockToken token, MarkdownBlockContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public StringBuffer Render(IMarkdownRenderer render, DfmXrefInlineToken token, MarkdownInlineContext context)
        {
            if (token.Rule is DfmXrefShortcutInlineRule)
            {
                if (TryResolveUid(token.Href))
                {
                    return $"@\"{token.Href}\"";
                }
            }

            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, DfmIncludeBlockToken token, MarkdownBlockContext context)
        {
            return RenderIncludeToken(token.SourceInfo.Markdown);
        }

        public override StringBuffer Render(IMarkdownRenderer render, DfmIncludeInlineToken token, MarkdownInlineContext context)
        {
            return RenderIncludeToken(token.SourceInfo.Markdown);
        }

        private string RenderIncludeToken(string markdown)
        {
            return _incRegex.Replace(markdown, m => m.Groups["path"].Value.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var result = StringBuffer.Empty;
            foreach (var inline in token.Content.Tokens)
            {
                result += MarkupInlineToken(render, inline);
            }

            return result;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            if (token.Rule is MarkdownLHeadingBlockRule)
            {
                return RenderLHeadingToken(render, token, context);
            }
            else
            {
                return RenderHeadingToken(render, token, context);
            }
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownListBlockToken token, MarkdownBlockContext context)
        {
            var content = StringBuffer.Empty;

            if (!token.Ordered)
            {
                var match = _unorderListStart.Match(token.SourceInfo.Markdown);
                var startString = match.Success ? match.Groups["start"] + " " : "* ";
                for (int i = 0; i < token.Tokens.Length; ++i)
                {
                    var t = token.Tokens[i];
                    var listItemToken = t as MarkdownListItemBlockToken;
                    if (listItemToken == null)
                    {
                        throw new Exception($"token {t.GetType()} is not unordered MarkdownListItemBlockToken in MarkdownListBlockToken. Token raw:{t.SourceInfo.Markdown}");
                    }
                    content += startString;
                    if (i == token.Tokens.Length - 1)
                    {
                        content += Render(render, listItemToken, "  ", true);
                    }
                    else
                    {
                        content += Render(render, listItemToken, "  ");
                    }
                }
            }
            else
            {
                var match = _orderListStart.Match(token.SourceInfo.Markdown);
                var start = 1;
                if (match.Success)
                {
                    var value = match.Groups["start"].Value;
                    if (Int32.TryParse(value, out int result))
                    {
                        start = result;
                    }
                }

                for (int i = 0; i < token.Tokens.Length; ++i)
                {
                    var listItemToken = token.Tokens[i] as MarkdownListItemBlockToken;

                    if (listItemToken == null)
                    {
                        throw new Exception($"token {token.Tokens[i].GetType()} is not ordered MarkdownListItemBlockToken in MarkdownListBlockToken. Token raw:{token.Tokens[i].SourceInfo.Markdown}");
                    }

                    content += $"{start + i}. ";
                    string indent = new string(' ', (i + 1).ToString().Length + 2);
                    if (i == token.Tokens.Length - 1)
                    {
                        content += Render(render, listItemToken, indent, true);
                    }
                    else
                    {
                        content += Render(render, listItemToken, indent);
                    }
                }
            }

            return content;
        }

        protected StringBuffer Render(IMarkdownRenderer render, MarkdownListItemBlockToken token, string indent, bool last = false)
        {
            var content = StringBuffer.Empty;
            if (token.Tokens.Length > 0)
            {
                var tokenRenderContent = StringBuffer.Empty;
                foreach (var t in token.Tokens)
                {
                    tokenRenderContent += render.Render(t);
                }

                var lines = tokenRenderContent.ToString().Split('\n');
                content += lines[0];
                content += "\n";

                for (var index = 1; index < lines.Count(); index++)
                {
                    if (last && index == lines.Count() - 1 && string.Equals(lines[index].Trim(), string.Empty))
                    {
                        continue;
                    }

                    if (!string.Equals(lines[index].Trim(), string.Empty))
                    {
                        content += indent;
                    }

                    content += lines[index];

                    if (last && index == lines.Count() - 1)
                    {
                        continue;
                    }
                    content += "\n";
                }
            }
            return content;
        }

        private StringBuffer RenderHeadingToken(IMarkdownRenderer render, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            var source = token.SourceInfo.Markdown;

            // TODO: improve
            if (source.Contains("<a "))
            {
                return source;
            }

            var match = _headingRegex.Match(source);
            if (match.Success)
            {
                var result = StringBuffer.Empty;
                var whitespace = match.Groups["whitespace"].Value;
                var content = RenderInlineTokens(token.Content.Tokens, render);

                result += match.Groups["pre"].Value;
                if (string.IsNullOrEmpty(whitespace))
                {
                    result += " ";
                }
                result += content;
                result += match.Groups["post"].Value;

                return result;
            }

            return base.Render(render, token, context);

        }

        private StringBuffer RenderLHeadingToken(IMarkdownRenderer render, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            var source = token.SourceInfo.Markdown;
            var match = _lheading.Match(source);
            if (match.Success)
            {
                var result = RenderInlineTokens(token.Content.Tokens, render);
                result += match.Groups["post"].Value;

                return result;
            }

            return base.Render(render, token, context);
        }

        private StringBuffer MarkupInlineTokens(IMarkdownRenderer render, ImmutableArray<IMarkdownToken> tokens)
        {
            var result = StringBuffer.Empty;
            if (tokens != null)
            {
                foreach (var t in tokens)
                {
                    result += MarkupInlineToken(render, t);
                }
            }

            return result;
        }

        private StringBuffer MarkupInlineToken(IMarkdownRenderer render, IMarkdownToken token)
        {
            return _dfmHtmlRender.Render((dynamic)render, (dynamic)token, (dynamic)token.Context);
        }

        private StringBuffer RenderInlineTokens(ImmutableArray<IMarkdownToken> tokens, IMarkdownRenderer render)
        {
            var result = StringBuffer.Empty;
            var insideHtml = false;
            for (var index = 0; index < tokens.Count(); index++)
            {
                if (tokens[index] is MarkdownLinkInlineToken token && token.LinkType is MarkdownLinkType.UrlLink)
                {
                    var pre = index - 1 >= 0 ? tokens[index - 1] : null;
                    if (pre is MarkdownTextToken t && (!IsValidPreviousCharacter(t.Content.Last())))
                    {
                        result += "<" + render.Render(token) + ">";
                        continue;
                    }
                }
                else if (tokens[index] is MarkdownTagInlineToken)
                {
                    if (!string.Equals(tokens[index].SourceInfo.Markdown, "<br>"))
                    {
                        insideHtml = !insideHtml;
                    }
                    result += MarkupInlineToken(render, tokens[index]);

                    var post = index + 1 < tokens.Count() ? tokens[index + 1] : null;
                    if (post != null && !insideHtml && !(post is MarkdownTagInlineToken) && !(post is MarkdownNewLineBlockToken))
                    {
                        result += '\n';
                    }
                }
                else
                {
                    result += insideHtml ? MarkupInlineToken(render, tokens[index]) : render.Render(tokens[index]);
                }
            }

            return result;
        }

        private bool TryResolveUid(string uid)
        {
            try
            {
                var task = CanResolveUidWithRetryAsync(uid);
                return task.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured while resolving uid, {ex.Message}");
                return true;
            }
        }

        private async Task<bool> CanResolveUidWithRetryAsync(string uid)
        {
            var retryCount = 3;
            var delay = TimeSpan.FromSeconds(3);

            var count = 1;
            while (true)
            {
                try
                {
                    return await CanResolveUidAsync(uid);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error occured while resolving uid:{uid}: ${ex.Message}. Retry {count}");

                    if (count >= retryCount)
                    {
                        throw;
                    }

                    count++;
                }

                await Task.Delay(delay);
            }
        }

        private async Task<bool> CanResolveUidAsync(string uid)
        {
            var requestUrl = string.Format(_requestTemplate, Uri.EscapeDataString(uid));
            using (var response = await _client.GetAsync(requestUrl))
            {
                response.EnsureSuccessStatusCode();
                using (var content = response.Content)
                {
                    var result = await content.ReadAsStringAsync();
                    if (!string.Equals("[]", result))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsValidPreviousCharacter(char c)
        {
            // All such recognized autolinks can only come at the beginning of a line, after whitespace, or any of the delimiting characters *, _, ~, and (.
            return IsWhiteSpaceOrZero(c) || c == '*' || c == '_' || c == '~' || c == '(';
        }

        public static bool IsWhiteSpaceOrZero(char c)
        {
            return IsWhitespace(c) || IsZero(c);
        }

        public static bool IsWhitespace(char c)
        {
            // 2.1 Characters and lines 
            // A whitespace character is a space(U + 0020), tab(U + 0009), newline(U + 000A), line tabulation (U + 000B), form feed (U + 000C), or carriage return (U + 000D).
            return c == ' ' || c == '\t' || c == '\n' || c == '\v' || c == '\f' || c == '\r';
        }

        public static bool IsZero(char c)
        {
            return c == '\0';
        }
    }
}
