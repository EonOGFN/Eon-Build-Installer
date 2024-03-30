using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Eon_Installer.Installer
{
    internal class Installer
    {
        public static async Task Download(FileManifest.ManifestFile manifest, string version, string resultPath)
        {
            long totalBytes = manifest.Size;
            long completedBytes = 0;
            int progressLength = 0;

            if (!Directory.Exists(resultPath))
                Directory.CreateDirectory(resultPath);

            SemaphoreSlim semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);
            WebClient httpClient = new WebClient();

            foreach (var chunkedFile in manifest.Chunks)
            {
                await semaphore.WaitAsync();

                try
                {
                    string outputFilePath = Path.Combine(resultPath, chunkedFile.File);
                    var fileInfo = new FileInfo(outputFilePath);

                    if (File.Exists(outputFilePath) && fileInfo.Length == chunkedFile.FileSize)
                    {
                        completedBytes += chunkedFile.FileSize;
                        semaphore.Release();
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                    using (FileStream outputStream = File.OpenWrite(outputFilePath))
                    {
                        foreach (int chunkId in chunkedFile.ChunksIds)
                        {
                        retry:
                            try
                            {
                                string chunkUrl = Globals.SeasonBuildVersion + $"/{version}/" + chunkId + ".chunk";
                                var chunkData = await httpClient.DownloadDataTaskAsync(chunkUrl);

                                using (MemoryStream memoryStream = new MemoryStream(chunkData))
                                using (GZipStream decompressionStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                                {
                                    byte[] chunkDecompData = new byte[Globals.CHUNK_SIZE];
                                    int bytesRead;

                                    while ((bytesRead = await decompressionStream.ReadAsync(chunkDecompData, 0, chunkDecompData.Length)) > 0)
                                    {
                                        await outputStream.WriteAsync(chunkDecompData, 0, bytesRead);
                                        Interlocked.Add(ref completedBytes, bytesRead);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                goto retry;
                            }
                        }
                    }

                    double progress = (double)completedBytes / totalBytes * 100;
                    string progressMessage = $"\rDownload Status: {ConvertStorageSize.FormatBytesWithSuffix(completedBytes)} / {ConvertStorageSize.FormatBytesWithSuffix(totalBytes)} ({progress:F2}%)";
                    int padding = progressLength - progressMessage.Length;

                    if (padding > 0)
                        progressMessage += new string(' ', padding);

                    Console.Write(progressMessage);
                    progressLength = progressMessage.Length;
                }
                finally
                {
                    semaphore.Release();
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\rDownload Progress: Completed!");
            Thread.Sleep(100);
            Console.ReadKey();
        }

    }
}
