using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCompare
{
    public class HtmlCompareStack
    {
        private Stack<HtmlNode> Stack = new Stack<HtmlNode>();
        private Dictionary<string, DiffRule> Rules = new Dictionary<string, DiffRule>();

        private const string StartLine = "sourcestartlinenumber";
        private const string EndLine = "sourceendlinenumber";

        public HtmlCompareStack(HtmlNode root, Dictionary<string, DiffRule> rules)
        {
            this.Rules = rules;
            Push(root);
        }

        public void Push(HtmlNode root)
        {
            var node = root;
            while (node != null)
            {
                while (node != null && Rules.ContainsKey(node.Name) && Rules[node.Name].IsIgnore != null && Rules[node.Name].IsIgnore(node))
                {
                    node = node.NextSibling;
                }

                if (node == null) return;

                if (Rules.ContainsKey(node.Name) && Rules[node.Name].Process != null)
                {
                    Rules[node.Name].Process(node);
                }

                Stack.Push(node);
                node = node.FirstChild;
            }
        }

        public HtmlNode Pop()
        {
            if (Stack.Count > 0)
                return Stack.Pop();

            return null;
        }

        public HtmlNode Next(HtmlNode current)
        {
            if (current == null) return null;
            if (current.NextSibling != null)
            {
                Push(current.NextSibling);
            }

            if (Stack.Count > 0) return Stack.Pop();

            return null;
        }

        public HtmlNode GetCompareNode(HtmlNode root)
        {
            var node = root;
            while (IsCompareChildrenOnly(node))
            {
                node = Next(node);
            }

            return node;
        }

        private bool IsCompareChildrenOnly(HtmlNode node)
        {
            return node != null && Rules.ContainsKey(node.Name)
                    && Rules[node.Name].CompareChildrenOnly != null
                    && Rules[node.Name].CompareChildrenOnly(node);
        }

        public Span GetSpanFromStack()
        {
            while (Stack.Count > 0)
            {
                var current = Stack.Pop();
                var span = GetSpan(current);
                if (span.Start > 0)
                    return span;
            }

            return default(Span);
        }

        private Span GetSpan(HtmlNode current)
        {
            if (current.Attributes.Contains(StartLine) && current.Attributes.Contains(EndLine))
            {
                return new Span
                {
                    Start = int.Parse(current.Attributes[StartLine].Value),
                    End = int.Parse(current.Attributes[EndLine].Value)
                };
            }
            return default(Span);
        }
    }
}
