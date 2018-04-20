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
    }
}
