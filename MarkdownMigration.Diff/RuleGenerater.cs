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
        public static Dictionary<string, DiffRule> AppendTextRule(this Dictionary<string, DiffRule> rules)
        {
            rules.Add("#text", new DiffRule
            {
                IsIgnore = (node) => String.IsNullOrEmpty(node.InnerText.Nomalize())
            });

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
            rules.Add("Xref", new DiffRule
            {
                CompareChildrenOnly = (node) =>
                {
                    var rawSource = node.Attributes["data-raw-source"].Value;
                    return rawSource[1] != '"';
                },
                Process = (node) =>
                {
                    var rawSource = node.Attributes["data-raw-source"].Value;
                    if (rawSource[1] != '"')
                    {
                        node.InnerHtml = rawSource;
                    }
                },
                CompareAttributes = new string[] { "href" }
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
                    text = text.Replace("\r\n", "\n")
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
                    var src = node.Attributes["src"].Value;
                    
                    if(src.StartsWith("https://channel9.msdn.com") && !src.EndsWith("?nocookie=true"))
                    {
                        node.Attributes["src"].Value = src + "?nocookie=true";
                    }

                    if(src.StartsWith("https://www.youtube") && !src.Contains("-nocookie"))
                    {
                        node.Attributes["src"].Value = src.Replace("https://www.youtube", "https://www.youtube-nocookie");
                    }
                },
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
                },
                CompareAttributes = new string[] { "id" }
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
                CompareChildrenOnly = (node) => true,
                Process = (node) =>
                {
                    node.InsertBefore(node.OwnerDocument.CreateTextNode("**"), node.FirstChild);
                    node.InsertAfter(node.OwnerDocument.CreateTextNode("**"), node.LastChild);
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
