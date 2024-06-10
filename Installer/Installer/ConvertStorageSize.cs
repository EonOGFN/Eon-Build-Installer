namespace Eon_Installer.Installer
{
    internal static class ConvertStorageSize
    {
        public static string FormatBytesWithSuffix(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1}{suffixes[counter]}";
        }
    }
}
