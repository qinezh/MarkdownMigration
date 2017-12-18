namespace MarkdownMigration.Diff.Test
{
    using Xunit;
    using HtmlCompare;

    public class HtmlCompareTest
    {
        [Fact]
        public void HtmlCompareSimpleTest()
        {
            var htmlA = "<a>dsa</a><a>dsa</a>";
            var htmlB = "<a>dsa</a> <a>dsa</a>";

            Assert.True(MarkdownMigrateDiffUtility.ComapreHtml(htmlA, htmlB));
        }
    }
}
