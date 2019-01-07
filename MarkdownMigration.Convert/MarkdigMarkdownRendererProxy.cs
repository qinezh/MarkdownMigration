using System.Collections.Generic;
using System.Linq;

using MarkdownMigration.Common;
using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;

namespace MarkdownMigration.Convert
{
    public class MarkdigMarkdownRendererProxy : MarkdownRenderer
    {
        public MarkdigMarkdownRenderer _renderer;
        private Stack<IMarkdownToken> _processedBlockTokens;
        private int _totalLines;
        private MigrationRule _rule;

        public MarkdigMarkdownRendererProxy(string basePath = ".", bool useLegacyMode = true, int totalLines = 0, MigrationRule rule = MigrationRule.All)
        {
            _processedBlockTokens = new Stack<IMarkdownToken>();
            _renderer = new MarkdigMarkdownRenderer(_processedBlockTokens, basePath, useLegacyMode, rule);
            _totalLines = totalLines;
            _rule = rule;
        }

        public new StringBuffer Render(IMarkdownRenderer render, IMarkdownToken token, IMarkdownContext context)
        {
            var migrated = RenderCore(render, token, context);
            if (context is MarkdownBlockContext)
            {
                _processedBlockTokens.Push(token);
            }

            return migrated;
        }

        private StringBuffer RenderCore(IMarkdownRenderer render, IMarkdownToken token, IMarkdownContext context)
        {
            if (!CheckRuleEnabled(token))
            {
                return token.SourceInfo.Markdown;
            }

            if (!NeedMigration(token))
            {
                return token.SourceInfo.Markdown;
            }

            var migrated = _renderer.Render((dynamic)render, (dynamic)token, (dynamic)context);

            if (!string.Equals(token.SourceInfo.Markdown.ToString(), migrated.ToString()))
            {
                _renderer.UpdateReport(token, _renderer.GetTokenName(token));
            }

            return migrated;
        }

        private bool CheckRuleEnabled(IMarkdownToken token)
        {
            if (_rule == MigrationRule.All) return true;

            if (token is DfmXrefInlineToken && _rule.HasFlag(MigrationRule.Xref)) return true;
            if (token is DfmIncludeInlineToken && _rule.HasFlag(MigrationRule.InclusionInline)) return true;
            if (token is MarkdownImageInlineToken && _rule.HasFlag(MigrationRule.Image)) return true;
            if (token is MarkdownLinkInlineToken && _rule.HasFlag(MigrationRule.Link)) return true;
            if (token is MarkdownStrongInlineToken && _rule.HasFlag(MigrationRule.Strong)) return true;
            if (token is MarkdownEmInlineToken && _rule.HasFlag(MigrationRule.Em)) return true;
            if (token is MarkdownTableBlockToken && _rule.HasFlag(MigrationRule.Table)) return true;

            if (token is DfmNoteBlockToken && _rule.HasFlag(MigrationRule.Note)) return true;
            if (token is DfmIncludeBlockToken && _rule.HasFlag(MigrationRule.InclusionBlock)) return true;
            if (token is DfmFencesBlockToken && _rule.HasFlag(MigrationRule.Code)) return true;
            if (token is MarkdownHtmlBlockToken && _rule.HasFlag(MigrationRule.HtmlBlock)) return true;
            if (token is MarkdownHeadingBlockToken && _rule.HasFlag(MigrationRule.Heading)) return true;
            if (token is MarkdownListBlockToken && _rule.HasFlag(MigrationRule.List)) return true;
            if (token is MarkdownBlockquoteBlockToken && _rule.HasFlag(MigrationRule.BlockQuote)) return true;

            if (token is MarkdownParagraphBlockToken && (_rule & MigrationRule.Paragraph) != 0) return true;
            if (token is MarkdownNonParagraphBlockToken && (_rule & MigrationRule.Paragraph) != 0) return true;

            return false;
        }

        private bool NeedMigration(IMarkdownToken token)
        {
            var file = token.SourceInfo.File;
            var markdown = token.SourceInfo.Markdown;

            if (token is MarkdownTableBlockToken t && NeedMigrationTable(t))
            {
                return true;
            }

            // remvoe end newlines in code block
            if (token is MarkdownCodeBlockToken)
            {
                return true;
            }

            if (token is DfmNoteBlockToken)
            {
                return true;
            }

            if (token is DfmIncludeBlockToken || token is DfmIncludeInlineToken || token is DfmFencesBlockToken)
            {
                return true;
            }

            if (HasLinkTokenToMigrate(token))
            {
                return true;
            }

            return !_renderer.CompareMarkupResult(markdown, file);
        }

        private bool HasLinkTokenToMigrate(IMarkdownToken token)
        {
            if(token is MarkdownLinkInlineToken || token is MarkdownImageInlineToken)
            {
                var link = token as MarkdownLinkInlineToken;
                if (link != null)
                {
                    return link.Href.Contains('\\') || link.Href.Contains(' ');
                }
                
                var image = token as MarkdownImageInlineToken;
                return image.Href.Contains('\\') || image.Href.Contains(' ');
            }
            else
            {
                return token.Children().Any(t => HasLinkTokenToMigrate(t));
            }
        }

        private bool NeedMigrationParagrah(MarkdownParagraphBlockToken token)
        {
            var markdown = token.SourceInfo.Markdown;
            var tokens = token.InlineTokens.Tokens;
            if (tokens != null && tokens.LastOrDefault() is MarkdownTagInlineToken tag)
            {
                var newLineCount = Helper.CountEndNewLine(markdown);
                if (newLineCount < 2)
                {
                    return true;
                }
            }

            return false;
        }

        private bool NeedMigrationTable(MarkdownTableBlockToken token)
        {
            var markdown = token.SourceInfo.Markdown;

            if (_processedBlockTokens != null && _processedBlockTokens.Count > 0)
            {
                var preToken = _processedBlockTokens.Peek();
                var preTokenNewLinecount = Helper.CountEndNewLine(preToken.SourceInfo.Markdown);
                if (!(preToken is MarkdownNewLineBlockToken) && preTokenNewLinecount < 2)
                {
                    return true;
                }
            }

            var newLineCount = Helper.CountEndNewLine(markdown);
            if (markdown.Split('\n').Count() + token.SourceInfo.LineNumber - 1 < _totalLines && newLineCount < 2)
            {
                return true;
            }

            return false;
        }
    }
}
