using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace GZipTest.Helpers.Multithreading
{
    class MyThreadPool
    {
        private readonly Thread[] threadPool;

        public MyThreadPool(ParameterizedThreadStart handler, int threadPoolSize)
        {
            if (threadPoolSize <= 0)
            {
                throw new ArgumentException("Недопустимое значение размера пула Thread'ов.");
            }

            threadPool = new Thread[threadPoolSize];
            for (int i = 0; i < threadPoolSize; ++i)
            {
                threadPool[i] = new Thread(handler);
            }
        }

        public void Execute(object obj)
        {
            foreach (var t in threadPool)
            {
                t.Start(obj);
            }
            foreach (var t in threadPool)
            {
                t.Join();
            }
        }

    }
}
