using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace GZipTest.Helpers.Multithreading
{
    public class ProducerConsumer<T> where T : class
    {
        const int DEFAULT_LIMIT = 3;
        readonly object locker = new object();

        readonly Queue<T> queue = new Queue<T>();
        bool isStoped = false;

        readonly int limit;

        public ProducerConsumer(int limit = DEFAULT_LIMIT)
        {
            this.limit = limit > 0 ?
                limit : throw new ArgumentException("Предельный размер очереди должен быть положительным числом.");
        }

        public void Enqueue(T task)
        {
            if (task == null)
                throw new ArgumentNullException("Эта очередь не предполагает возможности хранения null-элементов.");
            lock (locker)
            {
                while (queue.Count > limit && !isStoped)
                    Monitor.Wait(locker);                        

                queue.Enqueue(task);
                Monitor.Pulse(locker);
            }
        }

        public T Dequeue()
        {
            lock (locker)
            {
                while (queue.Count == 0 && !isStoped)
                    Monitor.Wait(locker);

                if (queue.Count == 0)
                    return null;

                var element = queue.Dequeue();

                Monitor.Pulse(locker);

                return element;
            }
        }

        public void TheEnd()
        {
            lock (locker)
            {
                isStoped = true;
                Monitor.PulseAll(locker);
            }
        }
    }
}
