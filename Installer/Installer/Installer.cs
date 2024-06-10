using Eon_Installer.Installer;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eon_Installer.Installer
{
    internal class Installer
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task Download(ManifestFile manifest, string version, string resultPath)
        {
            long totalBytes = manifest.Size;
            long completedBytes = 0;
            int progressLength = 0;

            if (!Directory.Exists(resultPath))
                Directory.CreateDirectory(resultPath);

            using SemaphoreSlim semaphore = new SemaphoreSlim(12);

            var downloadTasks = manifest.Chunks.Select(async chunkedFile =>
            {
                await semaphore.WaitAsync();

                try
                {
                    string outputFilePath = Path.Combine(resultPath, chunkedFile.File);
                    var fileInfo = new FileInfo(outputFilePath);

                    if (File.Exists(outputFilePath) && fileInfo.Length == chunkedFile.FileSize)
                    {
                        Interlocked.Add(ref completedBytes, chunkedFile.FileSize);
                        return;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                    using FileStream outputStream = File.Open(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

                    foreach (int chunkId in chunkedFile.ChunksIds)
                    {
                        bool success = false;
                        while (!success)
                        {
                            try
                            {
                                string chunkUrl = $"{Globals.SeasonBuildVersion}/{version}/{chunkId}.chunk";
                                var chunkData = await httpClient.GetByteArrayAsync(chunkUrl);

                                using MemoryStream memoryStream = new MemoryStream(chunkData);
                                using GZipStream decompressionStream = new GZipStream(memoryStream, CompressionMode.Decompress);

                                byte[] buffer = new byte[Globals.CHUNK_SIZE];
                                int bytesRead;
                                while ((bytesRead = await decompressionStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await outputStream.WriteAsync(buffer, 0, bytesRead);
                                    Interlocked.Add(ref completedBytes, bytesRead);

                                    double progress = (double)completedBytes / totalBytes * 100;
                                    string progressMessage = $"\rDownloaded: {ConvertStorageSize.FormatBytesWithSuffix(completedBytes)} / {ConvertStorageSize.FormatBytesWithSuffix(totalBytes)} ({progress:F2}%)";
                                    Console.Write(progressMessage.PadRight(progressLength));
                                    progressLength = progressMessage.Length;
                                }

                                success = true;
                            }
                            catch (Exception)
                            {
                                // Log exception or implement retry policy
                                // Wait a bit before retrying to avoid spamming the server
                                await Task.Delay(500);
                            }
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(downloadTasks);

            Console.WriteLine("\n\nFinished Downloading.\nPress any key to exit!");
            Console.ReadKey();
        }
    }
}
