using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractHtml
{
    class Program
    {
        private static string BasePath = string.Empty;

        static void Main(string[] args)
        {
            var jsonfolders = args[0].Split(',')?.ToList();

            if (args.Length > 1)
            {
                BasePath = args[1];
            }

            if (jsonfolders == null)
            {
                Console.WriteLine($"AppSetting 'jsonfolders' not found. Trying 'zipfiles'");
                var zipfiles = ConfigurationManager.AppSettings["zipfiles"]?.Split(',')?.ToList();

                if (zipfiles == null)
                {
                    Console.WriteLine($"AppSetting 'zipfiles' not found. Trying 'directory'");
                    var directory = ConfigurationManager.AppSettings["directory"];

                    if (directory == null)
                    {
                        Console.WriteLine($"AppSetting 'directory' not found. Searching zip file in {Environment.CurrentDirectory}");
                        directory = Environment.CurrentDirectory;
                    }

                    zipfiles = new DirectoryInfo(directory)
                        .EnumerateFileSystemInfos("*.zip", SearchOption.TopDirectoryOnly)
                        .Select(info => info.FullName)
                        .ToList();
                }

                Confirm(zipfiles);
                jsonfolders = new List<string>();
                foreach (var zipFile in zipfiles)
                {
                    Console.WriteLine($"Zip extracting {zipFile}...");
                    var zipFolder = zipFile.Replace(".zip", "");
                    DeleteFolder(zipFolder);
                    ZipFile.ExtractToDirectory(zipFile, zipFolder);
                    jsonfolders.Add(zipFolder);
                }
            }

            //Confirm(jsonfolders);
            ExtractHtmlFromJson(jsonfolders);
        }

        static void Confirm(IEnumerable<string> paths)
        {
            if (paths == null || paths.Count() == 0)
            {
                Console.WriteLine($"No Files Found.");
                System.Environment.Exit(0);
            }

            Console.WriteLine($"Extract html in {string.Join(", ", paths)}? (Y/N)");
            var key = Console.ReadKey().KeyChar;
            Console.WriteLine();
            if (key != 'y' && key != 'Y')
            {
                System.Environment.Exit(0);
            }
        }

        static void DeleteFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        static void ExtractHtmlFromJson(IEnumerable<string> jsonfolders)
        {
            var htmlToSourceFileMapping = new ConcurrentDictionary<string, string>();

            foreach (var jsongFolder in jsonfolders)
            {
                string targetFolder = jsongFolder + "-html";
                Console.WriteLine($"Extracting {jsongFolder} to {targetFolder}");

                var rawPages = Directory.GetFiles(jsongFolder, "*.raw.json", SearchOption.AllDirectories);
                var progressHelper = ProgressHelper.CreateStartedInstance(rawPages.Count());
                DeleteFolder(targetFolder);
                Directory.CreateDirectory(targetFolder);

                Parallel.ForEach(rawPages, rawPage =>
                {
                    var json = File.ReadAllText(rawPage);
                    dynamic data = JsonConvert.DeserializeObject(json);
                    string html = data?.conceptual;
                    string sourcePath = data?._key;

                    if (html != null)
                    {
                        var htmlPage = rawPage.Replace(jsongFolder, targetFolder).Replace(".raw.json", ".html");

                        if (sourcePath != null)
                        {
                            htmlToSourceFileMapping[htmlPage] = Path.Combine(BasePath, sourcePath);
                        }

                        if (!Directory.Exists(Path.GetDirectoryName(htmlPage)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(htmlPage));
                        }
                        File.WriteAllText(htmlPage, html);
                        progressHelper.Increase();
                    }
                });

                File.WriteAllText(Path.Combine(targetFolder, "htmlSourceMapping.json"), JsonConvert.SerializeObject(htmlToSourceFileMapping));
            }
        }
    }
}
