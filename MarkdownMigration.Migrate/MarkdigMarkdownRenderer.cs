using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;

namespace MarkdownMigration.Convert
{
    public class MarkdigMarkdownRenderer : MarkdownRenderer
    {
        private static HttpClient _client = new HttpClient();
        private static readonly string _requestTemplate = "https://xref.docs.microsoft.com/query?uid={0}";
        private static DfmRenderer _dfmHtmlRender = new DfmRenderer();
        private static readonly Regex _headingRegex = new Regex(@"^(?<pre> *#{1,6}(?<whitespace> *))(?<text>[^\n]+?)(?<post>(?: +#*)? *(?:\n+|$))", RegexOptions.Compiled);
        private static readonly Regex _lheading = new Regex(@"^(?<text>[^\n]+)(?<post>\n *(?:=|-){2,} *(?:\n+|$))", RegexOptions.Compiled);

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
            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownListBlockToken token, MarkdownBlockContext context)
        {
            return token.SourceInfo.Markdown;
        }

        protected override StringBuffer Render(IMarkdownRenderer render, MarkdownListItemBlockToken token, string indent)
        {
            return token.SourceInfo.Markdown;
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

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var result = StringBuffer.Empty;
            var inside = false;
            foreach (var inline in token.Content.Tokens)
            {
                if (inline is MarkdownTagInlineToken)
                {
                    inside = !inside;
                    result += MarkupInlineToken(render, inline);
                }
                else
                {
                    result += inside ? MarkupInlineToken(render, inline)
                                     : Render(render, inline, inline.Context);
                }
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
            for (var index = 0; index < tokens.Count(); index++)
            {
                if (tokens[index] is MarkdownLinkInlineToken token && token.LinkType is MarkdownLinkType.UrlLink)
                {
                    var pre = index - 1 >= 0 ? tokens[index - 1] : null;
                    if (pre is MarkdownTextToken t && (t.Content.EndsWith("&quot;") || t.Content.EndsWith("&#39;")))
                    {
                        result += "<" + render.Render(token) + ">";
                        continue;
                    }
                }
                result += render.Render(tokens[index]);
            }

            return result;
        }

        private bool TryResolveUid(string uid)
        {
            var task = CanResolveUidWithRetryAsync(uid);
            return task.Result;
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
    }
}
