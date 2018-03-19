using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Threading;

using GZipTest.Helpers.Accessors;

namespace GZipTest.Helpers.Multithreading
{
    class ThreadWriter
    {
        Thread thread;
        Queue<FileChunk> chunks;
        ManualResetEvent resetEvent;
        Func<bool> action;

        readonly string name;

        public ThreadWriter(string name, Func<bool> action)
        {
            chunks = new Queue<FileChunk>();
            resetEvent = new ManualResetEvent(false);
            thread = new Thread(ThreadHandler);
            this.action = action;
            this.name = name;

            thread.Start();
        }

        public void Add(FileChunk chunk)
        {
            lock (chunks)
            {
                chunks.Enqueue(chunk);
                resetEvent.Set();
            }
        }

        public void Start()
        {
            thread.Start();
        }

        public void Join()
        {
            thread.Join();
        }

        private int SyncGetQueueSize()
        {
            int size;
            lock (chunks)
            {
                size = chunks.Count;
            }
            return size;
        }

        private void ThreadHandler()
        {
            while (action() )
            {
                resetEvent.WaitOne();

                while (SyncGetQueueSize() > 0)
                {
                    FileChunk chunk;
                    lock (chunks)
                    {
                        chunk = chunks.Dequeue();

                        if (chunks.Count == 0) {
                            resetEvent.Reset();
                        }
                    }

                    string path = name + "." + chunk.Number;

                    using (var outFile = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        outFile.Write(chunk.Data, 0, chunk.Data.Length);
                    }
                }
            }
        }
    }
}
