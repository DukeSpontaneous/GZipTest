using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace GZipTest.Helpers.Accessors
{
    class ChunkedFileWriter : IDisposable
    {
        private readonly int chunkSize;
        private readonly int chunksNumber;        

        private readonly FileStream fileStream;
        private readonly HashSet<int> chunksGeted;

        public bool IsReady { get => chunksGeted.Count == chunksNumber; }

        public ChunkedFileWriter(FileInfo fi, int chunkSize, int chunksNumber)
        {
            this.fileStream = fi.Open(FileMode.Create, FileAccess.Write);
            this.chunksGeted = new HashSet<int>();

            this.chunkSize = chunkSize;
            this.chunksNumber = chunksNumber;
        }

        public void Dispose()
        {
            this.fileStream.Close();
        }

        public void Feed(FileChunk chunk)
        {
            if (chunksGeted.Add(chunk.Number) == false)
            {
                throw new ArgumentException(string.Format("Обнаружена копия блока №{0}!", chunk.Number));
            }
            if (chunk.Data.Length != chunkSize)
            {
                throw new ArgumentException(string.Format("Размер блока №{0} отличен от ожидаемого!", chunk.Number));
            }

            Console.WriteLine(chunk.Number);

            long position = chunk.Number * chunkSize;
            fileStream.Seek(position, SeekOrigin.Begin);
            fileStream.Write(chunk.Data, 0, chunk.Data.Length);
        }

    }
}
