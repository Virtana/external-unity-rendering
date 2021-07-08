using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UnityEngine;

namespace ExternalUnityRendering.TcpIp
{
    /// <summary>
    /// Class that manages socket transmission.
    /// </summary>
    public class Sender
    {
        /// <summary>
        /// Container for data to be passed to the async callback.
        /// </summary>
        private struct SendState
        {
            public SocketError errorCode;
            public Socket socket;
            public List<ArraySegment<byte>> data;
            public SocketFlags flags;
        }

        /// <summary>
        /// Internal queue of data to be sent. Works asynchronously.
        /// </summary>
        private readonly Channel<string> _dataToSend =
            Channel.CreateBounded<string>(new BoundedChannelOptions(10)
            {
                SingleReader = true,
                SingleWriter = true
            });

        /// <summary>
        /// The host that this class transmits to.
        /// </summary>
        private readonly IPHostEntry _host;

        /// <summary>
        /// The IP address of the host.
        /// </summary>
        private readonly IPAddress _ipAddress;

        /// <summary>
        /// An IPEndpoint consisting of the IP address and the port to communicate over.
        /// </summary>
        private readonly IPEndPoint _remoteEndPoint;


        /// <summary>
        /// The maximum number of attempts that the sender will try to send the
        /// data if the connection is refused.
        /// </summary>
        private readonly int _maxAttempts;

        /// <summary>
        /// The size in bytes of each chunk of data.
        /// </summary>
        private readonly int _chunkSize = 50;

        /// <summary>
        /// Event internally used to signal the completion state of the queue.
        /// </summary>
        private readonly ManualResetEvent _queueClosed = new ManualResetEvent(false);

        /// <summary>
        /// Helper function to split a string into chunks of bytes.
        /// </summary>
        /// <param name="data">The string to be converted.</param>
        /// <returns>A list of array segments to be used during transmission.</returns>
        private List<ArraySegment<byte>> ConvertToBuffer(string data)
        {
            byte[] dataAsBytes = Encoding.ASCII.GetBytes(data);

            List<ArraySegment<byte>> buffer = new List<ArraySegment<byte>>();

            for (int i = 0; i < dataAsBytes.Length; i += _chunkSize)
            {
                buffer.Add(new ArraySegment<byte>(dataAsBytes, i, Math.Min(_chunkSize, dataAsBytes.Length - i)));
            }

            return buffer;
        }

        /// <summary>
        /// Async callback to end connection.
        /// </summary>
        /// <param name="result">Represents the status of an asynchronous operation.</param>
        private void SendDataCallback(IAsyncResult result)
        {
            // NOTE: need to investigate what to do if result.isCompleted is false.
            SendState state = (SendState)result.AsyncState;


            if (state.errorCode != SocketError.Success)
            {
                Debug.LogError($"Socket Error: {state.errorCode}");
                return;
            }
            if (!result.IsCompleted)
            {
                Debug.LogWarning("Transmission is not completed. Data may not have been " +
                    "handled correctly.");
            }
            Socket socket = state.socket;
            socket.EndSend(result);
            socket.Close();
        }

        /// <summary>
        /// Initialize the sending queue to send data asynchronously.
        /// </summary>
        private async void InitializeSender()
        {
            Debug.Log("Opening message queue.");
            while (await _dataToSend.Reader.WaitToReadAsync())
            {
                // add fail check
                _dataToSend.Reader.TryRead(out string item);

                // Create a TCP/IP socket.
                Socket sender = new Socket(_ipAddress.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);

                if (sender == null)
                {
                    Debug.LogError("Cannot send data. No socket was assigned during initializaiton. " +
                        "An error may have occured. Try reassigning the socket.");
                    continue;
                }

                int connectionAttempts = 0;
                while (connectionAttempts < _maxAttempts)
                {
                    try
                    {
                        sender.Connect(_remoteEndPoint);
                        break;
                    }
                    catch (SocketException se)
                    {
                        Debug.LogError("Socket Exception occurred while trying to connect! " +
                            $"Error: {se.SocketErrorCode}. " +
                            $"Error Code: {se.ErrorCode}.");

                        if (se.ErrorCode != 10061 || _maxAttempts == connectionAttempts)
                        {
                            // if error is not connection refused or has run out of attempts
                            Debug.LogError("Aborting...");

                            // try queueing again?

                            break;
                        }

                        // add polling
                        Debug.Log($"Tried {++connectionAttempts}/{_maxAttempts} times. Retrying...");
                        await Task.Delay(100);
                        continue;
                    }
                    catch (ObjectDisposedException ode)
                    {
                        Debug.LogError($"The socket has been closed.\n{ode}");
                    }
                    catch (System.Security.SecurityException se)
                    {
                        Debug.LogError("A caller higher in the call stack does not have permission " +
                            $"for the requested operation.\n{se}");
                    }
                    catch (InvalidOperationException ioe)
                    {
                        Debug.LogError("The socket has been placed in a listening state by calling " +
                            $"Listen(Int32).\n{ioe}");
                    }
                }

                SendState state = new SendState
                {
                    socket = sender,
                    data = ConvertToBuffer(item),
                    flags = SocketFlags.None
                };

                try
                {
                    await Task.Factory.FromAsync((callback, callbackData) =>
                    {
                        return sender.BeginSend(state.data, state.flags, callback, callbackData);
                    }, SendDataCallback, state);
                }
                catch (SocketException se)
                {
                    // handle according to
                    // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socketerror?view=net-5.0
                    Debug.LogError($"Socket Error: {se.ErrorCode}");
                }
                catch (ObjectDisposedException ode)
                {
                    Debug.LogError($"The socket has been closed.\n{ode}");
                }
            }

            Debug.Log("Completing...");
            _queueClosed.Set();
        }

        /// <summary>
        /// Initialize data for the socket transmission.
        /// </summary>
        /// <param name="port">The port to send data over.</param>
        /// <param name="ipString">The string representing the IP address.</param>
        /// <param name="maxRetries">The maximum number of times to retry sending data
        /// after the connection has been refused.</param>
        public Sender(int port = 11000, string ipString = "localhost", int maxRetries = 3)
        {
            try
            {
                _maxAttempts = maxRetries;
                // Connect to a Remote server
                // Get Host IP Address that is used to establish a connection
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1
                // If a host has multiple addresses, you will get a list of addresses
                _host = Dns.GetHostEntry(ipString);
                _ipAddress = _host.AddressList[0];
                _remoteEndPoint = new IPEndPoint(_ipAddress, port);

                InitializeSender();
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
        /// Add a string to the queue of data to be sent.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool QueueSend(string data)
        {
            _queueClosed.Set();
            return _dataToSend.Writer.TryWrite(data);
        }

        /// <summary>
        /// Queue a closing message to the server and wait until it has been sent.
        /// </summary>
        public void FinishTransmissionsAndClose()
        {
            Debug.Log("Sending closing message.");
            _dataToSend.Writer.WriteAsync(
                Newtonsoft.Json.JsonConvert.SerializeObject(new SerializableScene()
                {
                    ContinueImporting = false
                }));
            _dataToSend.Writer.Complete();

            _queueClosed.WaitOne();
            Debug.Log("Closed queue. When queue is empty, the program will terminate.");
        }
    }
}
