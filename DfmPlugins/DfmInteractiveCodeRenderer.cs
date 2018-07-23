namespace Microsoft.DocAsCode.EntityModel.Plugins.OpenPublishing
{
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    public class DfmInteractiveCodeRenderer : DfmCodeRenderer
    {
        public DfmInteractiveCodeRenderer()
            : base(GetBuilder())
        {
        }

        private static CodeLanguageExtractorsBuilder GetBuilder() =>
            TagNameBlockPathQueryOption.GetDefaultCodeLanguageExtractorsBuilder().AddAlias(n => n[0] == '.' ? null : n + DfmInteractiveCodeRendererPartProvider.InteractivePostfix);

        public override StringBuffer RenderOpenCodeTag(StringBuffer result, DfmFencesToken token, Options options, IDfmFencesBlockPathQueryOption pathQueryOption)
        {
            var lang = GetLang(token, out bool isInteractive);

            var gitUrl = GetGitUrl(token);

            result += "<code";
            if (!string.IsNullOrEmpty(lang))
            {
                result = result + " class=\"" + options.LangPrefix + lang + "\"";
                if (isInteractive)
                {
                    result += " data-interactive=\"";
                    result += StringHelper.HtmlEncode(lang);
                    result += "\"";
                }
            }
            if (gitUrl != null && gitUrl.StartsWith("https://github.com/"))
            {
                result += " data-src=\"";
                result += StringHelper.HtmlEncode(gitUrl);
                result += "\"";
            }
            if (!string.IsNullOrEmpty(token.Name))
            {
                result = result + " name=\"" + StringHelper.HtmlEncode(token.Name) + "\"";
            }
            if (!string.IsNullOrEmpty(token.Title))
            {
                result = result + " title=\"" + StringHelper.HtmlEncode(token.Title) + "\"";
            }
            if (!string.IsNullOrEmpty(pathQueryOption?.HighlightLines))
            {
                result = result + " highlight-lines=\"" + StringHelper.HtmlEncode(pathQueryOption.HighlightLines) + "\"";
            }
            result += ">";
            return result;
        }

        private static string GetLang(DfmFencesToken token, out bool isInteractive)
        {
            isInteractive = false;
            if (token.Lang == null)
            {
                return null;
            }
            if (token.Lang.EndsWith(DfmInteractiveCodeRendererPartProvider.InteractivePostfix))
            {
                isInteractive = true;
                return token.Lang.Remove(token.Lang.Length - DfmInteractiveCodeRendererPartProvider.InteractivePostfix.Length);
            }
            return token.Lang;
        }

        protected virtual string GetGitUrl(DfmFencesToken token)
        {
            var file = FindFile(token, token.Context);
            var path = EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file);
            return "https://www.github.com/fakeRepo";
        }
    }
}
