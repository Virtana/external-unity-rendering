using System;
using System.IO;
using System.Linq;
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
        private readonly AwaitableConcurrentQueue<string> _messageQueue = new AwaitableConcurrentQueue<string>();
        // TODO dispatch multiple listeners to collect data

        /// <summary>
        /// Initialise a receiver and bind and listen on the socket.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="ipAddr">The IP address to listen on.</param>
        public Server(int port, string ipAddr, int maxListeners = 5)
        {
            try
            {
                IPAddress ipAddress = null;
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                if (Dns.GetHostAddresses(ipAddr).Any((hostIP) =>
                        localIPs.Any((localIP) =>
                            hostIP.Equals(localIP)) || IPAddress.IsLoopback(hostIP)))
                {
                    IPHostEntry host = Dns.GetHostEntry(ipAddr);
                    ipAddress = host.AddressList[0];
                }
                else
                {
                    // Get Host IP Address that is used to establish a connection
                    // In this case, we get one IP address of localhost that is IP : 127.0.0.1
                    // If a host has multiple addresses, you will get a list of addresses

                    ipAddress = IPAddress.Parse(ipAddr);
                    // Create a Socket that will use Tcp protocol
                }

                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);
                listener.Listen(maxListeners);

                //Task.Run(() => AcceptAsync(listener));
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

        public async void ProcessCallbackAsync(Func<string, bool> dataReceivedCallback)
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
                    Debug.LogWarning("Failed to read from internal channel");
                }
            }

            Debug.Log("Finished importing.");
        }

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

        // TODO test accept and delete receive

        /// <summary>
        /// Begin receiving data asynchronously.
        /// </summary>
        public async void ReceiveAsync(Socket listener)
        {
            StringBuilder sb = new StringBuilder();
            ArraySegment<byte> cache = new ArraySegment<byte>(new byte[1024]);

            Socket handler;
            // terminates on application exit, mayber replace??
            bool successfulReceipt = false;
            while (true)
            {
                try
                {
                    handler = await listener.AcceptAsync();

                    int bytesReceived = 0;
                    int totalBytesReceived = 0;
                    do
                    {
                        bytesReceived = await handler.ReceiveAsync(cache, SocketFlags.None);
                        totalBytesReceived += bytesReceived;
                        sb.Append(Encoding.ASCII.GetString(cache.Array, 0, bytesReceived));
                    } while (!(handler.Poll(1, SelectMode.SelectRead) && handler.Available == 0));

                    Debug.Log($"Read {totalBytesReceived} bytes from {handler.RemoteEndPoint} at {DateTime.Now}.");
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
                    _messageQueue.Enqueue(sb.ToString());
                }

                sb.Clear();
                successfulReceipt = false;
            }
        }

        /// <summary>
        /// Begin receiving data asynchronously.
        /// </summary>
        public async void AcceptAsync(Socket listener)
        {
            while (true)
            {
                try
                {
                    ReceiveDataAsync(await listener.AcceptAsync());
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
            }
        }

        private async void ReceiveDataAsync(Socket handler)
        {
            ArraySegment<byte> cache = new ArraySegment<byte>(new byte[1024]);
            string data = null;
            bool successfulReceipt = false;
            try
            {
                int bytesReceived = 0;
                int totalBytesReceived = 0;
                do
                {
                    bytesReceived = await handler.ReceiveAsync(cache, SocketFlags.None);
                    totalBytesReceived += bytesReceived;
                    data = Encoding.ASCII.GetString(cache.Array, 0, bytesReceived);
                } while (!(handler.Poll(1, SelectMode.SelectRead) && handler.Available == 0));

                Debug.Log($"Read {totalBytesReceived} bytes from {handler.RemoteEndPoint} at {DateTime.Now}.");
                successfulReceipt = true;
            }
            catch (SocketException se)
            {
                Debug.Log("An error occurred when attempting to access the socket." +
                $"ErrorCode: {se.SocketErrorCode}\n{se}");
            }
            catch (ObjectDisposedException ode)
            {
                Debug.LogError($"The socket or memory stream has been closed.\n{ode}");
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
            catch (ArgumentException ae)
            {
                Debug.LogError("The memory stream is unable to read from the byte " +
                $"buffer.\n{ae}");
            }
            catch (NotSupportedException nse)
            {
                Debug.LogError("An error has occurred that prevents writing to the " +
                $"memory stream.\n{nse}");
            }
            catch (IOException ioe)
            {
                Debug.LogError($"An I/O error has occurred.\n{ioe}");
            }
            catch (OutOfMemoryException oome)
            {
                // log but don't handle. Unity **should** handle this appropriately
                // The following link (split in two)
                // https://docs.microsoft.com/en-us/dotnet/api/system.outofmemoryexception?
                // view=net-5.0#:~:text=This%20type%20of%20OutOfMemoryException,example%20does.
                // says that Environment.FailFast() should be called, but unity should do that
                // if unity doesn't do it well ¯\_(ツ)_/¯
                Debug.LogError("Catastrophic error. Out of memory when trying to " +
                    $"read from the cache.\n{ oome }");
                throw;
            }

            if (successfulReceipt)
            {
                _messageQueue.Enqueue(data);
            }

            handler.Shutdown(SocketShutdown.Both);
            handler.Disconnect(false);
            handler.Close();
        }
    }
}
