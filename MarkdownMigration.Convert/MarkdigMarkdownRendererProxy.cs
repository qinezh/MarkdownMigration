using System.Collections.Generic;
using System;

using MarkdigEngine;
using MarkdigEngine.Extensions;
using MarkdownMigration.Common;
using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;
using Microsoft.DocAsCode.Plugins;
using HtmlCompare;

namespace MarkdownMigration.Convert
{
    public class MarkdigMarkdownRendererProxy : MarkdownRenderer
    {
        private MarkdownEngine _dfmEngine;
        private MarkdigMarkdownService _service;
        private MarkdigMarkdownRenderer _renderer;
        private Stack<IMarkdownToken> _processedBlockTokens;

        public MarkdigMarkdownRendererProxy(string basePath = ".")
        {
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = true;
            var builder = new DfmEngineBuilder(option);
            var render = new DfmRenderer();
            _dfmEngine = builder.CreateDfmEngine(render);

            var parameter = new MarkdownServiceParameters
            {
                BasePath = basePath,
                Extensions = new Dictionary<string, object>
                {
                    { LineNumberExtension.EnableSourceInfo, false }
                }
            };
            _service = new MarkdigMarkdownService(parameter);
            _processedBlockTokens = new Stack<IMarkdownToken>();
            _renderer = new MarkdigMarkdownRenderer(_processedBlockTokens);
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
            if (!NeedMigration(token))
            {
                return token.SourceInfo.Markdown;
            }

            var migrated = _renderer.Render((dynamic)render, (dynamic)token, (dynamic)context);
            UpdateReport(token, migrated);

            return migrated;
        }

        private bool NeedMigration(IMarkdownToken token)
        {
            var file = token.SourceInfo.File;
            var markdown = token.SourceInfo.Markdown;

            if (token is MarkdownTableBlockToken t && NeedMigrationTable(t))
            {
                return true;
            }

            try
            {
                var dfmHtml = _dfmEngine.Markup(markdown, file);
                var markdigHtml = _service.Markup(markdown, file).Html;

                var compareTool = new HtmlDiffTool(dfmHtml, markdigHtml, true);
                if (compareTool.Compare())
                {
                    return false;
                }
            }
            catch (Exception)
            {
                // TODO
            }

            return true;
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
            if (newLineCount < 2)
            {
                return true;
            }

            return false;
        }


        private void UpdateReport(IMarkdownToken token, string migrated)
        {
            var file = token.SourceInfo.File;
            var markdown = token.SourceInfo.Markdown;
            if (!string.Equals(markdown.ToString(), migrated.ToString()))
            {
                var tokenName = GetTokenName(token);
                var tokenInfo = new MigratedTokenInfo(tokenName, token.SourceInfo.LineNumber);
                ReportUtility.Add(file, tokenInfo);
            }
        }

        private string GetTokenName(IMarkdownToken token)
        {
            var fullName = token.GetType().ToString();
            var name = fullName.Substring(fullName.LastIndexOf('.') + 1);

            return name;
        }
    }
}
