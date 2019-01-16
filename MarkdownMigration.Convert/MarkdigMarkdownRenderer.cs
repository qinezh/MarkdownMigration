using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using HtmlCompare;
using MarkdownMigration.Common;
using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdigEngine;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Microsoft.DocAsCode.MarkdownLite;
using Microsoft.DocAsCode.Plugins;
using System.Globalization;
using System.Net.Http;
using Microsoft.DocAsCode.Common;

namespace MarkdownMigration.Convert
{
    public class MarkdigMarkdownRenderer : DfmMarkdownRenderer
    {
        private static ThreadLocal<HttpClient> _client = new ThreadLocal<HttpClient>(() => new HttpClient());
        private static readonly string _requestTemplate = "https://xref.docs.microsoft.com/query?uid={0}";
        private readonly DfmHtmlRender _dfmHtmlRender;
        private static readonly Regex _headingRegex = new Regex(@"^(?<pre> *#{1,6}(?<whitespace> *))(?<text>[^\n]+?)(?<post>(?: +#*)? *(?:\n+|$))", RegexOptions.Compiled);
        private static readonly Regex _lheading = new Regex(@"^(?<text>[^\n]+)(?<post>\n *(?:=|-){2,} *(?:\n+|$))", RegexOptions.Compiled);
        private static readonly Regex _orderListItem = new Regex(@"^( *)((?:[*+-]|\d+\.)) [^\n]*(?:\n(?!\1(?:[*+-]|\d+\.) )[^\n]*)*", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex _orderListStart = new Regex(@"^\s*(?<start>\d+)\.", RegexOptions.Compiled);
        private static readonly Regex _unorderListStart = new Regex(@"^\s*(?<start>.)", RegexOptions.Compiled);
        private static readonly Regex _incRegex = new Regex(@"(?<=\()(?<path>.+?)(?=\)\])", RegexOptions.Compiled);
        private static readonly Regex _whitespaceInNormalLinkregex = new Regex(@"(?<=\]) (?=\(.+?\))", RegexOptions.Compiled);
        private static readonly Regex _fenceCodeRegex = new Regex(@"(?<pre> *`{3,}\w*\n)(?<code>[\s\S]+?)(?<post>\n *`{3,}\n?)", RegexOptions.Compiled);
        private static readonly Regex _tagName = new Regex(@"\<(\/?[a-zA-Z1-9]+)", RegexOptions.Compiled);
        private static readonly Regex _strongRegex = new Regex(@"<strong>(.*?)</strong>", RegexOptions.Compiled);
        private static readonly Regex _emRegex = new Regex(@"<em>(.*?)</em>", RegexOptions.Compiled);

        private static readonly char[] punctuationExceptions = { '−', '-', '†', '‡' };
        private static readonly char NewLine = '\n';

        private Microsoft.DocAsCode.MarkdownLite.MarkdownEngine _dfmEngine;
        private MarkdigMarkdownService _service;
        private Stack<IMarkdownToken> _processedBlockTokens;
        private MigrationRule _rule;

        public MarkdigMarkdownRenderer(Stack<IMarkdownToken> processedBlockTokens, string basePath, bool useLegacyMode = true, MigrationRule rule = MigrationRule.All)
        {
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = useLegacyMode;
            var builder = new DfmEngineBuilder(option);
            var render = new DfmRenderer();
            _dfmEngine = builder.CreateDfmEngine(render);
            _dfmHtmlRender = new DfmHtmlRender(useLegacyMode);
            _rule = rule;

            var parameter = new MarkdownServiceParameters
            {
                BasePath = basePath,
                Extensions = new Dictionary<string, object>
                {
                    { "EnableSourceInfo", false }
                }
            };
            _service = new MarkdigMarkdownService(parameter);
            _processedBlockTokens = processedBlockTokens;
        }

        public bool CompareMarkupResult(string markdown, string file = "topic.md")
        {
            try
            {
                var dfmHtml = _dfmEngine.Markup(markdown, file);
                var markdigHtml = _service.Markup(markdown, file).Html;

                var compareTool = new HtmlDiffTool(dfmHtml, markdigHtml, true);
                return compareTool.Compare();
            }
            catch (Exception)
            {
                // TODO
                return false;
            }
        }

        #region override default renderer
        public override StringBuffer Render(IMarkdownRenderer render, IMarkdownToken token, IMarkdownContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            if (string.IsNullOrEmpty(token.Title))
            {
                return "![" + token.Text + "](" + RenderHref(token.Href) + ")";
            }
            else
            {
                return "![" + token.Text + "](" + RenderHref(token.Href) + " \"" + token.Title + "\")";
            }
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            switch (token.LinkType)
            {
                case MarkdownLinkType.AutoLink:
                    return RenderAutoLink(token);
                case MarkdownLinkType.NormalLink:
                    return RenderNormalLink(token, render);
                default:
                    return token.SourceInfo.Markdown;
            }
        }

        private string RenderHref(string href)
        {
            // dfm href is already unescaped by: Regex(@"\\([\\`*{}\[\]()#+\-.!_>@])")
            // may need to escape any of CommonMark full characters: !"#$%&'()*+,-./:;<=>?@[]^_`{|}~
            var result = href.Replace("\\", "/").Replace(" ", "%20");

            return result;
        }

        private StringBuffer RenderNormalLink(MarkdownLinkInlineToken token, IMarkdownRenderer render)
        {
            if(string.IsNullOrEmpty(token.Title))
            {
                return "[" + RenderInlineTokens(token.Content, render) + "](" + RenderHref(token.Href) + ")";
            }
            else
            {
                return "[" + RenderInlineTokens(token.Content, render) + "](" + RenderHref(token.Href) + " \"" + token.Title + "\")";
            }
        }

        private StringBuffer RenderAutoLink(MarkdownLinkInlineToken token)
        {
            var markdown = token.SourceInfo.Markdown;
            if (markdown.StartsWith("<mailto:", StringComparison.OrdinalIgnoreCase) &&
                token.Content.Length == 1)
            {
                var content = token.Content.First();
                return $"<{content.SourceInfo.Markdown}>";
            }

            return markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public StringBuffer Render(IMarkdownRenderer render, MarkdownCodeBlockToken token, MarkdownBlockContext context)
        {
            var markdown = token.SourceInfo.Markdown;

            if (token.Rule is MarkdownCodeBlockRule)
            {
                var newlineCount = Helper.CountEndNewLine(markdown);
                if (_processedBlockTokens != null && _processedBlockTokens.Count > 0)
                {
                    var preToken = _processedBlockTokens.Peek();
                    if (preToken is MarkdownListBlockToken)
                    {
                        return $"~~~\n{token.Code}\n~~~" + new string('\n', newlineCount);
                    }
                }
            }

            return _fenceCodeRegex.Replace(markdown, m =>
            {
                return m.Groups["pre"].Value + m.Groups["code"].Value.TrimEnd('\n') + m.Groups["post"].Value;
            });
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

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            var source = token.SourceInfo.Markdown;
            var strongDelimiter = token.SourceInfo.Markdown.Substring(0, 2);
            var result = strongDelimiter + RenderInlineTokens(token.Content, render) + strongDelimiter;
            if (source.EndsWith("\n"))
            {
                return result + "\n";
            }

            return result;
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            var source = token.SourceInfo.Markdown;
            var emDelimiter = source.Substring(0, 1);
            var result = emDelimiter + RenderInlineTokens(token.Content, render) + emDelimiter;
            if (source.EndsWith("\n"))
            {
                return result + "\n";
            }

            return result;
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
            var tokens = token.InlineTokens.Tokens;

            if (source.EndsWith("\n"))
            {
                return RenderInlineTokens(tokens, render) + "\n";
            }

            return RenderInlineTokens(tokens, render);
        }

        private StringBuffer AddIndentForEachLine(string indent, StringBuffer content)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(indent)) return content;

            var lines = content.ToString().Split(NewLine);
            for(int index = 0; index < lines.Length; index++)
            {
                lines[index] = indent + lines[index];
            }

            return string.Join(NewLine.ToString(), lines);
        }

        private StringBuffer BuildRowExtension(IMarkdownRenderer render, MarkdownTableBlockToken token)
        {
            var result = StringBuffer.Empty;

            const int SpaceCount = 4;
            var columnCount = token.Header.Length;

            //Heading
            result += ":::row:::" + NewLine;
            for (var column = 0; column < columnCount; column++)
            {
                var header = token.Header[column];
                result += new string(' ', SpaceCount) + ":::column:::" + NewLine;
                result += AddIndentForEachLine(new string(' ', SpaceCount * 2), RenderInlineTokens(header.Content.Tokens, render)) + NewLine;
                result += new string(' ', SpaceCount) + ":::column-end:::" + NewLine;
            }
            result += ":::row-end:::" + NewLine;
            result += "* * *" + NewLine;

            //Body
            for (var row = 0; row < token.Cells.Length; row++)
            {
                var cells = token.Cells[row];

                result += ":::row:::" + NewLine;
                for (var column = 0; column < cells.Count(); column++)
                {
                    var cell = cells[column];
                    result += new string(' ', SpaceCount) + ":::column:::" + NewLine;
                    result += AddIndentForEachLine(new string(' ', SpaceCount * 2), RenderInlineTokens(cell.Content.Tokens, render)) + NewLine;
                    result += new string(' ', SpaceCount) + ":::column-end:::" + NewLine;
                }
                result += ":::row-end:::" + NewLine;

                if(row != token.Cells.Length - 1)
                {
                    result += "* * *" + NewLine;
                }
            }

            var markdown = token.SourceInfo.Markdown;
            var newLineCount = Helper.CountEndNewLine(markdown);

            if (newLineCount >= 2)
            {
                return result += NewLine;
            }

            return result;
        }

        private bool IsTableContainCodesnippet(MarkdownTableBlockToken token)
        {
            return token.Cells.Any(c => c.Any(item => item.Content.Tokens.Any(t => t is DfmFencesBlockToken)));
        }

        public override StringBuffer Render(IMarkdownRenderer render, MarkdownTableBlockToken token, MarkdownBlockContext context)
        {
            if(IsTableContainCodesnippet(token))
            {
                return BuildRowExtension(render, token);
            }

            var markdown = token.SourceInfo.Markdown;
            var newLineCount = Helper.CountEndNewLine(markdown);

            if (CompareMarkupResult("\n" + markdown) && newLineCount >= 2)
            {
                return "\n" + markdown;
            }

            const int SpaceCount = 2;
            var rowCount = token.Cells.Length + 2;
            var columnCount = token.Header.Length;
            var maxLengths = new int[columnCount];
            var matrix = new StringBuffer[rowCount, columnCount];

            for (var column = 0; column < columnCount; column++)
            {
                var header = token.Header[column];
                var content = RenderInlineTokens(header.Content.Tokens, render, true);
                matrix[0, column] = content;
                maxLengths[column] = Math.Max(1, content.GetLength()) + SpaceCount;
            }

            for (var row = 0; row < token.Cells.Length; row++)
            {
                var cell = token.Cells[row];
                for (var column = 0; column < columnCount; column++)
                {
                    var item = cell[column];
                    var content = RenderInlineTokens(item.Content.Tokens, render, true);
                    matrix[row + 2, column] = content;
                    maxLengths[column] = Math.Max(maxLengths[column], content.GetLength() + SpaceCount);
                }
            }

            for (var column = 0; column < columnCount; column++)
            {
                var align = token.Align[column];
                switch (align)
                {
                    case Align.NotSpec:
                        matrix[1, column] = "---";
                        break;
                    case Align.Left:
                        matrix[1, column] = ":--";
                        break;
                    case Align.Right:
                        matrix[1, column] = "--:";
                        break;
                    case Align.Center:
                        matrix[1, column] = ":-:";
                        break;
                    default:
                        throw new NotSupportedException($"align:{align} doesn't support in GFM table");
                }
            }

            var result = BuildTable(matrix, maxLengths, rowCount, columnCount);

            if (_processedBlockTokens != null && _processedBlockTokens.Count > 0)
            {
                var preToken = _processedBlockTokens.Peek();
                var preTokenNewLinecount = Helper.CountEndNewLine(preToken.SourceInfo.Markdown);
                if (preTokenNewLinecount < 2)
                {
                    return '\n' + result;
                }
            }

            return result;
        }

        private StringBuffer BuildTable(StringBuffer[,] matrix, int[] maxLenths, int rowCount, int nCol)
        {
            var content = StringBuffer.Empty;
            for (var row = 0; row < rowCount; row++)
            {
                content += "|";
                for (var j = 0; j < nCol; j++)
                {
                    var align = matrix[1, j];
                    if (row == 1)
                    {
                        content += BuildAlign(align, maxLenths[j]);
                    }
                    else
                    {
                        content += BuildItem(align, matrix[row, j], maxLenths[j]);
                    }
                    content += "|";
                }
                content += "\n";
            }

            return content + "\n";
        }

        private string BuildAlign(StringBuffer align, int maxLength)
        {
            switch (align)
            {
                case "---":
                    return new string('-', maxLength);
                case ":--":
                    return ":" + new string('-', maxLength - 1);
                case "--:":
                    return new string('-', maxLength - 1) + ":";
                case ":-:":
                    return ":" + new string('-', maxLength - 2) + ":";
                default:
                    throw new NotSupportedException($"align:{align} doesn't support in GFM table");
            }
        }

        private StringBuffer BuildItem(StringBuffer align, StringBuffer value, int maxLength)
        {
            var length = value.GetLength();
            var totalPad = maxLength - value.GetLength();

            switch (align)
            {
                case "---":
                case ":-:":
                    var leftPad = totalPad / 2;
                    return BuildItem(value, leftPad, totalPad - leftPad);
                case ":--":
                    return BuildItem(value, 1, totalPad - 1);
                case "--:":
                    return BuildItem(value, totalPad - 1, 1);
                default:
                    throw new NotSupportedException($"align:{align} doesn't support in GFM table");
            }
        }

        private StringBuffer BuildItem(StringBuffer value, int leftPad, int rightPad)
        {
            var leftValue = leftPad == 1 ? " " : new string(' ', leftPad);
            var rightValue = rightPad == 1 ? " " : new string(' ', rightPad);
            return StringBuffer.Empty + leftValue + value + rightValue;
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

        public override StringBuffer Render(IMarkdownRenderer render, DfmNoteBlockToken token, MarkdownBlockContext context)
        {
            return $"[!{token.NoteType}]\n";
        }

        public override StringBuffer Render(IMarkdownRenderer render, DfmIncludeBlockToken token, MarkdownBlockContext context)
        {
            var src = token.Src.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (!string.Equals(src, token.Src))
            {
                return string.IsNullOrEmpty(token.Title)
                        ? $"[!INCLUDE [{token.Name}]({src})]\n\n"
                        : $"[!INCLUDE [{token.Name}]({src} \"{token.Title}\")]\n\n";
            }

            return token.SourceInfo.Markdown;
        }

        public override StringBuffer Render(IMarkdownRenderer render, DfmIncludeInlineToken token, MarkdownInlineContext context)
        {
            var src = token.Src.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (!string.Equals(src, token.Src))
            {
                return string.IsNullOrEmpty(token.Title)
                    ? $"[!INCLUDE [{token.Name}]({src})]"
                    : $"[!INCLUDE [{token.Name}]({src} \"{token.Title}\")]";
            }

            return token.SourceInfo.Markdown;
        }

        public StringBuffer Render(IMarkdownRenderer render, DfmFencesBlockToken token, MarkdownBlockContext context)
        {
            var markdown = token.SourceInfo.Markdown;
            var originPath = token.Path;
            var path = originPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!string.Equals(path, originPath))
            {
                return markdown.Replace(originPath, path);
            }

            return base.Render(render, token, context);
        }

        public StringBuffer Render(IMarkdownRenderer render, DfmFencesBlockToken token, MarkdownInlineContext context)
        {
            var markdown = token.SourceInfo.Markdown;
            var originPath = token.Path;
            var path = originPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var result = StringBuffer.Empty;

            if (!string.Equals(path, originPath))
            {
                result = markdown.Replace(originPath, path);
            }
            else
            {
                result = base.Render(render, token, context);
            }

            if (token.Rule.Name == "DfmFencesInline")
            {
                return NewLine + result + NewLine;
            }

            return result;
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
                // in dfm, start always be 1:
                // if orignal markdown starts with "1.", keep the orignal numbers
                // if not, migrate them to "1"
                var matches = token.SourceInfo.Markdown.Match(_orderListItem);
                var starts = matches.Select( item => {
                    var match = _orderListStart.Match(item);
                    if (match.Success)
                    {
                        var value = match.Groups["start"].Value;
                        if (Int32.TryParse(value, out int result))
                        {
                            return (int?)result;
                        }
                    }
                    return null;
                }).Where(item=>item != null).Cast<int>().ToArray();

                if (starts.Count() != token.Tokens.Length || starts.FirstOrDefault() != 1) starts = null;

                for (int i = 0; i < token.Tokens.Length; ++i)
                {
                    var listItemToken = token.Tokens[i] as MarkdownListItemBlockToken;

                    if (listItemToken == null)
                    {
                        throw new Exception($"token {token.Tokens[i].GetType()} is not ordered MarkdownListItemBlockToken in MarkdownListBlockToken. Token raw:{token.Tokens[i].SourceInfo.Markdown}");
                    }

                    var number = starts?[i] ?? 1;
                    content += $"{number}. ";
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

                for (var index = 0; index < lines.Count(); index++)
                {
                    if (last && index == lines.Count() - 1 && string.Equals(lines[index].Trim(), string.Empty))
                    {
                        continue;
                    }

                    if (!string.Equals(lines[index].Trim(), string.Empty) && index > 0)
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

            var match = _headingRegex.Match(source);
            if (match.Success)
            {
                var result = StringBuffer.Empty;
                var whitespace = match.Groups["whitespace"].Value;
                var text = match.Groups["text"].Value;
                result += match.Groups["pre"].Value;

                if (string.IsNullOrEmpty(whitespace))
                {
                    result += " ";
                }

                if (text.StartsWith("<a "))
                {
                    result += text;
                }
                else
                {
                    result += RenderInlineTokens(token.Content.Tokens, render);
                }
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
            return _dfmHtmlRender.Render((dynamic)_dfmHtmlRender, (dynamic)token, (dynamic)token.Context);
        }

        private bool CheckInlineRuleEnabled(IMarkdownToken token)
        {
            if (_rule == MigrationRule.All) return true;

            if (token is DfmXrefInlineToken && _rule.HasFlag(MigrationRule.Xref)) return true;
            if (token is DfmIncludeInlineToken && _rule.HasFlag(MigrationRule.InclusionInline)) return true;
            if (token is MarkdownImageInlineToken && _rule.HasFlag(MigrationRule.Image)) return true;
            if (token is MarkdownLinkInlineToken link && _rule.HasFlag(MigrationRule.Link)
                && link.LinkType == MarkdownLinkType.NormalLink) return true;
            if (token is MarkdownStrongInlineToken && _rule.HasFlag(MigrationRule.Strong)) return true;
            if (token is MarkdownEmInlineToken && _rule.HasFlag(MigrationRule.Em)) return true;
            if (token is MarkdownTableBlockToken && _rule.HasFlag(MigrationRule.Table)) return true;

            return false;
        }

        private StringBuffer RenderInlineTokens(ImmutableArray<IMarkdownToken> tokens, IMarkdownRenderer render, bool inSideTable = false)
        {
            var result = StringBuffer.Empty;
            var tags = new Stack<string>();
            var localTokens = tokens.ToList();

            for (var index = 0; index < localTokens.Count(); index++)
            {
                var pre = index - 1 >= 0 ? localTokens[index - 1] : null;
                var post = index + 1 < localTokens.Count() ? localTokens[index + 1] : null;
                if (!CheckInlineRuleEnabled(localTokens[index]))
                {
                    result += localTokens[index].SourceInfo.Markdown;
                }
                else if (localTokens[index] is DfmIncludeInlineToken include)
                {
                    var temp = render.Render(include);
                    var filePath = ((RelativePath)include.Src).BasedOn((RelativePath)include.SourceInfo.File).RemoveWorkingFolder();
                    if (EnvironmentContext.FileAbstractLayer.Exists(filePath))
                    {
                        var content = EnvironmentContext.FileAbstractLayer.ReadAllText(filePath);
                        if(content.StartsWith(" ") && pre is MarkdownTextToken pret && !pret.Content.EndsWith(" "))
                        {
                            temp = " " + temp;
                        }
                    }
                    result += temp;
                }
                else if (localTokens[index] is MarkdownLinkInlineToken token && token.LinkType is MarkdownLinkType.UrlLink)
                {
                    if (pre is MarkdownTextToken t && (!IsValidPreviousCharacter(t.Content.Last())))
                    {
                        result += "<" + render.Render(token) + ">";
                        continue;
                    }

                    if (post is MarkdownTextToken tt && (!IsValidPostCharacters(tt.Content)))
                    {
                        result += "<" + render.Render(token) + ">";
                        continue;
                    }

                    if (pre is MarkdownTagInlineToken)
                    {
                        result += "<" + render.Render(token) + ">";
                        continue;
                    }

                    result += render.Render(token);
                }
                else if (localTokens[index] is MarkdownTagInlineToken)
                {
                    result += MarkupInlineToken(render, localTokens[index]);
                }
                else if (localTokens[index] is MarkdownTextToken textToken)
                {
                    var text = textToken.SourceInfo.Markdown;
                    result += inSideTable? text.Replace("`", "\\`").Replace("*", "\\*") : text;
                }
                else if (localTokens[index] is MarkdownEscapeInlineToken)
                {
                    result += render.Render(localTokens[index]);
                }
                else if (localTokens[index] is MarkdownStrongInlineToken || localTokens[index] is MarkdownEmInlineToken)
                {
                    var delimiterCount = localTokens[index] is MarkdownStrongInlineToken ? 2 : 1;
                    var seToken = localTokens[index];
                    var enableWithinWord = seToken.SourceInfo.Markdown[0] == '*';

                    ////Check strart delimiter
                    //var pc = pre == null ? '\0' : pre.SourceInfo.Markdown.Last();
                    //var c = seToken.SourceInfo.Markdown[delimiterCount];
                    //bool startCanOpen, startCanClose;
                    //CheckOpenCloseDelimiter(pc, c, enableWithinWord, out startCanOpen, out startCanClose);

                    ////Check end delimiter
                    //pc = seToken.SourceInfo.Markdown[seToken.SourceInfo.Markdown.Length - delimiterCount - 1];
                    //c = post == null ? '\0' : post.SourceInfo.Markdown.First();
                    //bool endCanOpen, endCanClose;
                    //CheckOpenCloseDelimiter(pc, c, enableWithinWord, out endCanOpen, out endCanClose);

                    //if(startCanOpen && endCanClose)
                    //{
                    //    result += insideHtml ? MarkupInlineToken(render, seToken) : render.Render(seToken);
                    //}
                    //else
                    {

                        if (seToken is MarkdownStrongInlineToken strong)
                        {
                            localTokens.Insert(index, new MarkdownTagInlineToken(null, null, SourceInfo.Create("</strong>", seToken.SourceInfo.File)));
                            if(strong.Content.Any() && strong.Content.Last() is MarkdownTextToken && strong.Content.Last().SourceInfo.Markdown.EndsWith("\\"))
                            {
                                localTokens.Insert(index, new MarkdownTextToken(null, null, "\\", SourceInfo.Create("\\", seToken.SourceInfo.File)));
                            }
                            localTokens.InsertRange(index, strong.Content);
                            localTokens.Insert(index, new MarkdownTagInlineToken(null, null, SourceInfo.Create("<strong>", seToken.SourceInfo.File)));
                        }
                        else
                        {
                            var em = seToken as MarkdownEmInlineToken;
                            localTokens.Insert(index, new MarkdownTagInlineToken(null, null, SourceInfo.Create("</em>", seToken.SourceInfo.File)));
                            if (em.Content.Any() && em.Content.Last() is MarkdownTextToken && em.Content.Last().SourceInfo.Markdown.EndsWith("\\"))
                            {
                                localTokens.Insert(index, new MarkdownTextToken(null, null, "\\", SourceInfo.Create("\\", seToken.SourceInfo.File)));
                            }
                            localTokens.InsertRange(index, em.Content);
                            localTokens.Insert(index, new MarkdownTagInlineToken(null, null, SourceInfo.Create("<em>", seToken.SourceInfo.File)));
                        }

                        localTokens.Remove(seToken);
                        UpdateReport(seToken, "StrongEmToTag");
                        index--;
                    }
                }
                else
                {
                    result += render.Render(localTokens[index]);
                }
            }

            return RollBackStrongEmTag(result);
        }

        private string RollBackStrongEmTag(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            //Replace <strong>
            var result = RevertSingleStrongEmTag(content, true);

            //Replace <em>
            result = RevertSingleStrongEmTag(result, false);

            return result;
        }

        private string RevertSingleStrongEmTag(string content, bool isStrong)
        {
            var index = 0;
            var result = content;
            var oldContent = content;
            var tag = isStrong ? "<strong>" : "<em>";
            var tagRegex = isStrong ? _strongRegex : _emRegex;
            var tagStar = isStrong ? "**" : "*";
            var baseHtml = _service.Markup(content, "topic.md").Html;

            while (true)
            {
                index = result.IndexOf(tag, index);
                if (index == -1) break;
                result = tagRegex.Replace(result, m => tagStar + m.Groups[1].Value + tagStar, 1, index);
                index++;

                if (oldContent != result)
                {
                    var oldHtml = _service.Markup(oldContent, "topic.md").Html;
                    var newHtml = _service.Markup(result, "topic.md").Html;
                    
                    if (IsSameHtml(baseHtml, newHtml) && IsSameHtml(baseHtml, oldHtml))
                    {
                        oldContent = result;
                    }
                    else
                    {
                        result = oldContent;
                    }
                }
            }

            return result;
        }

        bool IsSameHtml(string html1, string html2)
        {
            try
            {
                var compareTool = new HtmlDiffTool(html1, html2, true);
                return html1 == html2 || compareTool.Compare();
            }
            catch (Exception)
            {
                return false;
            }
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
                Console.WriteLine($"Error occured while resolving uid, {ex}");
                return false;
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
            using (var response = await GetResponse(requestUrl))
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

        private static async Task<HttpResponseMessage> GetResponse(string requestUrl)
        {
            try
            {
                return await _client.Value.GetAsync(requestUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurs while get response: {ex}");
                throw;
            }
        }

        private static bool IsValidPostCharacters(string content)
        {
            if (string.IsNullOrEmpty(content)) return true;

            return IsTrailingUrlStopCharacter(content[0])
                && (content.Length > 1 && IsEndOfUri(content[1]) || content.Length == 1);
        }

        private static bool IsTrailingUrlStopCharacter(char c)
        {
            return c == '?' || c == '!' || c == '.' || c == ',' || c == ':' || c == '*' || c == '*' || c == '_' || c == '~';
        }

        private static bool IsEndOfUri(char c)
        {
            return c == '\t' || c <= ' ' || Char.IsControl(c); // TODO: specs unclear. space is strict or relaxed? (includes tabs?)
        }

        private static void CheckOpenCloseDelimiter(char pc, char c, bool enableWithinWord, out bool canOpen, out bool canClose)
        {
            // A left-flanking delimiter run is a delimiter run that is 
            // (a) not followed by Unicode whitespace, and
            // (b) either not followed by a punctuation character, or preceded by Unicode whitespace 
            // or a punctuation character. 
            // For purposes of this definition, the beginning and the end of the line count as Unicode whitespace.
            bool nextIsPunctuation;
            bool nextIsWhiteSpace;
            bool prevIsPunctuation;
            bool prevIsWhiteSpace;
            CheckUnicodeCategory(pc, out prevIsWhiteSpace, out prevIsPunctuation);
            CheckUnicodeCategory(c, out nextIsWhiteSpace, out nextIsPunctuation);

            var prevIsExcepted = prevIsPunctuation && punctuationExceptions.Contains(pc);
            var nextIsExcepted = nextIsPunctuation && punctuationExceptions.Contains(c);

            canOpen = !nextIsWhiteSpace &&
                           ((!nextIsPunctuation || nextIsExcepted) || prevIsWhiteSpace || prevIsPunctuation);


            // A right-flanking delimiter run is a delimiter run that is 
            // (a) not preceded by Unicode whitespace, and 
            // (b) either not preceded by a punctuation character, or followed by Unicode whitespace 
            // or a punctuation character. 
            // For purposes of this definition, the beginning and the end of the line count as Unicode whitespace.
            canClose = !prevIsWhiteSpace &&
                            ((!prevIsPunctuation || prevIsExcepted) || nextIsWhiteSpace || nextIsPunctuation);

            if (!enableWithinWord)
            {
                var temp = canOpen;
                // A single _ character can open emphasis iff it is part of a left-flanking delimiter run and either 
                // (a) not part of a right-flanking delimiter run or 
                // (b) part of a right-flanking delimiter run preceded by punctuation.
                canOpen = canOpen && (!canClose || prevIsPunctuation);

                // A single _ character can close emphasis iff it is part of a right-flanking delimiter run and either
                // (a) not part of a left-flanking delimiter run or 
                // (b) part of a left-flanking delimiter run followed by punctuation.
                canClose = canClose && (!temp || nextIsPunctuation);
            }
        }

        private static void CheckUnicodeCategory(char c, out bool space, out bool punctuation)
        {
            // Credits: code from CommonMark.NET
            // Copyright (c) 2014, Kārlis Gaņģis All rights reserved. 
            // See license for details:  https://github.com/Knagis/CommonMark.NET/blob/master/LICENSE.md
            if (c <= 'ÿ')
            {
                space = c == '\0' || c == ' ' || (c >= '\t' && c <= '\r') || c == '\u00a0' || c == '\u0085';
                punctuation = c == '\0' || (c >= 33 && c <= 47 && c != 38) || (c >= 58 && c <= 64) || (c >= 91 && c <= 96) || (c >= 123 && c <= 126);
            }
            else
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                space = category == UnicodeCategory.SpaceSeparator
                    || category == UnicodeCategory.LineSeparator
                    || category == UnicodeCategory.ParagraphSeparator;
                punctuation = !space &&
                    (category == UnicodeCategory.ConnectorPunctuation
                    || category == UnicodeCategory.DashPunctuation
                    || category == UnicodeCategory.OpenPunctuation
                    || category == UnicodeCategory.ClosePunctuation
                    || category == UnicodeCategory.InitialQuotePunctuation
                    || category == UnicodeCategory.FinalQuotePunctuation
                    || category == UnicodeCategory.OtherPunctuation);
            }
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

        public void UpdateReport(IMarkdownToken token, string tokenName)
        {
            var file = token.SourceInfo.File;
            var tokenInfo = new MigratedTokenInfo(tokenName, token.SourceInfo.LineNumber);
            ReportUtility.Add(file, tokenInfo);
        }

        public string GetTokenName(IMarkdownToken token)
        {
            var fullName = token.GetType().ToString();
            var name = fullName.Substring(fullName.LastIndexOf('.') + 1);

            return name;
        }
    }
}
