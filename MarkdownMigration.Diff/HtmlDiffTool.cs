using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace HtmlCompare
{
    public struct Span
    {
        public int Start;
        public int End;
    }

    public class HtmlDiffTool
    {
        private Dictionary<string, DiffRule> Rules = new Dictionary<string, DiffRule>();

        public string DfmHtml { get; set; }
        public string MarkdigHtml { get; set; }

        public HtmlDiffTool(string dfmHtml, string markdigHtml)
        {
            this.DfmHtml = dfmHtml;
            this.MarkdigHtml = markdigHtml;

            Build();
        }

        private void Build()
        {
            this.Rules.AppendPreRule()
                .AppendTextRule()
                .AppendPRule()
                .AppendXrefRule()
                .AppendCodeRule()
                .AppendVideoRule()
                .AppendHeadingRule()
                .AppendALinkRule()
                .AppendStrongRule()
                .AppendEmRule()
                .AppendDelRule()
                .AppendBlockquoteRule();
        }

        public bool Compare()
        {
            Span sourceDiffSpan;
            string dfmHtml, markdigHtml;

            return Compare(out sourceDiffSpan, out dfmHtml, out markdigHtml);
        }

        public bool Compare(out Span sourceDiffSpan, out string dfmHtml, out string markdigHtml)
        {
            sourceDiffSpan = new Span();
            dfmHtml = markdigHtml = string.Empty;

            var dfmDoc = StringToHtml(DfmHtml);
            var markdigDoc = StringToHtml(MarkdigHtml);

            var dfmStack = new HtmlCompareStack(dfmDoc.DocumentNode, Rules);
            var markdigStack = new HtmlCompareStack(markdigDoc.DocumentNode, Rules);

            var dfmNode = dfmStack.Pop();
            var markdigNode = markdigStack.Pop();

            while (dfmNode != null || markdigNode != null)
            {
                dfmNode = dfmStack.GetCompareNode(dfmNode);
                markdigNode = markdigStack.GetCompareNode(markdigNode);

                if (dfmNode == null && markdigNode == null) return true;

                dfmHtml = dfmNode != null ? dfmNode.InnerHtml : string.Empty;
                markdigHtml = markdigNode!= null? markdigNode.InnerHtml : string.Empty;

                if (dfmNode == null || markdigNode == null || dfmNode.Name != markdigNode.Name)
                {
                    dfmStack.Push(dfmNode);
                    sourceDiffSpan = dfmStack.GetSpanFromStack();
                    return false;
                }

                //if(!CompareAttributes(dfmNode, markdigNode))
                //{
                //    return false;
                //}

                if (dfmNode.Name == "#text")
                {
                    var dfmText = dfmNode.InnerText.LocalNomalize();
                    var markdigText = markdigNode.InnerText.LocalNomalize();

                    if (dfmText.Length == markdigText.Length)
                    {
                        if (dfmText == markdigText)
                        {
                            dfmNode = dfmStack.Next(dfmNode);
                            markdigNode = markdigStack.Next(markdigNode);
                            continue;
                        }

                        sourceDiffSpan = dfmStack.GetSpanFromStack();
                        return false;
                    }

                    if (dfmText.Length > markdigText.Length)
                    {
                        if (!dfmText.StartsWith(markdigText))
                        {
                            sourceDiffSpan = dfmStack.GetSpanFromStack();
                            return false;
                        }

                        dfmNode.InnerHtml = dfmText.Substring(markdigText.Length);
                        markdigNode = markdigStack.Next(markdigNode);
                        continue;
                    }

                    if (dfmText.Length < markdigText.Length)
                    {
                        if (!markdigText.StartsWith(dfmText))
                        {
                            sourceDiffSpan = dfmStack.GetSpanFromStack();
                            return false;
                        }

                        markdigNode.InnerHtml = markdigText.Substring(dfmText.Length);
                        dfmNode = dfmStack.Next(dfmNode);
                        continue;
                    }
                }
                else
                {
                    dfmNode = dfmStack.Next(dfmNode);
                    markdigNode = markdigStack.Next(markdigNode);
                }
            }

            return true;
        }

        private bool CompareAttributes(HtmlNode dfmNode, HtmlNode markdigNode)
        {
            if (Rules.ContainsKey(dfmNode.Name) && Rules[dfmNode.Name].CompareAttributes != null)
            {
                foreach (var attribute in Rules[dfmNode.Name].CompareAttributes)
                {
                    if (dfmNode.Attributes[attribute].Value != markdigNode.Attributes[attribute].Value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private HtmlDocument StringToHtml(string source)
        {
            var doc = new HtmlDocument();
            doc.OptionCheckSyntax = true;
            doc.OptionFixNestedTags = true;
            doc.OptionWriteEmptyNodes = true;
            doc.OptionOutputAsXml = true;
            doc.LoadHtml(source);

            return doc;
        }
    }
}
