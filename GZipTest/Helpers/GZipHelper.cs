using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using GZipTest.Helpers.Accessors;

namespace GZipTest.Helpers
{
    class GZipHelper
    {
        const int DEFAULT_CHUNK_SIZE = 1024 * 1024;

        public static void ChunkedCompress(string src, string dst, int chunkSize = DEFAULT_CHUNK_SIZE)
        {
            var source = new FileInfo(src);
            if (!source.Exists)
            {
                throw new ArgumentException("Файл, подлежащий архивации, не обнаружен!");
            }
            if (chunkSize <= 0)
            {
                throw new ArgumentException("Размер сегмента архива должен быть положительным числом!");
            }

            using (var chReader = new ChunkedFileReader(source, chunkSize))
            {
                var conveyor = new GZipCompressConveyor(chReader, dst, chunkSize);
                conveyor.Execute();
            }
        }

        public static void ChunkedDecompress(string src, string dst, int chunkSize = DEFAULT_CHUNK_SIZE)
        {
            var sources = GetAllChunkFiles(src);
            var destination = new FileInfo(dst);

            if (chunkSize <= 0)
            {
                throw new ArgumentException("Размер сегмента архива должен быть положительным числом!");
            }
            if (destination.Exists)
            {
                throw new ArgumentException("Имя файла, необходимое для разархивации, уже занято!");
            }

            using (var chWriter = new ChunkedFileWriter(destination, chunkSize, sources.Count))
            {
                var conveyor = new GZipDecompressConveyor(sources, chWriter, chunkSize);
                conveyor.Execute();
            }
        }

        static Queue<FileInfo> GetAllChunkFiles(string name)
        {
            FileInfo chunk = new FileInfo(name);
            switch (chunk.Extension)
            {
                case ".gz":
                    if (chunk.Exists)
                    {
                        var q = new Queue<FileInfo>();
                        q.Enqueue(chunk);
                        return q;
                    }
                    else
                    {
                        throw new ArgumentException(String.Format("Не удалось обнаружить следующий файл: {0}", name));
                    }
                case ".0":
                    break;
                default:
                    throw new ArgumentException(String.Format("Имя следующего файла некорректно, или не является началом цепи сегментов, созданной этой утилитой: {0}", name));
            }

            string searchPattern = String.Format("{0}.*", Path.GetFileNameWithoutExtension(chunk.Name));
            var files = chunk.Directory.GetFiles(searchPattern);

            var result = new Queue<FileInfo>();
            for (int i = 0; i < int.MaxValue; ++i)
            {
                string extPattern = String.Format(".{0}", i);
                var file = files.FirstOrDefault(f => f.Extension.Equals(extPattern));
                if (file != null)
                {
                    result.Enqueue(file);
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
