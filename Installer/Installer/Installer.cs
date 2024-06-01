using Eon_Installer.Installer;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Eon_Installer.Installer.FileManifest;

namespace Eon_Installer.Installer
{
    internal class Installer
    {
        public static async Task Download(ManifestFile manifest, string version, string resultPath)
        {
            long totalBytes = manifest.Size;
            long completedBytes = 0;
            int progressLength = 0;

            if (!Directory.Exists(resultPath))
                Directory.CreateDirectory(resultPath);

            SemaphoreSlim semaphore = new SemaphoreSlim(12);

            await Task.WhenAll(manifest.Chunks.Select(async chunkedFile =>
            {
                await semaphore.WaitAsync();

                try
                {
                    WebClient httpClient = new WebClient();

                    string outputFilePath = Path.Combine(resultPath, chunkedFile.File);
                    var fileInfo = new FileInfo(outputFilePath);

                    if (File.Exists(outputFilePath) && fileInfo.Length == chunkedFile.FileSize)
                    {
                        completedBytes += chunkedFile.FileSize;
                        semaphore.Release();
                        return;
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

                                byte[] chunkDecompData = new byte[Globals.CHUNK_SIZE + 1];
                                int bytesRead;
                                long chunkCompletedBytes = 0;

                                MemoryStream memoryStream = new MemoryStream(chunkData);
                                GZipStream decompressionStream = new GZipStream(memoryStream, CompressionMode.Decompress);

                                while ((bytesRead = await decompressionStream.ReadAsync(chunkDecompData, 0, chunkDecompData.Length)) > 0)
                                {
                                    await outputStream.WriteAsync(chunkDecompData, 0, bytesRead);
                                    Interlocked.Add(ref completedBytes, bytesRead);
                                    Interlocked.Add(ref chunkCompletedBytes, bytesRead);

                                    double progress = (double)completedBytes / totalBytes * 100;
                                    string progressMessage = $"\rDownloaded: {ConvertStorageSize.FormatBytesWithSuffix(completedBytes)} / {ConvertStorageSize.FormatBytesWithSuffix(totalBytes)} ({progress:F2}%)";

                                    int padding = progressLength - progressMessage.Length;
                                    if (padding > 0)
                                        progressMessage += new string(' ', padding);

                                    Console.Write(progressMessage);
                                    progressLength = progressMessage.Length;
                                }

                                memoryStream.Close();
                                decompressionStream.Close();
                            }
                            catch (Exception ex)
                            {
                                goto retry;
                            }
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));

            Console.WriteLine("\n\nFinished Downloading.\nPress any key to exit!");
            Thread.Sleep(100);
            Console.ReadKey();
        }
    }
}