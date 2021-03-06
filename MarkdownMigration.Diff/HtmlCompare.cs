﻿using HtmlAgilityPack;
using MarkdownMigration.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace HtmlCompare
{
    public class HtmlCompare
    {
        static ConcurrentBag<DiffResult> Result = new ConcurrentBag<DiffResult>();
        //static ConcurrentDictionary<string, HtmlDocument> docs = new ConcurrentDictionary<string, HtmlDocument>();

        static List<Func<string, string>> StringMigrationSteps = new List<Func<string, string>>()
        {
            // need string
            IgnoreComments,
            IgnoreSourceInfo,
            IgnoreXref,
            IgnoreSourceInfo1,
            IgnoreEmptyP,
            FormatCustomTags,
            FormatTableStyle,
            IgnoreCodeLastLine,
            IgnorePre,
            IgnoreCodeInMutiLine,
            IgnoreNocookie,
            IgnoreAINHeader,
            IgnoreTryIt,
            IgnorePinli,

            // need parse to html
            //Xhtml,
            UnifySpaceAndNewLine,
            TrimCustomTag,
            IgnoreHeadingIdDash,
            IgnoreTaginP,
            IgnoreAutoLink,
            IgnoreBackSlashInAutolink,
            IgnoreAutolink1,
            IgnoreStrongEm,
            IgnoreAltInImage,
            IgnoreTrimTd,
            IgnoreDel,
            IgnoreConnectedUL,
            IgnoreEmptyBlockquote,
            IgnoreEmptyConnectedBlockQuote,

            // format xml
            IgnoreNewlineAroundTag,
            UnifySpaceAndNewLine,
            TrimCustomTag,
            DecodeAndFormatXml
        };

        static List<Func<string, string>> NoMatterSteps = new List<Func<string, string>>()
        {
            DecodeAndFormatXml,

            // need string
            IgnoreComments,
            IgnoreSourceInfo,
            IgnoreSourceInfo1,
            IgnoreEmptyP,
            FormatCustomTags,
            FormatTableStyle,
            IgnoreCodeLastLine,
            IgnorePre,
            IgnorePinli,
            IgnoreEmptyBlockquote,
            IgnoreEmptyConnectedBlockQuote,

            // need parse to html
            //Xhtml,
            IgnoreHeadingIdDash,
            IgnoreTrimTd,
            IgnoreDel,            

            // format xml
            IgnoreNewlineAroundTag,
            UnifySpaceAndNewLine,
            TrimCustomTag,
            IgnoreSpan,
            DecodeAndFormatXml
        };

        static bool debug = false;
        static string targetFileName = "docs-recommendation-function-spec.html";
        static ConcurrentDictionary<string, string> htmlToSourceFileMapping = new ConcurrentDictionary<string, string>();

        public static void CompareHtmlFromFolder(string folderA, string folderB, out List<DiffResult> differentFiles, out List<string> allFiles, bool diffBuildPackage)
        {
            // Prepare
            string timeStampStr = DateTime.Now.ToString("yy-MM-dd-hh-mm-ss");
            
            htmlToSourceFileMapping = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(File.ReadAllText(Path.Combine(folderA, "htmlSourceMapping.json")));
            
            Console.WriteLine($"folderA: {folderA}");
            Console.WriteLine($"folderB: {folderB}");

            string filePattern = debug ? targetFileName : "*.html";

            var filesA = Directory.GetFiles(folderA, filePattern, SearchOption.AllDirectories);
            var setA = new HashSet<string>(filesA);
            var filesB = Directory.GetFiles(folderB, filePattern, SearchOption.AllDirectories);
            var setB = new HashSet<string>(filesB);

            var AnotB = filesA.Where(a => !setB.Contains(a.Replace(folderA, folderB))).ToList();
            var BnotA = filesB.Where(b => !setA.Contains(b.Replace(folderB, folderA))).ToList();
            var AinB = setA.Except(AnotB);

            ConcurrentBag<string> result_Same = new ConcurrentBag<string>();
            ConcurrentBag<string> result_Equal = new ConcurrentBag<string>();
            ConcurrentBag<string> result_Different = new ConcurrentBag<string>();
            List<string> result_Unique = new List<string>();
            result_Unique.AddRange(AnotB);
            result_Unique.AddRange(BnotA);

            // Compare
            //ProgressHelper ph0 = ProgressHelper.CreateStartedInstance(AinB.Count(), "Proccessing Files");

            int parallel = debug ? 1 : 16;
            //PauseWhenDebug();

            Parallel.ForEach(AinB, new ParallelOptions() { MaxDegreeOfParallelism = parallel }, fileA =>
            {
                var fileB = fileA.Replace(folderA, folderB);
                var rawContentA = File.ReadAllText(fileA);
                var rawContentB = File.ReadAllText(fileB);

                // Step 1: Compare Raw Content
                if (rawContentA == rawContentB)
                {
                    result_Same.Add(fileA);
                }
                else
                {
                    // Step 2: Migrate Html
                    //string migratedContentA, migratedContentB;
                    HtmlDiffTool hdt = new HtmlDiffTool(rawContentA, rawContentB);
                    Span sourceDiffSpan;
                    string dfmHtml, markdigHtml;

                    if (hdt.Compare(out sourceDiffSpan, out dfmHtml, out markdigHtml))
                    {
                        result_Equal.Add(fileA);
                    }
                    else
                    {
                        var diffResult = new DiffResult
                        {
                            FileName = diffBuildPackage ? fileA.Replace(folderA, "") : htmlToSourceFileMapping[fileA],
                            SourceFileUrl = htmlToSourceFileMapping[fileA],
                            DFMHtml = dfmHtml,
                            MarkdigHtml = markdigHtml,
                            SourceDiffSpan = sourceDiffSpan,
                            DiffTagName = string.IsNullOrEmpty(sourceDiffSpan.TagName) ? "html" : sourceDiffSpan.TagName
                        };

                        Result.Add(diffResult);
                    }
                }
                //if(diffBuildPackage) ph0.Increase();
            });

            // Result
            int uniqueFiles = result_Unique.Count();
            int allFilesCount = (filesA.Count() + filesB.Count() + uniqueFiles) / 2;
            Console.WriteLine($"All Files: {allFilesCount}");
            Console.WriteLine($"Shared Files: {allFilesCount - uniqueFiles}");
            Console.WriteLine($"Same Files: {result_Same.Count()}");
            Console.WriteLine($"Equal Files: {result_Equal.Count()}");
            Console.WriteLine($"Different Files: {Result.Count()}");
            Console.WriteLine($"Unique Files: {uniqueFiles}");
            
            differentFiles = Result.ToList();
            allFiles = filesA.Select(fileA => diffBuildPackage ? fileA.Replace(folderA, "") : htmlToSourceFileMapping[fileA]).ToList();
        }

        #region Heplers
        static string LocateEXE(String filename)
        {
            List<string> folders = new List<string>() { $"{Environment.GetEnvironmentVariable("ProgramFiles(x86)")}", $"{Environment.GetEnvironmentVariable("ProgramFiles")}" };

            String path = Environment.GetEnvironmentVariable("path");
            folders.AddRange(path.Split(';'));
            foreach (String folder in folders)
            {
                if (File.Exists(folder + filename))
                {
                    return folder + filename;
                }
                else if (File.Exists(folder + "\\" + filename))
                {
                    return folder + "\\" + filename;
                }
            }

            return String.Empty;
        }

        static void Confirm(string folderA, string folderB)
        {
            if (string.IsNullOrEmpty(folderA) || string.IsNullOrEmpty(folderB) || !Directory.Exists(folderA) || !Directory.Exists(folderB))
            {
                Console.WriteLine($"Invalid folders.");
                System.Environment.Exit(0);
            }

            Console.WriteLine($"Diff the two folders: {folderA}, {folderB}? (Y/N)");
            var key = Console.ReadKey().KeyChar;
            Console.WriteLine();
            if (key != 'y' && key != 'Y')
            {
                Console.WriteLine($"Exited.");
                System.Environment.Exit(0);
            }
        }

        static void CreateFile(string path, string content)
        {
            //if (debug) return; 

            var folder = Path.GetDirectoryName(path);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            File.WriteAllText(path, content);
        }

        static void PauseWhenDebug()
        {
            if (debug)
            {
                ;
            }
        }

        static string ReplaceSpaceAndNewLine(string source)
        {
            return Regex.Replace(source, "[ \n]+", m =>
            {
                if (m.Value.Contains('\n'))
                {
                    return "\n";
                }
                else
                {
                    return " ";
                }
            });
        }

        static void UnifySpaceAndNewLineHelper(HtmlNode node)
        {
            if (node == null || node.Name == "pre") return;

            var preChildren = node.SelectNodes(".//pre");

            if (preChildren != null && preChildren.Count != 0)
            {
                foreach (var child in node.ChildNodes)
                {
                    UnifySpaceAndNewLineHelper(child);
                }
            }
            else
            {
                try
                {
                    var replaced = ReplaceSpaceAndNewLine(node.InnerHtml);
                    if (node.InnerHtml != ReplaceSpaceAndNewLine(node.InnerHtml))
                    {
                        node.InnerHtml = ReplaceSpaceAndNewLine(node.InnerHtml);
                    }
                }
                catch (Exception)
                {
                    // declaration and comment does not allow setting InnerXml
                }
            }
        }

        static string PrettyPrintXml(XmlDocument xml)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                xml.Save(writer);
            }

            return sb.ToString();
        }

        static HtmlDocument StringToHtml(string source)
        {
            // There is an issue, &#39; => &amp;#39;
            //var encodedSource = source.Replace("&", "&amp;");

            var doc = new HtmlDocument();
            doc.OptionCheckSyntax = true;
            doc.OptionFixNestedTags = true;
            doc.OptionWriteEmptyNodes = true;
            doc.OptionOutputAsXml = true;
            doc.LoadHtml(source);

            return doc;
        }

        static string HtmlToString(HtmlDocument html)
        {
            StringBuilder sb = new StringBuilder();
            using (TextWriter stringWriter = new StringWriter(sb))
            {
                html.Save(stringWriter);
            }

            var rawOuterHtml = sb.ToString();

            // There is an issue, &#39; => &amp;#39;
            //var decodedResult = rawOuterHtml.Replace("&amp;", "&");
            var removedDeclaration = Regex.Replace(rawOuterHtml, @"^<\?xml version=""1\.0"" encoding=""gb2312""\?>", "");
            removedDeclaration = Regex.Replace(removedDeclaration, @"^<\?xml version=""1\.0"" encoding=""iso-8859-1""\?>", "");
            return removedDeclaration;
        }
        #endregion

        #region Migration Steps
        static string Xhtml(string source)
        {
            var doc = StringToHtml(source);

            return HtmlToString(doc);
        }

        static string DecodeAndFormatXml(string source)
        {
            // only decode known strings in <code>
            var decodeSource = source.Replace("&#39;", "'");
            decodeSource = decodeSource.Replace("%7E", "~");
            decodeSource = decodeSource.Replace("%7B", "{");
            decodeSource = decodeSource.Replace("%7D", "}");
            decodeSource = decodeSource.Replace(' ', ' ');

            var doc = StringToHtml(decodeSource);

            var textList = doc.DocumentNode.SelectNodes($"//text()[normalize-space(.) != '' and not(ancestor::code)]");

            if (textList != null)
            {
                foreach (var text in textList)
                {
                    var replace = HtmlEntity.DeEntitize(text.InnerHtml);

                    if (text.InnerHtml != replace)
                    {
                        text.InnerHtml = replace;
                    }
                }
            }

            var htmlString = HtmlToString(doc);

            return FormatXml(htmlString);
        }

        static string FormatXml(string source)
        {
            var xhtml = Xhtml(source);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xhtml.Replace("&", "&amp;"));

            return PrettyPrintXml(doc).Replace("&amp;", "&");
        }

        static string TrimCustomTag(string source)
        {
            string[] tags = { "p", "li", "h1", "h2", "h3", "h4", "h5", "h6" };
            var doc = StringToHtml(source);

            foreach (var tag in tags)
            {
                var nodeList = doc.DocumentNode.SelectNodes($"//{tag}");
                if (nodeList == null) continue;
                for (int i = (nodeList.Count - 1); i >= 0; i--)
                {
                    var node = nodeList[i];
                    if (node.InnerHtml != node.InnerHtml.Trim())
                    {
                        node.InnerHtml = node.InnerHtml.Trim();
                    }
                }
            }

            return HtmlToString(doc);
        }

        static string IgnoreComments(string source)
        {
            var doc = StringToHtml(source);
            var nodes = doc.DocumentNode.SelectNodes("//comment()");
            if (nodes != null)
            {
                foreach (HtmlNode comment in nodes)
                {
                    comment.ParentNode.RemoveChild(comment);
                }
            }
            return HtmlToString(doc);
        }

        private static readonly Regex CodeInMutiLine = new Regex(@"<code([^<>]*?)>[\s\S]*?</code>", RegexOptions.Compiled);

        static string IgnoreCodeInMutiLine(string source)
        {
            return CodeInMutiLine.Replace(source, m =>
            {
                var result = Regex.Replace(m.ToString(), @"\s*\n\s*", " ");
                result = Regex.Replace(result, "<code([^<>]*?)> *", "<code$1>");
                result = Regex.Replace(result, " *</code>", "</code>");
                result = Regex.Replace(result, " +", " ");
                return result;
            });
        }
        private static readonly Regex s1 = new Regex(@" sourcefile=""[^""<>]*?""", RegexOptions.Compiled);
        private static readonly Regex s2 = new Regex(@" sourcestartlinenumber=""[^""<>]*?""", RegexOptions.Compiled);
        private static readonly Regex s3 = new Regex(@" sourceendlinenumber=""[^""<>]*?""", RegexOptions.Compiled);
        private static readonly Regex s4 = new Regex(@" data-raw-source=""[^""<>]*?""", RegexOptions.Compiled);
        private static readonly Regex s5 = new Regex(@" data-throw-if-not-resolved=""[^""<>]*?""", RegexOptions.Compiled);
        private static readonly Regex s6 = new Regex(@" start=""[^""<>]*?""", RegexOptions.Compiled);

        static string IgnoreSourceInfo(string source)
        {
            var result = s1.Replace(source, m => string.Empty);
            result = s2.Replace(result, m => string.Empty);
            result = s3.Replace(result, m => string.Empty);
            result = s5.Replace(result, m => string.Empty);
            result = s6.Replace(result, m => string.Empty);
            return result;
        }

        static string IgnoreSourceInfo1(string source)
        {
            var result = s4.Replace(source, m => string.Empty);

            return result;
        }

        private static readonly Regex Xref = new Regex(@"<xref href=""[^""<>]*?"" data-raw-source=""(@[^<>]*?)"">\s*<\/xref>", RegexOptions.Compiled);

        static string IgnoreXref(string source)
        {
            return Xref.Replace(source, m => m.Groups[1].Value);
        }

        static string IgnoreSpan(string source)
        {
            //var result = Regex.Replace(source, "<td>\n* *", "<td>");
            //result = Regex.Replace(result, " *\n*</td>", "</td>");
            return source.Replace("<span>", "").Replace("</span>", "");
        }

        static string IgnoreDel(string source)
        {
            //var result = Regex.Replace(source, "<td>\n* *", "<td>");
            //result = Regex.Replace(result, " *\n*</td>", "</td>");
            return source.Replace("<del>", "~~").Replace("</del>", "~~");
        }
        private static readonly Regex ConnectedUL = new Regex(@"</ul>\s*\n*<ul>", RegexOptions.Compiled);
        static string IgnoreConnectedUL(string source)
        {
            //var result = Regex.Replace(source, "<td>\n* *", "<td>");
            //result = Regex.Replace(result, " *\n*</td>", "</td>");
            return ConnectedUL.Replace(source, m => string.Empty);
        }

        private static readonly Regex TD = new Regex(@"<td>([\s\S]*?)</td>", RegexOptions.Compiled);
        static string IgnoreTrimTd(string source)
        {
            //var result = Regex.Replace(source, "<td>\n* *", "<td>");
            //result = Regex.Replace(result, " *\n*</td>", "</td>");
            return TD.Replace(source, m => "<td>" + m.Groups[1].ToString().Trim() + "</td>");
        }

        private static readonly Regex Image = new Regex(@"(<img[^<>]*?alt="").*?(""[^<>]*?/>)", RegexOptions.Compiled);

        static string IgnoreAltInImage(string source)
        {
            return Image.Replace(source, m => m.Groups[1].Value + m.Groups[2].Value);
        }

        private static readonly Regex Li = new Regex(@"<li>[\s\S]*?</li>", RegexOptions.Compiled);

        static string IgnorePinli(string source)
        {
            return Li.Replace(source, m => m.ToString().Replace("<p>", "")
            .Replace("</p>", "").Replace("<p />", ""));
        }

        private static readonly Regex LeftAngle = new Regex(@"\s*\n*\s*<", RegexOptions.Compiled);
        private static readonly Regex RightAngle = new Regex(@">\s*\n*\s*", RegexOptions.Compiled);

        static string IgnoreNewlineAroundTag(string source)
        {
            var result = LeftAngle.Replace(source, m => "\n<");
            result = RightAngle.Replace(result, n => ">\n");
            return result;
        }

        private static readonly Regex StrongEm = new Regex(@"<strong>\s*<em>(.*?)</em>\s*</strong>", RegexOptions.Compiled);

        static string IgnoreStrongEm(string source)
        {
            source = StrongEm.Replace(source, m => "<em><strong>" + m.Groups[1].Value + "</strong></em>");
            return source.Replace("<strong>", "**").Replace("</strong>", "**")
                .Replace("<em>", "*").Replace("</em>", "*");
        }

        private static readonly Regex TryIt = new Regex(@" data-src="".*?""", RegexOptions.Compiled);

        static string IgnoreTryIt(string source)
        {
            return TryIt.Replace(source, m => string.Empty);
        }

        private static readonly Regex AINHeader = new Regex(@"(<h2 id=""[^""]*?"">.*?)<a name=""(.*?)""></a>(\s*</h2>)", RegexOptions.Compiled);

        static string IgnoreAINHeader(string source)
        {
            return AINHeader.Replace(source, m =>
            {
                return m.Groups[1].Value + "&lt;a name=" + m.Groups[2].Value + "&gt;" + m.Groups[3].Value;
            });
        }

        private static readonly Regex EmptyP = new Regex(@"<p>((\s*)|(&gt;))<\/p>", RegexOptions.Compiled);

        static string IgnoreEmptyP(string source)
        {
            return EmptyP.Replace(source, m => string.Empty);
        }

        private static readonly Regex Autolink = new Regex(@"<a href=""[^""<>]*?"">((https?:.*?)|(www.*?))<\/a>", RegexOptions.Compiled);

        static string IgnoreAutolink1(string source)
        {
            return Autolink.Replace(source, m => m.Groups[1].Value);
        }

        static string IgnoreBackSlashInAutolink(string source)
        {
            return Autolink.Replace(source, m => m.ToString().Replace('\\', ' '));
        }

        private static readonly Regex ConnectedBlockQuote = new Regex(@"<\/blockquote>\n*\s*\n*<blockquote>", RegexOptions.Compiled);

        static string IgnoreEmptyConnectedBlockQuote(string source)
        {
            return ConnectedBlockQuote.Replace(source, m => string.Empty);
        }

        private static readonly Regex EmptyBlockQuote = new Regex(@"<blockquote>\n*\s*\n*<\/blockquote>", RegexOptions.Compiled);

        static string IgnoreEmptyBlockquote(string source)
        {
            return EmptyBlockQuote.Replace(source, m => string.Empty);
        }

        private static readonly Regex Channel9Video = new Regex(@"<iframe src=""(https://channel9\.msdn\.com.*?)""", RegexOptions.Compiled);
        private static readonly Regex YoutubeVideo = new Regex(@"(<iframe src=""https://www\.youtube)(\.com.*?)""", RegexOptions.Compiled);

        static string IgnoreNocookie(string source)
        {
            string result = Channel9Video.Replace(source, m =>
            {
                var g1 = m.Groups[1].Value;
                if (!g1.EndsWith("?nocookie=true"))
                {
                    return "<iframe src=\"" + g1 + "?nocookie=true" + "\"";
                }

                return m.ToString();
            });

            result = YoutubeVideo.Replace(result, m =>
            {
                var g2 = m.Groups[2].Value;
                if (!g2.StartsWith("-nocookie"))
                {
                    return m.Groups[1].Value + "-nocookie" + g2 + "\"";
                }

                return m.ToString();
            });
            return result;
        }

        static string IgnoreHeadingIdDash(string source)
        {
            var doc = StringToHtml(source);

            var idList = doc.DocumentNode.SelectNodes($"//*[self::h1 or self::h2 or self::h3 or self::h4 or self::h5 or self::h6]/@id");

            if (idList == null) return HtmlToString(doc);

            foreach (var id in idList)
            {
                if (!string.IsNullOrEmpty(id.Id))
                {
                    id.Id = "id-ignored";
                }
                //id.Value = Regex.Replace(id.Value, "-+", "-");
            }

            return HtmlToString(doc);
        }

        static string IgnorePinliOld(string source)
        {
            try
            {
                source = source.Replace("&#39;", "'")
                    .Replace("&", "&amp;");

                var doc = StringToHtml(source);

                var pList = doc.DocumentNode.SelectNodes($"//li/p");

                if (pList == null) return HtmlToString(doc);

                for (int i = (pList.Count - 1); i >= 0; i--)
                {
                    var p = pList[i];
                    foreach (var child in p.ChildNodes)
                    {
                        p.ParentNode.InsertBefore(child, p);
                    }
                    p.ParentNode.RemoveChild(p);
                }

                return HtmlToString(doc).Replace("&amp;", "&");
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        static string IgnoreTaginP(string source)
        {
            var doc = StringToHtml(source);

            var pList = doc.DocumentNode.SelectNodes($"//p");

            if (pList == null) return HtmlToString(doc);

            for (int i = (pList.Count - 1); i >= 0; i--)
            {
                var p = pList[i];
                if (p.ChildNodes.Count == 1 && p.FirstChild.NodeType == HtmlNodeType.Element)
                {
                    var tagChild = p.FirstChild;
                    p.ParentNode.ReplaceChild(tagChild, p);
                }
            }

            return HtmlToString(doc);
        }

        static string IgnoreAutoLink(string source)
        {
            var doc = StringToHtml(source);

            var autoLinkList = doc.DocumentNode.SelectNodes($"//a[@data-linktype]");

            if (autoLinkList == null) return HtmlToString(doc);

            for (int i = (autoLinkList.Count - 1); i >= 0; i--)
            {
                var link = autoLinkList[i];
                link.Attributes.RemoveAll();
            }

            return HtmlToString(doc);
        }

        static string IgnoreCodeLastLine(string source)
        {
            string pattern = @"\n*<\/code> *\n* *<\/pre>";
            string replace = "\n</code>\n</pre>";

            string result = Regex.Replace(source, pattern, replace);

            return result;
        }

        static string IgnorePre(string source)
        {
            string result = source.Replace(@" data-interactive=""azurecli""", "");

            result = result.Replace(@"class=""lang-azurecli-interactive""", @"class=""lang-azurecli""");
            result = result.Replace("<pre>", string.Empty);
            result = result.Replace("</pre>", string.Empty);

            return result;
        }

        static string UnifySpaceAndNewLine(string source)
        {
            var doc = StringToHtml(source);

            var textNodes = doc.DocumentNode.SelectNodes("//text()[normalize-space(.) != '' and not(ancestor::code)]");

            if (textNodes == null) return HtmlToString(doc);

            foreach (var textNode in textNodes)
            {
                var replaced = ReplaceSpaceAndNewLine(textNode.InnerHtml);
                if (textNode.NodeType == HtmlNodeType.Text && textNode.InnerHtml != replaced)
                {
                    textNode.InnerHtml = replaced;
                }
            }

            //UnifySpaceAndNewLineHelper(doc.DocumentNode);

            return HtmlToString(doc);
        }

        static string UnifySpace(string source)
        {
            return source.Replace("&nbsp;", "&#160;");
        }

        static string FormatCustomTags(string source)
        {
            string[] tags = { "<br />", "<ul>", "</ul>", "<ol>", "</ol>" };
            string result = source;

            foreach (var tag in tags)
            {
                string pattern = string.Format(@"[\r\n]* *{0} *[\r\n]*", tag);
                string replace = string.Format("\n{0}\n", tag);
                result = Regex.Replace(result, pattern, replace);
            }

            string imgTag = @"\n? *(<img [^>]*\/>) *\n?";
            result = Regex.Replace(result, imgTag, m => $"\n{m.Groups[1].Value}\n");

            return result;
        }

        static string FormatTableStyle(string source)
        {
            string[] styles = { "center", "left", "right" };
            string result = source;

            foreach (var style in styles)
            {
                string patternTd = $"<td style=\"text-align:{style}\">";
                string replaceTd = $"<td style=\"text-align: {style};\">";
                result = result.Replace(patternTd, replaceTd);

                string patternTh = $"<th style=\"text-align:{style}\">";
                string replaceTh = $"<th style=\"text-align: {style};\">";
                result = result.Replace(patternTh, replaceTh);
            }

            return result;
        }

        static string RemoveDashInComment(string source)
        {
            var result = Regex.Replace(source, "<!--([^-]*--[^-]*)-->", m => $"<!--{m.Groups[1].Value.Replace("--", "==")}-->");

            return result;
        }

        static string RemoveContentClass(string source)
        {
            string pattern = "^<div><div class=\"content\">";
            string result = source;
            if (Regex.Match(source, pattern).Success)
            {
                result = Regex.Replace(result, pattern, "<div>");
                result = Regex.Replace(result, @"<\/div><\/div>$", "</div>");
            }

            return result;
        }

        // OP method
        private static string TransformToXhtml(string htmlContent)
        {
            string sourceRelativePath = "";
            string ampersand = "&";
            string encodedAmpersand = "&amp;";
            var xml = new XmlDocument();
            try
            {
                xml.LoadXml(htmlContent);
                return htmlContent;
            }
            catch (XmlException)
            {
                HtmlDocument htmlDoc = new HtmlDocument { OptionOutputAsXml = true };

                // Workaround for HtmlAgilityPack bug http://htmlagilitypack.codeplex.com/workitem/12359
                // Code causing the issue @ http://htmlagilitypack.codeplex.com/SourceControl/latest#Branches/1.4.0/HtmlAgilityPack/HtmlDocument.cs, L301
                // As http://www.w3.org/TR/2000/REC-xml-20001006#syntax describes, The ampersand character (&) and the left angle bracket (<) may appear in their literal form only when used as markup delimiters, or within a comment, a processing instruction, or a CDATA section.
                // FIRST: Encode & to &amp; before LoadHtml begins to workaround HtmlAgilityPack's HtmlEncode method
                htmlContent = htmlContent.Replace(ampersand, encodedAmpersand);
                htmlDoc.LoadHtml(htmlContent);
                XDocument doc = new XDocument();
                try
                {
                    doc = XDocument.Parse(htmlDoc.DocumentNode.OuterHtml);
                }
                catch (XmlException xe)
                {
                    StringReader sr = new StringReader(htmlContent);
                    string line;
                    int currentLineNumber = 0;

                    do
                    {
                        ++currentLineNumber;
                        line = sr.ReadLine();
                    }
                    while (line != null && currentLineNumber < xe.LineNumber);

                    string errorMessage = currentLineNumber == xe.LineNumber ? $"Invalid content: {line}. Line: {xe.LineNumber}, position: {xe.LinePosition}, file: {sourceRelativePath}" : xe.Message;
                    throw new XmlException(errorMessage, xe, xe.LineNumber, xe.LinePosition);
                }

                var nodes = doc.DescendantNodes();
                foreach (var node in nodes)
                {
                    switch (node.NodeType)
                    {
                        case XmlNodeType.Element:
                            var elementNode = (XElement)node;

                            // attr.Value will get decoded value
                            foreach (var attr in elementNode.Attributes())
                            {
                                attr.SetValue(attr.Value);
                            }
                            break;
                        case XmlNodeType.Text:
                            var textNode = (XText)node;
                            textNode.Value = textNode.Value;

                            // If value contains \u0000-\u001F (except \u0009, \u000A, \u000D), doc.ToString() will break. so replace them with empty string to workaround.
                            textNode.Value = Regex.Replace(textNode.Value, "(?![\u0009|\u000A|\u000D])[\u0000-\u001F]", string.Empty);
                            break;
                        case XmlNodeType.CDATA:
                            var cdataNode = (XText)node;
                            cdataNode.Value = cdataNode.Value.Replace(encodedAmpersand, ampersand);
                            break;
                        case XmlNodeType.Comment:
                            var commentNode = (XComment)node;
                            commentNode.Value = commentNode.Value.Replace(encodedAmpersand, ampersand);
                            break;
                        case XmlNodeType.ProcessingInstruction:
                            var proceesingInstructionNode = (XProcessingInstruction)node;
                            proceesingInstructionNode.Data = proceesingInstructionNode.Data.Replace(encodedAmpersand, ampersand);
                            break;
                        default:
                            break;
                    }
                }

                return doc.ToString();
            }
        }
    }
    #endregion
}
