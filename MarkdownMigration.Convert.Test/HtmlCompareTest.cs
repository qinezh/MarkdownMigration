namespace MarkdownMigration.Diff.Test
{
    using HtmlCompare;
    using Xunit;

    public class HtmlCompareTest
    {
        [Fact]
        public void HtmlCompareSimpleTest()
        {
            var htmlA = @"<div class=""embeddedvideo""><iframe src=""https://channel9.msdn.com/Blogs/Azure/b2b-collaboration-redemption/Player"" frameborder=""0"" allowfullscreen=""true""></iframe></div> ";
            var htmlB = @"<div class=""embeddedvideo""><iframe src=""https://channel9.msdn.com/Blogs/Azure/b2b-collaboration-redemption/Player"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
<blockquote>
</blockquote> ";

            Assert.True(MarkdownMigrateDiffUtility.ComapreHtml(htmlA, htmlB));
        }
    }
}
