using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        /// Internal queue of data to be sent. Works asynchronously.
        /// </summary>
        private readonly AwaitableConcurrentQueue<string> _messageQueue =
            new AwaitableConcurrentQueue<string>();

        /// <summary>
        /// Event internally used to signal the completion state of the queue.
        /// </summary>
        private readonly ManualResetEventSlim _completedTransmission =
            new ManualResetEventSlim(false);

        /// <summary>
        /// Helper function to split a string into chunks of bytes.
        /// </summary>
        /// <param name="data">The string to be converted.</param>
        /// <returns>A list of array segments to be used during transmission.</returns>
        private List<ArraySegment<byte>> ConvertToBuffer(string data, int chunkSize)
        {
            byte[] dataAsBytes = Encoding.ASCII.GetBytes(data);

            List<ArraySegment<byte>> buffer = new List<ArraySegment<byte>>();

            for (int i = 0; i < dataAsBytes.Length; i += chunkSize)
            {
                buffer.Add(new ArraySegment<byte>(
                    dataAsBytes, i, Math.Min(chunkSize, dataAsBytes.Length - i)));
            }

            return buffer;
        }

        /// <summary>
        /// Initialize the sending queue to send data asynchronously.
        /// </summary>
        private async void InitializeSender(IPAddress ipAddress, IPEndPoint remoteEndPoint,
            int maxAttempts, int chunkSize)
        {
            Debug.Log("Opening message queue.");

            using (Socket pinger = new Socket(ipAddress.AddressFamily, SocketType.Stream,
                    ProtocolType.Tcp))
            {
                while (!pinger.Connected)
                {
                    try
                    {
                        pinger.Connect(remoteEndPoint);
                        await Task.Delay(100);
                    }
                    // catchall to prevent socketexceptions, need to handle better
                    catch (SocketException se)
                    {
                        if (se.ErrorCode != 10050 && se.ErrorCode != 10061)
                        {
                            Debug.LogError($"While waiting for server to come online, received: {se.SocketErrorCode} {se.ErrorCode}");
                        }
                    }
                }
            }

            while (_messageQueue.DataAvailable)
            {
                (bool readSuccess, string data) = await _messageQueue.DequeueAsync();

                if (!readSuccess)
                {
                    Debug.Log("Failed to read from queue");
                    continue;
                }
                using (Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream,
                    ProtocolType.Tcp))
                {
                    int connectionAttempts = 0;
                    while (connectionAttempts < maxAttempts)
                    {
                        try
                        {
                            await sender.ConnectAsync(remoteEndPoint);
                            break;
                        }
                        catch (SocketException se)
                        {
                            Debug.LogError("Socket Exception occurred while trying to connect! " +
                                $"Error: {se.SocketErrorCode}. " +
                                $"Error Code: {se.ErrorCode}.");

                            if ((se.ErrorCode != 10061
                                && se.ErrorCode != 10050
                                && se.ErrorCode != 10057)
                                || maxAttempts == connectionAttempts)
                            {
                                // if error is not connection refused or has run out of attempts
                                Debug.LogError("Aborting...");

                                // try queueing again?
                                connectionAttempts = maxAttempts;
                                break;
                            }

                            // add polling
                            Debug.Log($"Tried {++connectionAttempts}/{maxAttempts} times. Retrying...");
                            await Task.Delay(100);
                            continue;
                        }
                        catch (ObjectDisposedException ode)
                        {
                            Debug.LogError($"The sender socket has been closed.\n{ode}\nAborting...");
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
                        connectionAttempts = maxAttempts;
                        break;
                    }

                    if (maxAttempts == connectionAttempts)
                    {
                        Debug.LogError("Failed to connect. Discarding data.");
                        continue;
                    }

                    try
                    {
                        await sender.SendAsync(ConvertToBuffer(data, chunkSize), SocketFlags.None);
                        Debug.Log($"Sent {data.Length} bytes to {sender.RemoteEndPoint} "+
                            $"at {DateTime.Now}.");
                    }
                    catch (SocketException se)
                    {
                        Debug.LogError($"Socket Error: {se.ErrorCode}");
                    }
                    catch (ObjectDisposedException ode)
                    {
                        Debug.LogError($"The socket has been closed.\n{ode}");
                    }
                }
            }

            _completedTransmission.Set();
        }

        /// <summary>
        /// Initialize data for the socket transmission.
        /// </summary>
        /// <param name="port">The port to send data over.</param>
        /// <param name="ipString">The string representing the IP address.</param>
        /// <param name="maxRetries">The maximum number of times to retry sending data
        /// after the connection has been refused.</param>
        public Sender(int port = 11000, string ipString = "localhost",
            int maxRetries = 3, int chunkSize = 50)
        {
            try
            {
                // Connect to a Remote server
                // Get Host IP Address that is used to establish a connection
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1
                // If a host has multiple addresses, you will get a list of addresses
                IPHostEntry host = Dns.GetHostEntry(ipString);
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, port);

                Task.Run(() => InitializeSender(ipAddress, remoteEndPoint, maxRetries, chunkSize));
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
            return _messageQueue.Enqueue(data);
        }

        /// <summary>
        /// Queue a closing message to the server and wait until it has been sent.
        /// </summary>
        public void FinishTransmissionsAndClose()
        {
            Debug.Log("Sending closing message.");
            string text_file = Newtonsoft.Json.JsonConvert.SerializeObject(new SerializableScene()
                        {
                            ContinueImporting = false
                        });
            _messageQueue.Enqueue(text_file);
            _messageQueue.Close();
            _completedTransmission.Wait();
            Debug.Log("Closed message queue. When queue is empty, the program will terminate.");
        }
    }
}
