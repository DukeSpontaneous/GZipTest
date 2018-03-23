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
    class GZipDecompressConveyor
    {
        readonly Queue<FileInfo> sources;
        readonly ProducerConsumer<FileChunk> decompressQueue;
        readonly ProducerConsumer<FileChunk> writeQueue;

        readonly ChunkedFileWriter output;
        readonly int chunkSize;

        public GZipDecompressConveyor(Queue<FileInfo> sources, ChunkedFileWriter output, int chunkSize)
        {
            this.sources = sources != null ?
                sources : throw new ArgumentNullException();

            decompressQueue = new ProducerConsumer<FileChunk>();
            writeQueue = new ProducerConsumer<FileChunk>();

            this.output = output != null ?
                output : throw new ArgumentNullException();
            this.chunkSize = chunkSize > 0 ?
                chunkSize : throw new ArgumentException("Размер сегмента должен быть положительным числом.");
        }

        public void Execute()
        {
            var reader = new Thread(ReadHandler);
            var writer = new Thread(WriteHandler);
            Thread[] compressors = new Thread[Environment.ProcessorCount];
            for (int i = 0; i < compressors.Length; ++i)
                compressors[i] = new Thread(DecompressHandler);

            reader.Start();
            foreach (var comp in compressors)
                comp.Start();
            writer.Start();

            reader.Join();
            decompressQueue.TheEnd();

            foreach (var comp in compressors)
                comp.Join();
            writeQueue.TheEnd();

            writer.Join();
        }

        void ReadHandler()
        {
            FileInfo fi;
            byte[] chData;

            while (true)
            {
                lock (sources)
                {
                    if (sources.Count == 0)
                        break;

                    fi = sources.Dequeue();
                }

                try
                {
                    using (var reader = new BinaryReader(fi.Open(FileMode.Open, FileAccess.Read)))
                        chData = reader.ReadBytes(chunkSize);
                    int chNumber = int.Parse(fi.Extension.Remove(0, 1));

                    decompressQueue.Enqueue(new FileChunk(chData, chNumber));
                }
                catch (Exception ex)
                {
                    decompressQueue.TheEnd();
                    Console.WriteLine(String.Format("Поток чтения аварийно прерван!{0}{1}", Environment.NewLine, ex.Message));
                    return;
                }
            }
        }

        void DecompressHandler()
        {
            FileChunk chunk;
            byte[] chData;

            try
            {
                while ((chunk = decompressQueue.Dequeue()) != null)
                {
                    using (var ms = new MemoryStream(chunk.Data))
                    using (var gzipOut = new BinaryReader(new GZipStream(ms, CompressionMode.Decompress)))
                        chData = gzipOut.ReadBytes(chunkSize);

                    writeQueue.Enqueue(new FileChunk(chData, chunk.Number));
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
                    output.SyncFeed(chunk);
            }
            catch (Exception ex)
            {
                writeQueue.TheEnd();
                Console.WriteLine(String.Format("Поток записи аварийно прерван!{0}{1}", Environment.NewLine, ex.Message));
                return;
            }

            if (output.IsReady)
                Console.WriteLine("Успех: все сегменты были успешно обработаны!");
            else
                Console.WriteLine("Провел: не все найденные сегменты были обработаны!");
        }
    }
}
