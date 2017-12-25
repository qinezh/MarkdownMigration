using System.Collections.Generic;
using System;
using System.Diagnostics;

using MarkdigEngine;
using MarkdigEngine.Extensions;
using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;
using Microsoft.DocAsCode.Plugins;
using HtmlCompare;
using MarkdownMigration.Common;

namespace MarkdownMigration.Convert
{
    public class MarkdigMarkdownRendererProxy : MarkdigMarkdownRenderer
    {
        private MarkdownEngine _dfmEngine;
        private MarkdigMarkdownService _service;
        private MarkdigMarkdownRenderer renderer = new MarkdigMarkdownRenderer();

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
        }

        public new StringBuffer Render(IMarkdownRenderer render, IMarkdownToken token, IMarkdownContext context)
        {
            var file = token.SourceInfo.File;
            var original = token.SourceInfo.Markdown;

            try
            {
                var dfmHtml = _dfmEngine.Markup(original, file);
                var markdigHtml = _service.Markup(original, file).Html;

                if (MarkdownMigrateDiffUtility.ComapreHtml(dfmHtml, markdigHtml))
                {
                    return original;
                }
            }
            catch (Exception ex)
            {
                // TODO
            }


            var migrated = renderer.Render((dynamic)render, (dynamic)token, (dynamic)context);
            if (!string.Equals(original.ToString(), migrated.ToString()))
            {
                var tokenName = GetTokenName(token);
                var tokenInfo = new MigratedTokenInfo(tokenName, token.SourceInfo.LineNumber);
                ReportUtility.Add(file, tokenInfo);
            }

            return migrated;
        }

        private string GetTokenName(IMarkdownToken token)
        {
            var fullName = token.GetType().ToString();
            var name = fullName.Substring(fullName.LastIndexOf('.') + 1);

            return name;
        }
    }
}
