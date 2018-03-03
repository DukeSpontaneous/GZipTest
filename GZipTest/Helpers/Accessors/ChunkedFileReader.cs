using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace GZipTest.Helpers.Accessors
{
    class ChunkedFileReader : IDisposable
    {
        private readonly int chunkSize;
        private int currentChunk;

        private readonly BinaryReader sourceFile;

        public ChunkedFileReader(FileInfo fi, int chunkSize)
        {
            sourceFile = new BinaryReader(fi.Open(FileMode.Open, FileAccess.Read));

            this.chunkSize = chunkSize;
            this.currentChunk = 0;
        }

        public void Dispose()
        {
            sourceFile.Close();
        }

        public FileChunk NextChunk()
        {
            byte[] chunk = sourceFile.ReadBytes(chunkSize);
            return chunk.Length > 0 ? new FileChunk(chunk, this.currentChunk++) : null;
        }
       
    }
}
