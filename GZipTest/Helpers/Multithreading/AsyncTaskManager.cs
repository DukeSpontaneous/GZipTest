using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Threading.Tasks;

namespace GZipTest.Helpers.Multithreading
{
    class AsyncTaskManager : IDisposable
    {
        readonly Queue<Task> queue = new Queue<Task>();
        readonly List<Task> tasks = new List<Task>();
        CancellationTokenSource cancelSource = new CancellationTokenSource();

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

        public void Dispose()
        {
            cancelSource.Dispose();
        }

        public void Add(Task task)
        {
            lock (tasks)
            {
                if (tasks.Count < Environment.ProcessorCount)
                {
                    task.Start();
                    tasks.Add(task);

                    lock (cancelSource)
                    {
                        cancelSource.Cancel();
                    }
                }
                else
                {
                    queue.Enqueue(task);
                }
            }
        }

        public void Run()
        {
            while (!this.IsCompleted)
            {
                lock (tasks)
                {
                    for (int i = tasks.Count - 1; i < Environment.ProcessorCount && queue.Count > 0; ++i)
                    {
                        var task = queue.Dequeue();
                        task.Start();
                        tasks.Add(task);
                    }
                }

                CancellationToken cancel;

                do
                {
                    lock (cancelSource)
                    {
                        cancel = cancelSource.Token;
                    }

                    Task[] list;

                    lock (tasks)
                    {
                        list = tasks.ToArray();
                    }

                    try
                    {
                        Task.WaitAny(list, cancel);
                    }
                    catch (OperationCanceledException e)
                    {
                        // Очередь заданий обновлена
                    }                    

                    if (cancel.IsCancellationRequested)
                    {
                        lock (cancelSource)
                        {
                            cancelSource.Dispose();
                            cancelSource = new CancellationTokenSource();
                        }
                    }
                }
                while (cancel.IsCancellationRequested);

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
