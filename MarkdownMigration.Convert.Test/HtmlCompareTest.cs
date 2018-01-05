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
            var htmlA = @"<strong><em>a</em></strong>";
            var htmlB = @"<em> <strong>a</strong> </em>";

            HtmlDiffTool hdt = new HtmlDiffTool(htmlA, htmlB);
            Span diffSpan;
            string dfmHtml, markdigHtml;
            Assert.True(hdt.Compare(out diffSpan, out dfmHtml, out markdigHtml));
        }
    }
}
