using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.IO.Compression;
using System.Threading;

using GZipTest.Helpers.Accessors;

namespace GZipTest.Helpers
{
    class GZipHelper
    {
        private const int DEFAULT_CHUNK_SIZE = 1024 * 1024 * 100;
        private static readonly int CORS_COUNT = Environment.ProcessorCount;

        public static void ChunkedCompress(string form, string to, int chunkSize = DEFAULT_CHUNK_SIZE)
        {
            FileInfo source = new FileInfo(form);
            if (!source.Exists)
            {
                throw new ArgumentException("Файл, подлежащий архивации, не обнаружен!");
            }
            if (chunkSize <= 0)
            {
                throw new ArgumentException("Размер сегмента архива должен быть положительным числом!");
            }

            string chunkNamePattern = to + ".";

            using (var chReader = new ChunkedFileReader(source, chunkSize))
            {
                Thread[] threads = new Thread[CORS_COUNT];
                for (int i = 0; i < threads.Length; ++i)
                {
                    threads[i] = new Thread(
                        () =>
                        {
                            FileChunk chunk;
                            while (true)
                            {
                                lock (chReader)
                                {
                                    chunk = chReader.NextChunk();
                                }
                                if (chunk == null)
                                {
                                    break;
                                }

                                var destination = new FileInfo(chunkNamePattern + chunk.Number);
                                if (destination.Exists)
                                {
                                    throw new ArgumentException("Имя файла, необходимое сегменту архива, уже занято!");
                                }
                                using (var gzipOut = new GZipStream(destination.Open(FileMode.Create, FileAccess.Write), CompressionMode.Compress))
                                {
                                    gzipOut.Write(chunk.Data, 0, chunk.Data.Length);
                                }
                            }
                        });
                    threads[i].Start();
                }

                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }
        }

        public static void ChunkedDecompress(string form, string to, int chunkSize = DEFAULT_CHUNK_SIZE)
        {
            var chunks = new Queue<FileInfo>(GetAllChunkFiles(form));
            var destination = new FileInfo(to);

            if (chunkSize <= 0)
            {
                throw new ArgumentException("Размер сегмента архива должен быть положительным числом!");
            }
            if (destination.Exists)
            {
                throw new ArgumentException("Имя файла, необходимое для разархивации, уже занято!");
            }

            using (var chWriter = new ChunkedFileWriter(destination, chunkSize, chunks.Count))
            {
                Thread[] threads = new Thread[CORS_COUNT];
                for (int i = 0; i < threads.Length; ++i)
                {
                    threads[i] = new Thread(
                        () =>
                        {
                            while (true)
                            {
                                FileInfo chunk;
                                lock (chunks)
                                {
                                    if (chunks.Count <= 0)
                                    {
                                        break;
                                    }
                                    chunk = chunks.Dequeue();
                                }

                                byte[] chData;
                                using (var br = new BinaryReader(new GZipStream(chunk.Open(FileMode.Open, FileAccess.Read), CompressionMode.Decompress)))
                                {
                                    chData = br.ReadBytes(chunkSize);
                                }

                                int chNumber = int.Parse(chunk.Extension.Remove(0, 1));
                                lock (chWriter)
                                {
                                    chWriter.Feed(new FileChunk(chData, chNumber));
                                }
                            }

                        });
                    threads[i].Start();
                }

                foreach (var thread in threads)
                {
                    thread.Join();
                }

                if (chWriter.IsReady == false)
                {
                    throw new InvalidDataException("Не все найденные сегменты были обработаны!");
                }
            }
        }

        private static IEnumerable<FileInfo> GetAllChunkFiles(string name)
        {
            FileInfo chunk;
            switch (Path.GetExtension(name))
            {
                case ".gz":
                    chunk = new FileInfo(name);
                    if (chunk.Exists)
                    {
                        return new FileInfo[] { new FileInfo(name) };
                    }
                    else
                    {
                        throw new ArgumentException(String.Format("Не удалось обнаружить следующий файл: {0}", name));
                    }
                case ".0":
                    chunk = new FileInfo(name);
                    break;
                default:
                    throw new ArgumentException(String.Format("Имя следующего файла некорректно, или не является началом цепи сегментов, созданной этой утилитой: {0}", name));
            }

            string searchPattern = String.Format("{0}.*", Path.GetFileNameWithoutExtension(chunk.Name));
            var files = chunk.Directory.GetFiles(searchPattern);

            var result = new List<FileInfo>();
            for (int i = 0; i < int.MaxValue; ++i)
            {
                string extPattern = String.Format(".{0}", i);
                var file = files.FirstOrDefault(f => f.Extension.Equals(extPattern));
                if (file != null)
                {
                    result.Add(file);
                }
                else
                {
                    break;
                }
            }

            return result;
        }
    }
}
