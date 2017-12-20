namespace MarkdownMigration.Diff.Test
{
    using HtmlCompare;
    using Xunit;

    public class HtmlCompareTest
    {
        [Fact]
        public void HtmlCompareSimpleTest()
        {
            var htmlA = @"<yamlheader start=""1"" end=""4"" sourceFile=""sample.md"">title: abc</yamlheader>";
            var htmlB = @"<yamlheader start=""1"" end=""4"">title: abc</yamlheader>";

            Assert.True(MarkdownMigrateDiffUtility.ComapreHtml(htmlA, htmlB));
        }
    }
}
