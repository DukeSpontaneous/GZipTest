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
        public int СhunkSize
        {
            get => chunkSize;
        }

        private readonly int chunksNumber;

        private readonly FileStream fileStream;
        private readonly HashSet<int> chunksGeted;

        public bool IsReady { get => chunksGeted.Count == chunksNumber; }

        public ChunkedFileWriter(FileInfo fi, int chunkSize, int chunksNumber)
        {
            this.fileStream = fi.Open(FileMode.Create, FileAccess.Write);
            this.chunksGeted = new HashSet<int>();

            this.chunkSize = (chunkSize > 0) ?
                chunkSize : throw new ArgumentException("Размер сегмента архива должен быть целым положительным числом.");
            this.chunksNumber = (chunksNumber > 0) ?
                chunksNumber : throw new ArgumentException("Число сегментов архива должно быть целым положительным числом.");
        }

        public void Dispose()
        {
            this.fileStream.Close();
        }

        public void SynkFeed(FileChunk chunk)
        {
            if (chunksGeted.Add(chunk.Number) == false)
            {
                throw new ArgumentException(string.Format("Обнаружена копия блока №{0}!", chunk.Number));
            }
            if (chunk.Data.Length > chunkSize)
            {
                throw new ArgumentException(string.Format("Размер блока №{0} больше ожидаемого!", chunk.Number));
            }

            long position = (long)chunk.Number * chunkSize;

            lock (fileStream)
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                fileStream.Write(chunk.Data, 0, chunk.Data.Length);
            }
        }

    }
}
