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
            var htmlA = @"<div class=""embeddedvideo""><iframe src=""https://channel9.msdn.com/Blogs/Azure/b2b-collaboration-redemption/Player?nocookie=true"" frameborder=""0"" allowfullscreen=""true""></iframe></div> ";
            var htmlB = @"<div class=""embeddedvideo""><iframe src=""https://channel9.msdn.com/Blogs/Azure/b2b-collaboration-redemption/Player"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
<blockquote>
</blockquote> ";

            HtmlDiffTool hdt = new HtmlDiffTool(htmlA, htmlB);
            Span diffSpan;
            string dfmHtml, markdigHtml;
            Assert.True(hdt.Compare(out diffSpan, out dfmHtml, out markdigHtml));
        }
    }
}
