using System;

using Markdig;
using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;
using HtmlCompare;

namespace MarkdownMigration.Convert
{
    public class MarkdigMarkdownRendererProxy : MarkdigMarkdownRenderer
    {
        private MarkdownEngine _dfmEngine;
        private MarkdigMarkdownRenderer renderer = new MarkdigMarkdownRenderer();

        public MarkdigMarkdownRendererProxy()
        {
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = true;
            var builder = new DfmEngineBuilder(option);
            var render = new DfmRenderer();
            _dfmEngine = builder.CreateDfmEngine(render);
        }

        public new StringBuffer Render(IMarkdownRenderer render, IMarkdownToken token, IMarkdownContext context)
        {
            var original = token.SourceInfo.Markdown;

            var dfmHtml = _dfmEngine.Markup(original, token.SourceInfo.File);
            var markdigHtml = Markdown.ToHtml(original);

            if (MarkdownMigrateDiffUtility.ComapreHtml(dfmHtml, markdigHtml))
            {
                return original;
            }

            var migrated = renderer.Render((dynamic)render, (dynamic)token, (dynamic)context);
            if (!string.Equals(original.ToString(), migrated.ToString()))
            {
                Console.WriteLine($"{token.GetType()}: {token.SourceInfo.LineNumber}");
            }

            return migrated;
        }
    }
}
