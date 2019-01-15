using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HtmlCompare
{
    public  static class RuleGenerater
    {
        public static Dictionary<string, DiffRule> AppendDocumentRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("#document", new DiffRule
            {
                CompareChildrenOnly = (node) => true,
                IsIgnore = null
            });

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendTextRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("#text", new DiffRule());

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendPRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("p", new DiffRule
            {
                CompareChildrenOnly = (node) => true
            });

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendCommentRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("#comment", new DiffRule
            {
                IsIgnore = (node) => true
            });

            return rules;
        }

        /// <summary>
        /// This rule ignores the CodeSnippet Warning in html(Markdig), in dfm it is a comment.
        /// If CodeSnippet not found in both side, they would be both ignored.
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public static Dictionary<string, DiffRule> AppendCodeSnippetWarningRule(this Dictionary<string, DiffRule> rules)
        {
            var codeSnippetWarning = @"<div class=""WARNING"">
<h5>WARNING</h5>
<p>It looks like the sample you are looking for does not exist.</p>
</div>";

            rules.Add("div", new DiffRule
            {
                IsIgnore = (node) =>
                {
                    return node.OuterHtml == codeSnippetWarning;
                }
            });

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendPreRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("pre", new DiffRule
            {
                CompareChildrenOnly = (node) => true
            });

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendXrefRule(this Dictionary<string, DiffRule> rules)
        {
            // This logic is for azure-docs-pr, will not impact other migration
            // Will remove after azure-docs-pr migrated
            var specialListForAzureDocsPr = @"XXXX.com@azurestack.local@azurestack.local@xxxonline.com@contoso.com.’@domainname.com@flex.cd-adapco.com@mycompany.com@mycompany.com@MSFTAzureMedia@Microsoft.com@SERVERNAME@outlook.com)@AD.COM@microsoft.com)@hotmail.com)@contoso.com_@contosomigration.onmicrosoft.com@leader4vb.eastus.cloudapp.azure.com)@example.com@fabrikamonline.com@contoso.com.’@contoso.com.’@mail.windowsazure.com@item()@triggerOutputs()@azure.com@department.contoso.com)@mail.windowsazure.com@fabraikam.com@contoso.com_@hditutorialdata.blob.core.windows.net/twitter.hql@contosos.com@mystorage.blob.core.windows.net/@azurestack.local@example.com@azurestack.local@myazuredirectory.onmicrosoft.com@location.com@mydomain.onmicrosoft.com@azurestack.local@azurestack.local@AD.COM@contoso158.onmicrosoft.com@hditutorialdata.blob.core.windows.net/contacts.txt@hditutorialdata.blob.core.windows.net/@blob_storage_account_name.blob.core.windows.net/blob_name@MS@mysamplegroup.visualstudio.com:22/MyTeam/_git/MyTeamProjectTemplate@hk-cas-template.cloudapp.net:/home/localadmin/downloads/server-jre-8u5-linux-x64.tar.gz@yourdomain.com@NESTLEVEL@Ask@outlook.com)@azuremlsampleexperiments.blob.core.windows.net/raw/@local_variable@notcontoso.com@OutputCache@fabrikam.com)@Ask@cloudcruiser.com@verified.contoso.com@contoso.usa)@azure.com@contoso.onmicrosoft.com)@IDENTITY@emulated@contoso.onmicrosoft.com@drumkit.onmicrosoft.com@contoso.com@gmail.com@adventureworks.com@contoso.com'@adventureworks.com\@woodgroveonline.com@Contoso.com@f128.info@AzureSupport@contoso.com""@ contoso.com@smtp: jd @contoso.com@org.com@example.com)@myb2ctenant.onmicrosoft.com@fourthcoffeexyz.onmicrosoft.com@domain.onmicrosoft.com)@comcast.net@microsoft.com@domain.com@fabrikam.com@contoso.com.'@contoso.com)@flatterfiles.com@contosob2c.onmicrosoft.com@outlook.com@domainservicespreview.onmicrosoft.com@statuspage.io@CONTOSO100.COM@blueskyabove.onmicrosoft.com@contoso100.com@us.contoso.com@contoso.com}@sub.contoso.com@litware.com)@service.microsoft.com@contoso.com’@contoso.com’@eu.contoso.com)@contoso.com”@contoso.com.test@fourthcoffee.xyz@tenant-name.onmicrosoft.com@fabrikam.onmicrosoft.com@contoso.com”@live.com)@contoso.com`@azurecontoso.onmicrosoft.com@live.com"
.Split('@').ToList();
            specialListForAzureDocsPr.Add("contoso.com,smtp:jd@contoso.com");
            specialListForAzureDocsPr.Add("contoso.com;email2@contoso.com");
            specialListForAzureDocsPr.Add(@")                                                                                     | Event &amp;#124; where Computer matches regex");
            rules.Add("xref", new DiffRule
            {
                IsIgnore = null,
                CompareAttributes = new string[] { "href" },
                Process = (node) =>
                {
                    var attribute = node.Attributes["href"];
                    if (attribute != null && specialListForAzureDocsPr.Contains(attribute.Value))
                    {
                        node.InnerHtml = "@" + attribute.Value;
                    }
                },
                CompareChildrenOnly = (node) =>
                {
                    var attribute = node.Attributes["href"];
                    return attribute != null && specialListForAzureDocsPr.Contains(attribute.Value);
                }
            });

            return rules;
        }

        private static readonly Regex CodeInMutiLine = new Regex(@"<code([^<>]*?)>[\s\S]*?</code>", RegexOptions.Compiled);
        public static Dictionary<string, DiffRule> AppendCodeRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("code", new DiffRule
            {
                Process = (node) =>
                {
                    var text = node.InnerText.Trim();
                    text = text.Replace("&#13;", "\r")
                    .Replace("&#10;", "\n")
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n");

                    var result = Regex.Replace(text, @"\s*\n\s*", " ");
                    result = Regex.Replace(result, " +", " ");

                    node.InnerHtml = result;
                }
            });

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendVideoRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("iframe", new DiffRule
            {
                Process = (node) =>
                {
                    var srcAttribute = node.Attributes["src"];
                    if (node.Attributes["src"] == null) return;

                    var src = srcAttribute.Value;
                    
                    if(src.StartsWith("https://channel9.msdn.com") && !src.EndsWith("?nocookie=true"))
                    {
                        node.Attributes["src"].Value = src + "?nocookie=true";
                    }

                    if(src.StartsWith("https://www.youtube") && !src.Contains("-nocookie"))
                    {
                        node.Attributes["src"].Value = src.Replace("https://www.youtube", "https://www.youtube-nocookie");
                    }
                },
                IsIgnore = null,
                CompareAttributes = new string[] { "src" }
            });

            return rules;
        }

        private static readonly Regex AINHeader = new Regex(@"<a name=""(.*?)""></a>", RegexOptions.Compiled);

        public static Dictionary<string, DiffRule> AppendHeadingRule(this Dictionary<string, DiffRule> rules)
        {
            var headingRule = new DiffRule
            {
                Process = (node) =>
                {
                    node.InnerHtml = AINHeader.Replace(node.InnerHtml, m =>
                    {
                        return "&lt;a name=" + m.Groups[1].Value + "&gt;";
                    });
                }
            };

            rules.Add("h1", headingRule);
            rules.Add("h2", headingRule);
            rules.Add("h3", headingRule);
            rules.Add("h4", headingRule);
            rules.Add("h5", headingRule);
            rules.Add("h6", headingRule);

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendALinkRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("a", new DiffRule
            {
                CompareChildrenOnly = (node) => true
            });

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendStrongRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("strong", new DiffRule
            {
                Process = (node) =>
                {
                    if (node.LastChild.Name == "em" && node.ChildNodes.Count == 1)
                    {
                        node.Name = "em";
                        node.LastChild.Name = "strong";
                    }
                }
            });

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendEmRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("em", new DiffRule
            {
                CompareChildrenOnly = (node) => true,
                Process = (node) =>
                {
                    node.InsertBefore(node.OwnerDocument.CreateTextNode("*"), node.FirstChild);
                    node.InsertAfter(node.OwnerDocument.CreateTextNode("*"), node.LastChild);
                }
            });

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendDelRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("del", new DiffRule
            {
                CompareChildrenOnly = (node) => true,
                Process = (node) =>
                {
                    node.InsertBefore(node.OwnerDocument.CreateTextNode("~~"), node.FirstChild);
                    node.InsertAfter(node.OwnerDocument.CreateTextNode("~~"), node.LastChild);
                }
            });

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendULRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("ul", new DiffRule
            {
                CompareChildrenOnly = (node) => true,
            });

            return rules;
        }

        public static Dictionary<string, DiffRule> AppendBlockquoteRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("blockquote", new DiffRule());

            return rules;
        }
    }
}
