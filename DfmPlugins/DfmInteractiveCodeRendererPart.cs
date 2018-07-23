namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing
{
    using System;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmInteractiveCodeRendererPart : DfmCustomizedRendererPartBase<IMarkdownRenderer, DfmFencesToken, IMarkdownContext>
    {
        private readonly DfmInteractiveCodeRenderer _renderer;

        public DfmInteractiveCodeRendererPart()
        {
            _renderer = new DfmInteractiveCodeRenderer();
        }

        public override string Name => nameof(DfmInteractiveCodeRendererPart);

        public override bool Match(IMarkdownRenderer renderer, DfmFencesToken token, IMarkdownContext context)
        {
            return true;
        }

        public override StringBuffer Render(IMarkdownRenderer renderer, DfmFencesToken token, IMarkdownContext context)
        {
            return _renderer.Render(renderer, token, context);
        }
    }
}
