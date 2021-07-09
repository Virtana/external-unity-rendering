using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AwaitableConcurrentQueue
{
    public class AwaitableConcurrentQueue<T> : ConcurrentQueue<T>
    {
        public AwaitableConcurrentQueue() : base() {}
        public AwaitableConcurrentQueue(IEnumerable<T> collection ) : base(collection) { }

        private AutoResetEvent _available = new AutoResetEvent(false);

        private bool _closed = false;


        public bool DataAvailable {
            get 
            {
                return Count > 0 || !_closed;
            }
        }

        public new bool Enqueue(T item)
        {   
            // for now do nothing if set, would be better to throw an exception
            if (_closed) {
                //throw new Exception();
                return false;
            }
            
            base.Enqueue(item);
            _available.Set();
            return true;
        }

        public async Task<(bool success, T value)> DequeueAsync()
        {
            bool success;
            T item;
            if (_closed && Count == 0)
            {
                return (false, default(T));
            } 
            else if (Count > 0)
            {
                success = TryDequeue(out item);
                return (success, item);
            }

            await Task.Run(() => {
                _available.WaitOne();
            });
            
            success = TryDequeue(out item);
            return (success, item);
        }

        public void Finish(CancellationToken token = default)
        {
            _closed = true;
        }
    }
}