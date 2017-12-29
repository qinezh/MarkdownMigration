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
            var htmlA = File.ReadAllText(@"F:\MarkdownMigration\MarkdownMigration\artifacts\result\e2eprod-azure-documents\dfm-html\azure-stack\user\azure-stack-storage-dev.html");
            var htmlB = File.ReadAllText(@"F:\MarkdownMigration\MarkdownMigration\artifacts\result\e2eprod-azure-documents\markdig-html\azure-stack\user\azure-stack-storage-dev.html");


            HtmlDiffTool hdt = new HtmlDiffTool(htmlA, htmlB);
            Span diffSpan;
            string dfmHtml, markdigHtml;
            DiffStatus diffStatus;
            Assert.True(hdt.Compare(out diffSpan, out dfmHtml, out markdigHtml, out diffStatus));
        }
    }
}
