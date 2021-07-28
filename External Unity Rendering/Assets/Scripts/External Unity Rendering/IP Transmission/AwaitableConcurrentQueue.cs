using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Wrapper for <see cref="ConcurrentQueue{T}"/> that provides await functionality for dequeue.
/// Represents a thread-safe first in-first out (FIFO) collection.
/// </summary>
/// <typeparam name="T">Datatype that is stored in the queue.</typeparam>
/// <seealso cref="ConcurrentQueue{T}"/>
public class AwaitableConcurrentQueue<T> : ConcurrentQueue<T>
{
    /// <summary>
    /// Event that is signaled whenever data is available to be read in the queue.
    /// </summary>
    public readonly AutoResetEvent DataAvailable = new AutoResetEvent(false);

    /// <summary>
    /// Maximum number of elements to keep in the queue.
    /// </summary>
    private readonly int _maxCount = 0;

    /// <summary>
    /// The bool representing whether the queue has been closed. After closing, no more
    /// data can be written to the queue.
    /// </summary>
    private bool _closed = false;

    /// <summary>
    /// Gets whether data can be read from the queue.
    /// </summary>
    /// <value>
    /// true if there are items in the queue, or the queue is not closed, otherwise false.
    /// </value>
    public bool QueueComplete {
        get
        {
            return Count > 0 || !_closed;
        }
    }

    /// <summary>
    /// Create an unbounded <see cref="AwaitableConcurrentQueue{T}"/>.
    /// </summary>
    /// <seealso cref="ConcurrentQueue{T}.ConcurrentQueue"/>
    public AwaitableConcurrentQueue() : base() { }

    /// <summary>
    /// Create a bounded <see cref="AwaitableConcurrentQueue{T}"/> that when full, blocks until
    /// there is space in the queue.
    /// </summary>
    /// <param name="maxQueueCount"> Max number of elements to have in the queue. Minimum of 1.
    /// </param>
    /// <seealso cref="ConcurrentQueue{T}.ConcurrentQueue"/>
    public AwaitableConcurrentQueue(int maxQueueCount) : base()
    {
        if (maxQueueCount > 1)
        {
            _maxCount = maxQueueCount;
        }
    }

    /// <summary>
    /// Create an unbounded <see cref="AwaitableConcurrentQueue{T}"/> that contains elements copied
    /// from the specified collection.
    /// </summary>
    /// <param name="collection">The collection of elements to initialise the queue with.</param>
    /// <seealso cref="ConcurrentQueue{T}.ConcurrentQueue(IEnumerable{T})"/>
    public AwaitableConcurrentQueue(IEnumerable<T> collection) : base(collection) { }

    /// <summary>
    /// Create a bounded <see cref="AwaitableConcurrentQueue{T}"/> that contains elements copied
    /// from the specified collection. When full, it blocks until there is space available.
    /// </summary>
    /// <param name="collection">The collection of elements to initialise the queue with.</param>
    /// <param name="maxQueueCount">Max number of elements to have in the queue. Minimum of 1.
    /// </param>
    /// <seealso cref="ConcurrentQueue{T}.ConcurrentQueue(IEnumerable{T})"/>
    public AwaitableConcurrentQueue(IEnumerable<T> collection, int maxQueueCount) : base(collection)
    {
        if (maxQueueCount > 1)
        {
            _maxCount = maxQueueCount;
        }
    }

    /// <summary>
    /// Get the value at the top of the queue.
    /// </summary>
    /// <returns> A Task wrapping a tuple of <see cref="bool"/> success and <typeparamref name="T"/>
    /// value. If success is true then value is the dequeued item. If false, it is the default value
    /// of <typeparamref name="T"/>.</returns>
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
            DataAvailable.WaitOne();
        });

        success = TryDequeue(out item);
        return (success, item);
    }

    /// <summary>
    /// Adds an object to the end of the <see cref="AwaitableConcurrentQueue{T}"/> if the queue is not closed.
    /// </summary>
    /// <param name="item">The object to add to the end of the
    /// <see cref="AwaitableConcurrentQueue{T}"/>. The value can be a null reference (Nothing in
    /// Visual Basic) for reference types.</param>
    /// <returns>true if the <paramref name="item"/> was added to end of the queue,
    /// otherwise false.</returns>
    public new bool Enqueue(T item)
    {
        if (_closed) {
            return false;
        }

        // if a limit is set and reached
        if (_maxCount > 0 && _maxCount >= Count)
        {
            SpinWait waiter = new SpinWait();
            // spin until space opens up
            while (_maxCount >= Count)
            {
                waiter.SpinOnce();
            }
        }

        base.Enqueue(item);
        DataAvailable.Set();
        return true;
    }

    /// <summary>
    /// Set the queue to closed. After the last item has been read, attempts at
    /// dequeuing will return false.
    /// </summary>
    public void Close()
    {
        _closed = true;
    }
}
