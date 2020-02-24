using System.Collections.Generic;

namespace System.Collections.Concurrent
{
#if NET35

    internal class ConcurrentQueue<T>
    {
        private Queue<T> _queue = new Queue<T>();

        public void Enqueue(T resource)
        {
            lock (_queue)
            {
                _queue.Enqueue(resource);
            }
        }

        public bool TryDequeue(out T resource)
        {
            lock (_queue)
            {
                var result = _queue.Count > 0;
                if (result)
                {
                    resource = _queue.Dequeue();
                }
                else
                {
                    resource = default(T);
                }
                return result;
            }
        }
    }

#endif
}
