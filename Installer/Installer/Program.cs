using Newtonsoft.Json;
using System.IO.Compression;
using System.Net;
using static Eon_Installer.Installer.FileManifest;

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
            Console.Write("Are you sure you want to install Fortnite Version 11.31");

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
            var manifest = JsonConvert.DeserializeObject<ManifestFile>(httpClient.DownloadString(Globals.SeasonBuildVersion + $"/{targetVersion}/{targetVersion}.manifest"));

            Console.Write("Enter your desired download path for Fortnite Version 11.31: ");
            var targetPath = Console.ReadLine();
            Console.Write("\n");

            Installer.Download(manifest, targetVersion, targetPath).GetAwaiter().GetResult();
        }
    }
}