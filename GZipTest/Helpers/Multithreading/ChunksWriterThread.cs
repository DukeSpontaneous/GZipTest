using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Threading;

using GZipTest.Helpers.Accessors;

namespace GZipTest.Helpers.Multithreading
{
    class ChunksWriterThread 
    {
        readonly Thread thread;
        readonly Queue<FileChunk> chunks = new Queue<FileChunk>();
        readonly ManualResetEvent resetEvent = new ManualResetEvent(false);
        readonly Func<bool> stoppingСondition;

        readonly string name;

        public ChunksWriterThread(string name, Func<bool> stoppingСondition)
        {
            thread = new Thread(ThreadHandler);

            this.stoppingСondition = stoppingСondition;
            this.name = name;

            thread.Start();
        }

        public void Start()
        {
            thread.Start();
        }

        public void Join()
        {
            thread.Join();
        }

        public void SyncAddChunk(FileChunk chunk)
        {
            lock (chunks)
            {
                chunks.Enqueue(chunk);
                resetEvent.Set();
            }
        }

        int SyncGetQueueSize()
        {
            int size;
            lock (chunks)
            {
                size = chunks.Count;
            }
            return size;
        }

        void ThreadHandler()
        {
            while (stoppingСondition())
            {
                resetEvent.WaitOne();

                while (SyncGetQueueSize() > 0)
                {
                    FileChunk chunk;
                    lock (chunks)
                    {
                        chunk = chunks.Dequeue();

                        if (chunks.Count == 0)
                        {
                            resetEvent.Reset();
                        }
                    }

                    string path = String.Format("{0}.{1}", name, chunk.Number);

                    using (var outFile = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        outFile.Write(chunk.Data, 0, chunk.Data.Length);
                    }
                }
            }
        }
    }
}
