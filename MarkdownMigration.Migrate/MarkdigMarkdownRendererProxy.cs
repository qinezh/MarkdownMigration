using Microsoft.DocAsCode.MarkdownLite;
using System;

namespace MarkdownMigration.Convert
{
    public class MarkdigMarkdownRendererProxy : MarkdigMarkdownRenderer
    {
        private MarkdigMarkdownRenderer renderer = new MarkdigMarkdownRenderer();

        public override StringBuffer Render(IMarkdownRenderer render, IMarkdownToken token, IMarkdownContext context)
        {
            var original = token.SourceInfo.Markdown;
            var migrated = renderer.Render(render, token, context);
            if (!string.Equals(original.ToString(), migrated.ToString()))
            {
                Console.WriteLine($"{token.GetType()}: {token.SourceInfo.LineNumber}");
            }

            return migrated;
        }
    }
}
