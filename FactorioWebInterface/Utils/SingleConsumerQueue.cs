using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FactorioWebInterface.Utils
{
    public class SingleConsumerQueue<T>
    {
        private Queue<T> queue = new Queue<T>();
        private volatile TaskCompletionSource<T> taskCompletionSource;

        private readonly Func<T, Task> consumer;

        public SingleConsumerQueue(Func<T, Task> consumer)
        {
            this.consumer = consumer;

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var item = await DequeueAsync();
                        await consumer(item);
                    }
                    catch
                    {
                    }
                }
            });
        }

        public void Enqueue(T item)
        {
            lock (queue)
            {
                if (taskCompletionSource != null)
                {
                    var tcs = taskCompletionSource;
                    taskCompletionSource = null;
                    tcs.SetResult(item);
                }
                else
                {
                    queue.Enqueue(item);
                }
            }
        }

        public int Count
        {
            get
            {
                lock (queue)
                {
                    return queue.Count;
                }
            }
        }

        private Task<T> DequeueAsync()
        {
            lock (queue)
            {
                if (queue.TryDequeue(out T item))
                {
                    return Task.FromResult(item);
                }

                taskCompletionSource = new TaskCompletionSource<T>();
                return taskCompletionSource.Task;
            }
        }
    }
}
