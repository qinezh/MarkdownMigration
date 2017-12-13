namespace MarkdownMigration.Convert.Test
{
    using Microsoft.DocAsCode.Dfm;
    using Xunit;

    public class MarkdigMarkdownRewritersTests
    {
        private DfmEngine _engine;

        public MarkdigMarkdownRewritersTests()
        {
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = true;
            var builder = new DfmEngineBuilder(option);
            _engine = builder.CreateDfmEngine(new MarkdigMarkdownRenderer());
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestResloved_ShortcutXref()
        {
            var source = "@System.String";
            var expected = "@\"System.String\"";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestUnresloved_ShortcutXref()
        {
            var source = "@outlook.com";
            var expected = "@outlook.com";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestAutoLinkXref()
        {
            var source = "<xref:system.string>";
            var expected = "<xref:system.string>";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestNormalizeMultipleVideo()
        {
            var source = @"> Video Sample
> [!VIDEO https://channel9.msdn.com]
>
>[!VIDEO https://channel9.msdn.com]
>
> *abc*
";
            var expected = @"> Video Sample
> [!VIDEO https://channel9.msdn.com]
>
>[!VIDEO https://channel9.msdn.com]
>
> *abc*
";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMailTo()
        {
            var source = "<Mailto:docs@microsoft.com>";
            var expected = "<docs@microsoft.com>";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);

            result = Rewrite(result, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestHtml()
        {
            var source = @"
**markdown**


<div>
This is **markdown** content.
</div>

# header";
            var expected = @"
**markdown**


<div>
This is <strong>markdown</strong> content.
</div>

# header";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestHeadingWithWhitespace()
        {
            var source = @" #    Heading with whitespace    #

content...";
            var expected = @" #    Heading with whitespace    #

content...";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestHeadingWithoutWhitespace()
        {
            var source = @" #Heading with whitespace

content...";
            var expected = @" # Heading with whitespace

content...";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestLHeading()
        {
            var source = @"Heading with whitespace
===

content...";
            var expected = @"Heading with whitespace
===

content...";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestStrongAndEm()
        {
            var source = "__a__ and _b_ and **a** and **b** and *__ab__*";
            var expected = "__a__ and _b_ and **a** and **b** and *__ab__*";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        private string Rewrite(string source, string filePath)
        {
            return _engine.Markup(source, filePath);
        }
    }
}
