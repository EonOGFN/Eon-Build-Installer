using Newtonsoft.Json;
using System.IO.Compression;
using System.Net;

namespace Eon_Installer.Installer
{
    class Program
    {
        static void Main(string[] args)
        {
            var httpClient = new WebClient();

            List<string> versions = JsonConvert.DeserializeObject<List<string>>(httpClient.DownloadString(Globals.SeasonBuildVersion + "/versions.json"));
            Console.Clear();

            Console.Title = "Eon 11.31 Build Installer";
            Console.Write("Are you sure you want to install Fortnite Version ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("11.31");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("?\nPlease type ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("'Yes' ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("or ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("'No'\n");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(">> ");

            var Version = Console.ReadLine();
            var FN11 = 0;

            switch (Version.ToLower())
            {
                case "yes":
                    FN11 = 10;
                    break;
                case "no":
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Closing in 5 seconds...");
                    Console.Out.Flush();
                    Thread.Sleep(5000);
                    return;
                default:
                    Main(args);
                    return;
            }

            var targetVersion = versions[FN11].Split("-")[1];
            var manifestUrl = $"{Globals.SeasonBuildVersion}/{targetVersion}/{targetVersion}.manifest";
            var manifest = JsonConvert.DeserializeObject<FileManifest.ManifestFile>(httpClient.DownloadString(manifestUrl));

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Enter your desired download path for Fortnite Version 11.31: ");
            Console.ForegroundColor = ConsoleColor.White;
            var targetPath = Console.ReadLine();
            Console.WriteLine();

            Installer.Download(manifest, targetVersion, targetPath).GetAwaiter().GetResult();
        }
    }
}