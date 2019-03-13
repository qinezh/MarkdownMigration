namespace MarkdownMigration.Diff.Test
{
    using HtmlCompare;
    using MarkdownMigration.Common;
    using System.IO;
    using Xunit;

    public class HtmlCompareTest
    {
        [Fact]
        public void HtmlCompareSimpleTest()
        {
            var htmlA = @"<strong><em>a</em></strong><xref href=""Microsoft.Crm.Sdk.Messages.AssignRequest"" data-throw-if-not-resolved=""True"" data-raw-source=""&lt;xref:Microsoft.Crm.Sdk.Messages.AssignRequest&gt;"" sourcefile=""developer/entities/msdyn_timegroup.md"" sourcestartlinenumber=""24"" sourceendlinenumber=""24""></xref>";
            var htmlB = @"<em> <strong>a</strong> </em><xref href=""Microsoft.Crm.Sdk.Messages.AssignRequest"" data-throw-if-not-resolved=""True"" sourcefile=""developer/entities/msdyn_timegroup.md"" sourcestartlinenumber=""24"" sourceendlinenumber=""24""></xref>";

            HtmlDiffTool hdt = new HtmlDiffTool(htmlA, htmlB);
            Span diffSpan;
            string dfmHtml, markdigHtml;
            Assert.True(hdt.Compare(out diffSpan, out dfmHtml, out markdigHtml));
        }

        [Fact]
        public void HtmlCompareCodeSnippetWarning()
        {
            var htmlA = @"<!-- Can not find reference ../../../powershell_scripts/virtual-machine/create-vm-detailed/create-vm-detailed.ps1 -->";
            var htmlB = @"<div class=""WARNING"">
<h5>WARNING</h5>
<p>It looks like the sample you are looking for does not exist.</p>
</div>";
            var htmlC = @"<div class=""WARNING"">
<h5>WARNING</h5>
<p>I am a real warning.</p>
</div>";

            HtmlDiffTool hdt = new HtmlDiffTool(htmlA, htmlB);
            Assert.True(hdt.Compare(out _, out _, out _));

            HtmlDiffTool hdt2 = new HtmlDiffTool(htmlA, htmlC);
            Assert.False(hdt2.Compare(out _, out _, out _));
        }

        [Fact]
        public void HtmlCompareOrderedList()
        {
            var htmlA = @"<ol start=""6""><li>hello</li></ol>";
            var htmlB = @"<ol><li>hello</li></ol>";

            HtmlDiffTool hdt = new HtmlDiffTool(htmlA, htmlB);
            Assert.False(hdt.Compare(out _, out _, out _));
        }

        [Fact]
        public void HtmlCompareEmoji()
        {
            var htmlA = @"<p><span class=""emoji"" shortcode=""heavy_minus_sign"">➖</span></p>";
            var htmlB = @"<p>➖</p>";

            HtmlDiffTool hdt = new HtmlDiffTool(htmlA, htmlB);
            Assert.True(hdt.Compare(out _, out _, out _));

            var htmlA2 = @"<p><span>➖</span></p>";
            HtmlDiffTool hdt2 = new HtmlDiffTool(htmlA2, htmlB);
            Assert.False(hdt2.Compare(out _, out _, out _));
        }
    }
}
