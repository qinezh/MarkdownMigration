namespace MarkdownMigration.Convert.Test
{
    using Xunit;

    public class MarkdigMarkdownRewritersTests
    {
        private MarkdownMigrateTool _tool;

        public MarkdigMarkdownRewritersTests()
        {
            _tool = new MarkdownMigrateTool();
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestResloved_ShortcutXref()
        {
            var source = "@System.String";
            var expected = "@\"System.String\"";

            var result = _tool.Convert("topic.md", source);
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestUnresloved_ShortcutXref()
        {
            var source = "@outlook.com";
            var expected = "@outlook.com";

            var result = _tool.Convert("topic.md", source);
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestAutoLinkXref()
        {
            var source = "<xref:system.string>";
            var expected = "<xref:system.string>";

            var result = _tool.Convert("topic.md", source);
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

            var result = _tool.Convert("topic.md", source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMailTo()
        {
            var source = "<Mailto:docs@microsoft.com>";
            var expected = "<docs@microsoft.com>";

            var result = _tool.Convert("topic.md", source);
            Assert.Equal(expected, result);

            result = _tool.Convert("topic.md", result);
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

# header
**markdown**

<div>
This is **markdown** content.
</div>";
            var expected = @"

**markdown**



<div>
This is <strong>markdown</strong> content.
</div>

# header
**markdown**

<div>
This is <strong>markdown</strong> content.
</div>";

            var result = _tool.Convert("topic.md", source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestHeadingWithHref()
        {
            var source = @"## <a id=""WhatIs""></a>What is Twilio?
";
            var expected = @"## <a id=""WhatIs""></a>What is Twilio?
";

            var result = _tool.Convert("topic.md", source);
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

            var result = _tool.Convert("topic.md", source);
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

            var result = _tool.Convert("topic.md", source);
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

            var result = _tool.Convert("topic.md", source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestStrongAndEm()
        {
            var source = "__a__ and _b_ and **a** and **b** and *__ab__*";
            var expected = "__a__ and _b_ and **a** and **b** and *__ab__*";

            var result = _tool.Convert("topic.md", source);
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestAutoLinkWithQuota()
        {
            var source = "This kind of url link such as \'https://github.com\' are not supported in markdig";
            var expected = "This kind of url link such as \'<https://github.com>\' are not supported in markdig";

            var result = _tool.Convert("topic.md", source);
            Assert.Equal(expected, result);
        }
    }
}
