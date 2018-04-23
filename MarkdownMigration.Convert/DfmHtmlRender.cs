using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;
using System.Collections.Generic;

namespace MarkdownMigration.Convert
{
    public class DfmHtmlRender : DfmRenderer, IMarkdownRenderer
    {
        public DfmHtmlRender(bool useLegacyMode = true)
        {
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = useLegacyMode;
            var builder = new DfmEngineBuilder(option);

            Engine = builder.CreateDfmEngine(this);
            Renderer = this;
            Options = option;
            Links = null;
        }

        public IMarkdownEngine Engine { get; }

        public object Renderer { get; }

        public Options Options { get; }

        public Dictionary<string, LinkObj> Links { get; }

        public StringBuffer Render(IMarkdownToken token)
        {
            return this.Render((dynamic)this, (dynamic)token, (dynamic)token.Context);
        }
    }
}
