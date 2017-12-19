namespace MarkdownMigration.Diff.Test
{
    using Xunit;
    using HtmlCompare;

    public class HtmlCompareTest
    {
        [Fact]
        public void HtmlCompareSimpleTest()
        {
            var htmlA = @"<div class=""TIP""><h5>TIP</h5><p>For phase estimation</p>
</div>
<div class=""TIP""><h5>TIP</h5><p>The true canon.</p>
</div>";
            var htmlB = @"<div class=""TIP"">
<h5>TIP</h5>
<p>For phase estimation</p>
</div>
<div class=""TIP"">
<h5>TIP</h5>
<p>The true canon.</p>
</div> ";

            Assert.True(MarkdownMigrateDiffUtility.ComapreHtml(htmlA, htmlB));
        }
    }
}
