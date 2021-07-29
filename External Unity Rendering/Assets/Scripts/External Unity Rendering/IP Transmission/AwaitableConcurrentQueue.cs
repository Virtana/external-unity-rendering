using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Wrapper for <see cref="System.Collections.Generic.ConcurrentQueue"/> that provides await
/// functionality for dequeue. Represents a thread-safe first in-first out (FIFO) collection.
/// </summary>
/// <typeparam name="T">Datatype that is stored in the queue.</typeparam>
/// <seealso cref="ConcurrentQueue{T}"/>
public class AwaitableConcurrentQueue<T> : ConcurrentQueue<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AwaitableConcurrentQueue{T}"/> class.
    /// </summary>
    /// <seealso cref="ConcurrentQueue{T}.ConcurrentQueue"/>
    public AwaitableConcurrentQueue() : base() {}

    /// <summary>
    /// Initializes a new instance of the <see cref="AwaitableConcurrentQueue{T}"/> class that
    /// contains elements copied from the specified collection.
    /// </summary>
    /// <param name="collection">The collection of elements to initialise the queue with.</param>
    /// <seealso cref="ConcurrentQueue{T}.ConcurrentQueue(IEnumerable{T})"/>
    public AwaitableConcurrentQueue(IEnumerable<T> collection) : base(collection) { }

    /// <summary>
    /// Event that is signaled whenever data is available to be read in the queue.
    /// </summary>
    public readonly AutoResetEvent DataAvailable = new AutoResetEvent(false);

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
    /// Adds an object to the end of the AwaitableConcurrentQueue<T> if the queue is not closed.
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

        base.Enqueue(item);
        DataAvailable.Set();
        return true;
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
    /// Set the queue to closed. After the last item has been read, attempts at
    /// dequeuing will return false.
    /// </summary>
    public void Close()
    {
        _closed = true;
    }
}
