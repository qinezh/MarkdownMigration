namespace MarkdownMigration.Convert.Test
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
            var expected = "@System.String";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrate_KeepLineEndingIfNothingMigrated()
        {
            var source = "line1\r\nline2\nline3\rline4";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(source, result);
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
            var source = @"[github] (https://github.com)
[Dynamics **36:**5 Certifications](https://www.microsoft.com/en-us/learning/browse-all-certifications.aspx?technology=Microsoft Dynamics 365)
[Dynamics 365 Certifications](https://www.microsoft.com/en-us/learning/browse-all-certifications.aspx?technology=Microsoft Dynamics 365 ""title"")";
            var expected = @"[github](https://github.com)
[Dynamics <strong>36:</strong>5 Certifications](https://www.microsoft.com/en-us/learning/browse-all-certifications.aspx?technology=Microsoft%20Dynamics%20365)
[Dynamics 365 Certifications](https://www.microsoft.com/en-us/learning/browse-all-certifications.aspx?technology=Microsoft%20Dynamics%20365 ""title"")";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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

**More info**<br>

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

**More info**<br>

<div>
This is <strong>markdown</strong> content.
</div>

# header
**markdown**

<div>
This is <strong>markdown</strong> content.
</div>";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateHtml1()
        {
            var source = @"
<br>
![](a.png)
<br>

<p> *bold*

<p>

</p>

<a>

</a>

<p></p>

</p><p>

#title";
            var expected = @"
<br>
<img src=""a.png"" alt=""""/>
<br>

<p> <em>bold</em>

<p>

</p>

<a>

</a>

<p></p>

</p><p>

# title";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
<img src=""a.png"" alt=""""/>
";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
 <center><img src=""a.png"" alt=""""/></center>";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
<a href=""text"" data-raw-source=""[link](text)"">link</a>

<br> [link](text)

<br>
<a href=""text"" data-raw-source=""[link](text)"">link</a>";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateHtml6()
        {
            var source = @"<!--### [Sprint 133](release-notes/sprint133.md) 
### [Sprint 134](release-notes/sprint134.md) -->";
            var expected = @"<!--### [Sprint 133](release-notes/sprint133.md) 
### [Sprint 134](release-notes/sprint134.md) -->";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateStrongAndEm()
        {
            var source = @"__a__ and _b_ and **a:**b and **b** and *__ab__* ***a*** 

*a:*a

**\***";
            var expected = @"**a** and *b* and <strong>a:</strong>b and **b** and ***ab*** ***a*** 

<em>a:</em>a

**\\***";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateStrongAndEm1()
        {
            var source = @"<p> </p>
<td style=""border:1px solid black;"">
**/warnrestart\[:x\]**
</td>";
            var expected = @"<p> </p>
<td style=""border:1px solid black;"">
<strong>/warnrestart[:x]</strong>
</td>";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateStrongAndEm2()
        {
            var source = @"""*https://docs.microsoft.com/en-us/dotnet/api/microsoft.windowsazure.storage?view=azure-dotnet*""";
            var expected = @"""*<https://docs.microsoft.com/en-us/dotnet/api/microsoft.windowsazure.storage?view=azure-dotnet>*""";
            var result = _tool.Convert(source, "topic.md");

            Assert.Equal(expected.Replace("\r\n", "\n"), result);
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
        public void TestMigrateAutoLinkInTable()
        {
            var source = @"|Claim type (URI)|
| ----- |
|http://schemas.microsoft.com/ws/2008/06/identity/claims/expiration|";
            var expected = @"|                          Claim type (URI)                          |
|--------------------------------------------------------------------|
| http://schemas.microsoft.com/ws/2008/06/identity/claims/expiration |

";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestCodeSnippetInTable()
        {
            var source = @"a
|**Method**|**Description**|**Example**|
|----------|---------------|----------|
|`11`|12|13[!code-csharp[Main](introducing-razor-syntax-c/samples/sample28.cs)]13|
|`21`|22|23[!code-csharp[Main](introducing-razor-syntax-c/samples/sample29.cs)]23|
a";
            var expected = @"a
:::row:::
    :::column:::
        **Method**
    :::column-end:::
    :::column:::
        **Description**
    :::column-end:::
    :::column:::
        **Example**
    :::column-end:::
:::row-end:::
* * *
:::row:::
    :::column:::
        `11`
    :::column-end:::
    :::column:::
        12
    :::column-end:::
    :::column:::
        13
        [!code-csharp[Main](introducing-razor-syntax-c/samples/sample28.cs)]
        13
    :::column-end:::
:::row-end:::
* * *
:::row:::
    :::column:::
        `21`
    :::column-end:::
    :::column:::
        22
    :::column-end:::
    :::column:::
        23
        [!code-csharp[Main](introducing-razor-syntax-c/samples/sample29.cs)]
        23
    :::column-end:::
:::row-end:::
a";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateAutoLinkInUriLink()
        {
            var source = @"[link](text (https://msdn.microsoft.com/library/ms732023(v=vs.110).aspx').";
            var expected = @"[link](text (<https://msdn.microsoft.com/library/ms732023(v=vs.110).aspx>').";

            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateTableBlock3()
        {
            var source = @"text
f<br>g|a <br/>`b`
-|-
b|<ul><li>[te\|xt](#bookmark)</li></ul> `b`
text";
            var expected = @"text

| f<br>g |                a <br/>`b`                 |
|--------|-------------------------------------------|
|   b    | <ul><li>[te\|xt](#bookmark)</li></ul> `b` |

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
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateIndentCodeInList()
        {
            var source = @"
1. list


    code
";
            var expected = @"
1. list


~~~
code
~~~
";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMigrateCodeSnippet()
        {
            var source = @"[!code-csharp[main](../a\b.cs?name=add ""Startup.cs"")]";
            var expected = @"[!code-csharp[main](../a/b.cs?name=add ""Startup.cs"")]";
            var result = _tool.Convert(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_InclusionPath()
        {
            var source = @"[!INCLUDE [title](.\..\..\..\includes\a.md)]";
            var expected = @"[!INCLUDE [title](./../../../includes/a.md)]

";
            var result = _tool.Convert(source, "topic.md");

            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }
    }
}
