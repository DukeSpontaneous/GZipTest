using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace GZipTest.Helpers.Accessors
{
    class ChunkedFileReader : IDisposable
    {
        readonly int chunkSize;
        public int СhunkSize
        {
            get => chunkSize;
        }

        readonly BinaryReader sourceFile;
        int currentChunk;

        public ChunkedFileReader(FileInfo fi, int chunkSize)
        {
            sourceFile = new BinaryReader(fi.Open(FileMode.Open, FileAccess.Read));

            this.chunkSize = (chunkSize > 0) ?
                chunkSize : throw new ArgumentException("Размер сегмента архива должен быть целым положительным числом.");
            this.currentChunk = 0;
        }

        public void Dispose()
        {
            sourceFile.Close();
        }

        public FileChunk SyncNextChunk()
        {
            byte[] chunk;
            lock (sourceFile)
            {
                chunk = sourceFile.ReadBytes(СhunkSize);
            }
            return chunk.Length > 0 ? new FileChunk(chunk, this.currentChunk++) : null;
        }

    }
}
