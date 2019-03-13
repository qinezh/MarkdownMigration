using HtmlAgilityPack;
using MarkdownMigration.Common;
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
        public string TagName;
        public int Start;
        public int End;
    }

    public class HtmlDiffTool
    {
        private Dictionary<string, DiffRule> Rules = new Dictionary<string, DiffRule>();
        private readonly List<string> NoteTypes = new List<string> { "NOTE", "TIP", "WARNING", "IMPORTANT", "CAUTION" };
        private static readonly Regex CodesnippetError = new Regex(@"<!-- BEGIN ERROR CODESNIPPET: Unable to find(.*?)<!--END ERROR CODESNIPPET -->", RegexOptions.Compiled);

        public string DfmHtml { get; set; }
        public string MarkdigHtml { get; set; }

        public HtmlDiffTool(string dfmHtml, string markdigHtml, bool isPureCompare = false)
        {
            this.DfmHtml = dfmHtml;
            this.MarkdigHtml = markdigHtml;

            if(isPureCompare)
            {
                PureCompareBuild();
            }
            else
            {
                Build();
            }
        }

        private void Build()
        {
            this.Rules.AppendDocumentRule()
                .AppendPreRule()
                .AppendTextRule()
                .AppendPRule()
                .AppendXrefRule()
                .AppendCodeRule()
                .AppendVideoRule()
                //.AppendHeadingRule()
                //.AppendALinkRule()
                .AppendStrongRule()
                //.AppendEmRule()
                .AppendDelRule()
                .AppendBlockquoteRule()
                .AppendCommentRule()
                .AppendOrderedListRule()
                .AppendCodeSnippetWarningRule()
                .AppendEmojiRule();
        }

        private void PureCompareBuild()
        {
            this.Rules.AppendDocumentRule()
                .AppendPreRule()
                .AppendTextRule()
                .AppendStrongRule()
                .AppendPRule()
                .AppendCodeRule()
                .AppendDelRule()
                .AppendBlockquoteRule()
                .AppendCommentRule()
                .AppendOrderedListRule()
                .AppendCodeSnippetWarningRule()
                .AppendEmojiRule();
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
                try
                {
                    dfmHtml = dfmNode != null ? dfmNode.OuterHtml : string.Empty;
                    markdigHtml = markdigNode != null ? markdigNode.OuterHtml : string.Empty;
                }
                catch (Exception)
                {
                    sourceDiffSpan = dfmStack.GetSpanFromStack();
                    return false;
                }

                if (dfmNode == null || markdigNode == null || dfmNode.Name != markdigNode.Name || !CompareAttributes(dfmNode, markdigNode))
                {
                    dfmStack.Push(dfmNode);
                    sourceDiffSpan = dfmStack.GetSpanFromStack();
                    return false;
                }

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
                    var dfmAttribute = dfmNode.Attributes[attribute];
                    var markdigAttribute = markdigNode.Attributes[attribute];
                    if (dfmAttribute == null && markdigAttribute == null) continue;
                    if (dfmAttribute == null || markdigAttribute == null) return false;
                    if (dfmAttribute.Value.LocalNomalize() != markdigAttribute.Value.LocalNomalize())
                        return false;
                }
            }

            return true;
        }

        private HtmlDocument StringToHtml(string source)
        {
            var doc = new HtmlDocument();
            doc.OptionCheckSyntax = true;
            doc.OptionWriteEmptyNodes = true;
            doc.OptionOutputAsXml = true;
            source = CodesnippetError.Replace(source, m => string.Empty);
            doc.LoadHtml(source);

            return doc;
        }
    }
}
