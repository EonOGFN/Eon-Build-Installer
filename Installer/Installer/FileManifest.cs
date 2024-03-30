using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eon_Installer.Installer
{
    internal class FileManifest
    {
        public class ChunkedFile
        {
            public List<int> ChunksIds = new();
            public string File = string.Empty;
            public long FileSize = 0;
        }

        public class ManifestFile
        {
            public string Name = string.Empty;
            public List<ChunkedFile> Chunks = new();
            public long Size = 0;
        }
    }
}
