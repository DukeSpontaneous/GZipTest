using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Threading.Tasks;

namespace GZipTest.Helpers.Multithreading
{
    class MyTaskManager : IDisposable
    {
        private Queue<Task> queue;
        private List<Task> tasks = new List<Task>();
        private CancellationTokenSource source = new CancellationTokenSource();

        public bool IsCompleted
        {
            get
            {
                bool result;
                lock (tasks)
                {
                    result = queue.Count == 0 && tasks.Count == 0;
                }

                return result;
            }
        }

        public MyTaskManager()
        {
            queue = new Queue<Task>();
        }

        public void Dispose()
        {
            source.Dispose();
        }

        public void Add(Task task)
        {
            lock (tasks)
            {
                if (tasks.Count < Environment.ProcessorCount)
                {
                    task.Start();
                    tasks.Add(task);

                    lock (source)
                    {
                        source.Cancel();
                    }
                }
                else
                {
                    queue.Enqueue(task);
                }
            }
        }

        public void Execute()
        {
            while (!this.IsCompleted)
            {
                lock (tasks)
                {
                    for (int i = tasks.Count - 1; i < Environment.ProcessorCount && queue.Count > 0; ++i)
                    {
                        var t = queue.Dequeue();
                        t.Start();
                        tasks.Add(t);
                    }
                }

                CancellationToken token;

                do
                {
                    lock (source)
                    {
                        token = source.Token;
                    }

                    Task[] list;

                    lock (tasks)
                    {
                        list = tasks.ToArray();
                    }

                    try
                    {
                        Task.WaitAny(list, token);
                    }
                    catch (Exception e)
                    {

                    }

                    if (token.IsCancellationRequested)
                    {
                        lock (source)
                        {
                            source.Dispose();
                            source = new CancellationTokenSource();
                        }
                    }
                }
                while (token.IsCancellationRequested);

                lock (tasks)
                {
                    for (int i = tasks.Count - 1; i >= 0; --i)
                    {
                        if (tasks[i].IsCompleted)
                        {
                            tasks.RemoveAt(i);
                        }
                    }
                }
            }
        }
    }
}
