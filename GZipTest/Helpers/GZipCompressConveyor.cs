using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.IO.Compression;

using System.Threading;

using GZipTest.Helpers.Accessors;
using GZipTest.Helpers.Multithreading;

namespace GZipTest.Helpers
{
    class GZipCompressConveyor
    {
        readonly ChunkedFileReader source;
        readonly ProducerConsumer<FileChunk> compressQueue;
        readonly ProducerConsumer<FileChunk> writeQueue;

        readonly string chunkNamePattern;
        readonly int chunkSize;

        public GZipCompressConveyor(ChunkedFileReader source, string outName, int chunkSize)
        {
            this.source = source != null ?
                source : throw new ArgumentNullException();

            compressQueue = new ProducerConsumer<FileChunk>();
            writeQueue = new ProducerConsumer<FileChunk>();

            this.chunkNamePattern = outName + ".";
            this.chunkSize = chunkSize > 0 ?
                chunkSize : throw new ArgumentException("Размер сегмента должен быть положительным числом.");
        }

        public void Execute()
        {
            var reader = new Thread(ReadHandler);
            var writer = new Thread(WriteHandler);
            Thread[] compressors = new Thread[Environment.ProcessorCount];
            for (int i = 0; i < compressors.Length; ++i)
                compressors[i] = new Thread(CompressHandler);

            reader.Start();
            foreach (var comp in compressors)
                comp.Start();
            writer.Start();

            reader.Join();
            compressQueue.TheEnd();

            foreach (var comp in compressors)
                comp.Join();
            writeQueue.TheEnd();

            writer.Join();
        }

        void ReadHandler()
        {
            FileChunk chunk;

            try
            {
                while ((chunk = source.SyncNextChunk()) != null)
                    compressQueue.Enqueue(chunk);
            }
            catch (Exception ex)
            {
                compressQueue.TheEnd();
                Console.WriteLine(String.Format("Поток чтения аварийно прерван!{0}{1}", Environment.NewLine, ex.Message));
                return;
            }
        }

        void CompressHandler()
        {
            FileChunk chunk;

            try
            {
                while ((chunk = compressQueue.Dequeue()) != null)
                {
                    using (var dest = new MemoryStream())
                    {
                        using (var gzipOut = new GZipStream(dest, CompressionMode.Compress))
                            gzipOut.Write(chunk.Data, 0, chunk.Data.Length);

                        writeQueue.Enqueue(new FileChunk(dest.ToArray(), chunk.Number));
                    }
                }
            }
            catch (Exception ex)
            {
                writeQueue.TheEnd();
                Console.WriteLine(String.Format("Поток обработки аварийно прерван!{0}{1}", Environment.NewLine, ex.Message));
                return;
            }
        }

        void WriteHandler()
        {
            FileChunk chunk;

            try
            {
                while ((chunk = writeQueue.Dequeue()) != null)
                {
                    var destination = new FileInfo(chunkNamePattern + chunk.Number);
                    if (destination.Exists)
                        throw new ArgumentException("Имя файла, необходимое сегменту архива, уже занято!");

                    using (var outputFile = destination.Open(FileMode.Create, FileAccess.Write))
                        outputFile.Write(chunk.Data, 0, chunk.Data.Length);
                }
            }
            catch (Exception ex)
            {
                writeQueue.TheEnd();
                Console.WriteLine(String.Format("Поток записи аварийно прерван!{0}{1}", Environment.NewLine, ex.Message));
                return;
            }

            Console.WriteLine("Успех: все сегменты были успешно обработаны!");
        }

    }
}
