using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Wrapper for Concurrent Queue from System.Collections.Generic.
/// Represents a thread-safe first in-first out (FIFO) collection.
/// </summary>
/// <typeparam name="T">Datatype that is stored in the Queue.</typeparam>
/// <seealso cref="ConcurrentQueue{T}"/>
public class AwaitableConcurrentQueue<T> : ConcurrentQueue<T>
{
    /// <summary>
    /// Initializes a new instance of the AwaitableConcurrentQueue<T> class.
    /// </summary>
    /// <seealso cref="ConcurrentQueue{T}.ConcurrentQueue"/>
    public AwaitableConcurrentQueue() : base() {}

    /// <summary>
    /// Initializes a new instance of the AwaitableConcurrentQueue<T> class that contains
    /// elements copied from the specified collection.
    /// </summary>
    /// <param name="collection"></param>
    /// <seealso cref="ConcurrentQueue{T}.ConcurrentQueue(IEnumerable{T})"/>
    public AwaitableConcurrentQueue(IEnumerable<T> collection ) : base(collection) { }

    /// <summary>
    /// Event that is signaled whenever data is available to be read in the queue.
    /// </summary>
    private readonly AutoResetEvent _available = new AutoResetEvent(false);

    /// <summary>
    /// Event that is signaled whenever the queue becomes empty.
    /// </summary>
    private readonly AutoResetEvent _empty = new AutoResetEvent(false);

    /// <summary>
    /// The bool representing whether the Queue has been closed. After closing, no more
    /// data can be written to the queue.
    /// </summary>
    private bool _closed = false;

    /// <summary>
    /// Gets whether data can be read from the queue.
    /// </summary>
    /// <value>
    /// true if there are items in the queue, or the queue is not closed, otherwise false.
    /// </value>
    public bool DataAvailable {
        get
        {
            return Count > 0 || !_closed;
        }
    }

    /// <summary>
    /// Adds an object to the end of the AwaitableConcurrentQueue<T> if the queue is not
    /// closed.
    /// </summary>
    /// <param name="item">The object to add to the end of the ConcurrentQueue<T>. The value
    /// can be a null reference (Nothing in Visual Basic) for reference types.</param>
    /// <returns>true if the <paramref name="item"/> was added to end of the queue,
    /// otherwise false.</returns>
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

    /// <summary>
    /// Returns a Task bool, TResult that will complete when data is available to read.
    /// </summary>
    /// <returns>A tuple containing whether the read was successful and the read item,
    /// or a default value if no item could be read.</returns>
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

    /// <summary>
    /// Set the queue to closed. After the last item has been read, attempts at
    /// dequeuing will return false.
    /// </summary>
    public void Close()
    {
        _closed = true;
    }
}
