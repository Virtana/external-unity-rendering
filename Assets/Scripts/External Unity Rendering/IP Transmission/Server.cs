using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace ExternalUnityRendering.TcpIp
{
    public class Server
    {
        /// <summary>
        /// Internal queue holding all received messages.
        /// </summary>
        private readonly AwaitableConcurrentQueue<string> _messageQueue =
            new AwaitableConcurrentQueue<string>();
        // TODO dispatch multiple listeners to collect data

        /// <summary>
        /// Begin receiving data asynchronously.
        /// </summary>
        /// <param name="listener">
        /// Socket that is currently listening for sockets to connect.
        /// </param>
        private async void ReceiveAsync(Socket listener)
        {
            Socket handler;
            bool successfulReceipt = false;
            byte[] cache = new byte[1024];
            ArraySegment<byte> segmentCache = new ArraySegment<byte>(cache);

            using (MemoryStream ms = new MemoryStream())
            while (true)
            {
                try
                {
                    handler = await listener.AcceptAsync();

                    int bytesReceived = 0;
                    int totalBytesReceived = 0;
                    do
                    {
                        bytesReceived = await handler.ReceiveAsync(segmentCache, SocketFlags.None);
                        totalBytesReceived += bytesReceived;
                        await ms.WriteAsync(cache, 0, bytesReceived);
                    } while (!(handler.Poll(1, SelectMode.SelectRead) && handler.Available == 0));

                    if (totalBytesReceived == 0)
                    {
                        Debug.Log("Empty string received.");
                        continue;
                    }

                    Debug.Log($"Read {totalBytesReceived} bytes from {handler.RemoteEndPoint} at " +
                        $"{DateTime.Now}.");
                    successfulReceipt = true;
                }
                catch (SocketException se)
                {
                    Debug.Log("An error occurred when attempting to access the socket." +
                    $"ErrorCode: {se.SocketErrorCode}\n{se}");
                }
                catch (ObjectDisposedException ode)
                {
                    Debug.LogError($"The socket has been closed.\n{ode}");
                }
                catch (InvalidOperationException ioe)
                {
                    Debug.LogError("The accepting socket is not listening for connections." +
                    " You must call Bind(EndPoint) and Listen(Int32) before calling " +
                    $"Accept().\n{ioe}");
                }
                catch (System.Security.SecurityException se)
                {
                    Debug.LogError("A caller higher in the call stack does not have permission " +
                    $"for the requested operation.\n{se}");
                }
                catch (IOException ioe)
                {
                    Debug.LogError($"An I/O error has occurred.\n{ioe}");
                }

                if (successfulReceipt)
                {
                    _messageQueue.Enqueue(Encoding.UTF8.GetString(ms.ToArray()));
                }
                ms.Seek(0, SeekOrigin.Begin);
                ms.SetLength(0);
                ms.Capacity = 0;
                successfulReceipt = false;
            }
        }

        /// <summary>
        /// Initialise a receiver and bind and listen on the socket.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="ipAddr">The IP address to listen on.</param>
        /// <param name="maxListeners"> The maximum number of sockets that can be accepted, or
        /// queued to be accepted, at any point in time.</param>
        public Server(int port, string ipAddr, int maxListeners = 5)
        {
            try
            {
                IPAddress ipAddress = null;
                if (ipAddr == "localhost")
                {
                    ipAddress = IPAddress.Loopback;
                }
                else
                {
                    ipAddress = IPAddress.Parse(ipAddr);
                }

                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
                // Create a Socket that will use Tcp protocol
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);
                listener.Listen(maxListeners);

                Task.Run(() => ReceiveAsync(listener));

                Debug.Log($"Listening on {localEndPoint}. Waiting for a connection...");
            }
            catch (SocketException se)
            {
                Debug.LogError("An error occured while trying to initialise the socket. " +
                       $"The error code is {se.SocketErrorCode}.\n{se}");
            }
            catch (ArgumentException ae)
            {
                Debug.LogError("An error occurred while trying to resolve the host. " +
                    $"\n{ae}");
            }
        }

        /// <summary>
        /// Synchronously process data received by the <see cref="Server"/>.
        /// </summary>
        /// <param name="dataReceivedCallback">Function to pass the received data. Returns whether
        /// to keep processing or stop.</param>
        public void ProcessCallback(Func<string, bool> dataReceivedCallback)
        {
            bool continueReading = true;
            string data;
            // after a few nanoseconds, should yield every time it checks, reducing cpu time wasted
            SpinWait waiter = new SpinWait();
            while (continueReading && _messageQueue.QueueComplete)
            {
                while (!_messageQueue.TryDequeue(out data))
                {
                    waiter.SpinOnce();
                }
                continueReading = dataReceivedCallback(data);
            }

            Debug.Log("Finished importing.");
        }

        /// <summary>
        /// Asynchronously process data received by the <see cref="Server"/>.
        /// </summary>
        /// <param name="dataReceivedCallback">Function to pass the received data. Returns whether
        /// to keep processing or stop.</param>
        public async Task ProcessCallbackAsync(Func<string, bool> dataReceivedCallback)
        {
            bool continueReading = true;

            while (continueReading && _messageQueue.QueueComplete)
            {
                (bool readSuccess, string data) = await _messageQueue.DequeueAsync();
                if (readSuccess)
                {
                    continueReading = dataReceivedCallback(data);
                }
                else
                {
                    Debug.LogWarning("Failed to read from internal channel.");
                }
            }

            Debug.Log("Finished importing.");
        }
    }
}
