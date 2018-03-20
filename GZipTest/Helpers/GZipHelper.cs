using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.IO.Compression;

using System.Threading.Tasks;

using GZipTest.Helpers.Accessors;
using GZipTest.Helpers.Multithreading;

namespace GZipTest.Helpers
{
    class GZipHelper
    {
        private const int DEFAULT_CHUNK_SIZE = 1024 * 1024;
        private static readonly int CORS_COUNT = Environment.ProcessorCount;

        private static void compressChunk(FileChunk chunk, ThreadWriter threadWriter)
        {
            using (var dest = new MemoryStream())
            {
                using (var gzipOut = new GZipStream(dest, CompressionMode.Compress))
                {
                    gzipOut.Write(chunk.Data, 0, chunk.Data.Length);
                }

                threadWriter.Add(
                    new FileChunk(
                        dest.ToArray(),
                        chunk.Number
                    )
                );
            }
        }

        private static void readFile(string name, MyTaskManager manager, ThreadWriter threadWriter)
        {
            var chr = new ChunkedFileReader(new FileInfo(name), DEFAULT_CHUNK_SIZE);

            for (var chunk = chr.SyncNextChunk(); chunk != null; chunk = chr.SyncNextChunk())
            {
                var local = chunk;

                manager.Add(new Task(
                    () =>
                    {
                        compressChunk(local, threadWriter);
                    }
                ));
            }

        }

        /**/

        public static void ChunkedCompress(string from, string to, int chunkSize = DEFAULT_CHUNK_SIZE)
        {
            var source = new FileInfo(from);
            if (!source.Exists)
            {
                throw new ArgumentException("Файл, подлежащий архивации, не обнаружен!");
            }
            if (chunkSize <= 0)
            {
                throw new ArgumentException("Размер сегмента архива должен быть положительным числом!");
            }

            MyTaskManager manager = new MyTaskManager();

            ThreadWriter writer = new ThreadWriter(to, () =>
            {
                return !manager.IsCompleted;
            });

            manager.Add(new Task(
                () =>
                {
                    readFile(from, manager, writer);
                }
            ));

            manager.Execute();

            writer.Join();
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
                var threads = new MyThreadPool(ChunkedDecompressHandler, CORS_COUNT);
                var ptrs = new Tuple<Queue<FileInfo>, ChunkedFileWriter>(chunks, chWriter);

                threads.Execute(ptrs);

                if (chWriter.IsReady == false)
                {
                    throw new InvalidDataException("Не все найденные сегменты были обработаны!");
                }
            }
        }

        private static void ChunkedDecompressHandler(object obj)
        {
            var ptrs = (Tuple<Queue<FileInfo>, ChunkedFileWriter>)obj;

            var chunks = ptrs.Item1;
            var chWriter = ptrs.Item2;

            int chunkSize = chWriter.СhunkSize;

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
                using (var br = new BinaryReader( new GZipStream(chunk.Open(FileMode.Open, FileAccess.Read), CompressionMode.Decompress)))
                {
                    chData = br.ReadBytes(DEFAULT_CHUNK_SIZE);
                }

                int chNumber = int.Parse(chunk.Extension.Remove(0, 1));
                chWriter.SynkFeed(new FileChunk(chData, chNumber));
            }
        }

        private static void ChunkedCompressHandler(object obj)
        {
            var ptrs = (Tuple<ChunkedFileReader, string>)obj;

            var chReader = ptrs.Item1;
            var chunkNamePattern = ptrs.Item2;

            FileChunk chunk;
            chunk = chReader.SyncNextChunk();
            while (chunk != null)
            {
                var destination = new FileInfo(chunkNamePattern + chunk.Number);
                if (destination.Exists)
                {
                    throw new ArgumentException("Имя файла, необходимое сегменту архива, уже занято!");
                }
                using (var gzipOut = new GZipStream(destination.Open(FileMode.Create, FileAccess.Write), CompressionMode.Compress))
                {
                    gzipOut.Write(chunk.Data, 0, chunk.Data.Length);
                }

                chunk = chReader.SyncNextChunk();
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
