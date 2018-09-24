using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FactorioWebInterface.Utils
{
    public class SingleConsumerQueue<T>
    {
        private Queue<T> queue = new Queue<T>();

        private event Action ItemAdded;

        private readonly Func<T, Task> consumer;

        public SingleConsumerQueue(Func<T, Task> consumer)
        {
            this.consumer = consumer;

            Task.Run(async () =>
            {
                while (true)
                {
                    var item = await DequeueAsync();
                    await consumer(item);
                }
            });
        }

        public void Enqueue(T item)
        {
            lock (queue)
            {
                queue.Enqueue(item);
                ItemAdded?.Invoke();
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
            }

            var tcs = new TaskCompletionSource<T>();

            void handler()
            {
                ItemAdded -= handler;
                var item = queue.Dequeue();
                tcs.SetResult(item);
            }

            lock (queue)
            {
                ItemAdded += handler;
            }

            return tcs.Task;
        }
    }
}
