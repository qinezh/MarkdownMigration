﻿using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MarkdownMigration.ExtractHtml
{
    public class ExtractHtml
    {
        public static void ExtractHtmlFromJson(IEnumerable<string> jsonfolders, string docsetFolder, bool diffBuildPackage)
        {
            if (jsonfolders == null) return;
            var htmlToSourceFileMapping = new ConcurrentDictionary<string, string>();

            foreach (var jsongFolder in jsonfolders)
            {
                string targetFolder = jsongFolder + "-html";
                string ext = diffBuildPackage ? ".raw.page.json" : ".raw.json";

                var rawPages = Directory.GetFiles(jsongFolder, "*" + ext, SearchOption.AllDirectories);
                DeleteFolder(targetFolder);
                Directory.CreateDirectory(targetFolder);

                Parallel.ForEach(rawPages, rawPage =>
                {
                    var json = File.ReadAllText(rawPage);
                    dynamic data = JsonConvert.DeserializeObject(json);
                    string html = diffBuildPackage ? data?.content : data?.conceptual;
                    string sourcePath = diffBuildPackage ? 
                        data?.rawMetadata.original_ref_skeleton_git_url ?? data?.rawMetadata.content_git_url :
                        data?._key;

                    if (html != null)
                    {
                        var htmlPage = rawPage.Replace(jsongFolder, targetFolder).Replace(ext, ".html");

                        if (sourcePath != null)
                        {
                            htmlToSourceFileMapping[htmlPage] = diffBuildPackage ? sourcePath : Path.Combine(docsetFolder, sourcePath);
                        }

                        if (!Directory.Exists(Path.GetDirectoryName(htmlPage)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(htmlPage));
                        }
                        File.WriteAllText(htmlPage, html);
                    }
                });

                File.WriteAllText(Path.Combine(targetFolder, "htmlSourceMapping.json"), JsonConvert.SerializeObject(htmlToSourceFileMapping));
            }
        }

        static void DeleteFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
