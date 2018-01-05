﻿namespace MarkdownMigration.Convert.Test
{
    using Xunit;

    public class MarkdigMarkdownRewritersTests
    {
        private MarkdownMigrateTool _tool;

        public MarkdigMarkdownRewritersTests()
        {
            _tool = new MarkdownMigrateTool(null);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestResloved_ShortcutXref()
        {
            var source = "@System.String";
            var expected = "@\"System.String\"";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateUnresloved_ShortcutXref()
        {
            var source = "@outlook.com";
            var expected = "@outlook.com";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateAutoLinkXref()
        {
            var source = "<xref:system.string>";
            var expected = "<xref:system.string>";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateNormalLink()
        {
            var source = "[github] (https://github.com)";
            var expected = "[github](https://github.com)";

            var result = _tool.Convert(source, "topic.md");
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
";
            var expected = @"> Video Sample
> [!VIDEO https://channel9.msdn.com]
>
>[!VIDEO https://channel9.msdn.com]
>
";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateMailTo()
        {
            var source = "<Mailto:docs@microsoft.com>";
            var expected = "<docs@microsoft.com>";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected, result);

            result = _tool.Convert(result, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateHtml0()
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

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateHtml1()
        {
            var source = @"
<br>
![](a.png)
<br>
#title";
            var expected = @"
<br>

![](a.png)
<br>

# title";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateHtml2()
        {
            var source = @"
1. list
  <center>![](a.png)</center>
";
            var expected = @"
1. list
   <center><img src=""a.png"" alt=""""/></center>
";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateHtml3()
        {
            var source = @"
<h2>abc</h2>
![](a.png)
";
            var expected = @"
<h2>abc</h2>

![](a.png)
";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateHtml4()
        {
            var source = @"
 <br>
 <center>![](a.png)</center>";
            var expected = @"
 <br>
 
<center><img src=""a.png"" alt=""""/></center>

";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateHtml5()
        {
            var source = @"
<br> 
[link](text)

<br> [link](text)

<br>
[link](text)";
            var expected = @"
<br> 

[link](text)

<br> [link](text)

<br>

[link](text)";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateHeadingWithHref()
        {
            var source = @"##<a id=""WhatIs""></a>What is Twilio?
";
            var expected = @"## <a id=""WhatIs""></a>What is Twilio?
";

            var result = _tool.Convert(source, "topic.md");
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

            var result = _tool.Convert(source, "topic.md");
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

            var result = _tool.Convert(source, "topic.md");
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

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateStrongAndEm()
        {
            var source = "__a__ and _b_ and **a** and **b** and *__ab__*";
            var expected = "__a__ and _b_ and **a** and **b** and *__ab__*";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateAutoLinkWithQuota()
        {
            var source = "This kind of url link such as \'https://github.com\' are not supported in markdig";
            var expected = "This kind of url link such as \'<https://github.com>\' are not supported in markdig";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateBlockQuote()
        {
            var source = @">- One
- Two
    - Three";
            var expected = @"> - One
> - Two
>     - Three";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateImportant()
        {
            var source = @">[!IMPORTANT]
>one

>two";
            var expected = @"> [!IMPORTANT]
> one
> 
> two";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateBlockQuotaWithList()
        {
            var source = @">[!IMPORTANT]
>List:

>- Web
- Email-:
    - Microsoft Outlook 2010/";
            var expected = @"> [!IMPORTANT]
> List:
> 
> - Web
> - Email-:
>     - Microsoft Outlook 2010/";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateBlockQuotaWithMultipleNewLine()
        {
            var source = @"> Line


# title";
            var expected = @"> Line


# title";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateListWithUnsupportedIndent()
        {
            var source = @"2. a
  2. b";
            var expected = @"2. a
   2. b";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateUnorderedList()
        {
            var source = @"-  a
  * b
";
            var expected = @"- a
  * b
";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateNewLine()
        {
            var source = @"- a

- b
";
            var expected = @"- a

- b
";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateListWithNewLine()
        {
            var source = @"1. One

  ![page](a.png)

2. Two

  ![Search for ](b.png)
";
            var expected = @"1. One

   ![page](a.png)

2. Two

   ![Search for ](b.png)
";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateTableBlock0()
        {
            var source = @"a|a
-|-
b|b| |
---";
            var expected = @"| a | a |
|---|---|
| b | b |

---";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateTableBlock1()
        {
            var source = @"text
a|a
-|-
b|b
text";
            var expected = @"text

| a | a |
|---|---|
| b | b |

text";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateTableBlock2()
        {
            var source = @"text

a|a
-|-
b|b

text";
            var expected = @"text

a|a
-|-
b|b

text";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateTableBlock3()
        {
            var source = @"text
f<br>g|a
-|-
b|<ul><li>[text](#bookmark)</li></ul>
text";
            var expected = @"text

| f<br>g |                                         a                                          |
|--------|------------------------------------------------------------------------------------|
|   b    | <ul><li><a href=""#bookmark"" data-raw-source=""[text](#bookmark)"">text</a></li></ul> |

text";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateNewlineInCodeBlock()
        {
            var source = @"  ```
code


```";
            var expected = @"  ```
code
```";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateYamlHeader()
        {
            var source = @"

---
a: b

---
#title";
            var expected = @"---
a: b

---
# title";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateNote()
        {
            var source = @"
>  [!NOTE]
> here's note";
            var expected = @"
> [!NOTE]
> here's note";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }
    }
}
