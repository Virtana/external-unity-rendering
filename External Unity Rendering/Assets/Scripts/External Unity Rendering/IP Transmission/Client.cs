using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ExternalUnityRendering.Serialization;

using UnityEngine;

namespace ExternalUnityRendering.TcpIp
{
    /// <summary>
    /// Class that manages socket transmission.
    /// </summary>
    public class Client
    {
        /// <summary>
        /// Internal queue of data to be sent. Used to transmit messages in a non blocking manner.
        /// </summary>
        private readonly AwaitableConcurrentQueue<string> _messageQueue =
            new AwaitableConcurrentQueue<string>();

        /// <summary>
        /// Event internally used to signal when all the data has been sent.
        /// </summary>
        private readonly ManualResetEventSlim _completedTransmission =
            new ManualResetEventSlim(false);

        /// <summary>
        /// Get whether all the messages have been sent.
        /// </summary>
        /// <returns>Whether all the messages have been sent.</returns>
        public bool IsDone
        {
            get
            {
                return _completedTransmission.IsSet;
            }
        }

        /// <summary>
        /// Helper function to split a string into chunks of bytes.
        /// </summary>
        /// <param name="data">The string to be converted.</param>
        /// <returns>A list of array segments to be used during transmission.</returns>
        private List<ArraySegment<byte>> ConvertToBuffer(string data, int chunkSize)
        {
            byte[] dataAsBytes = Encoding.UTF8.GetBytes(data);

            List<ArraySegment<byte>> buffer = new List<ArraySegment<byte>>();

            for (int i = 0; i < dataAsBytes.Length; i += chunkSize)
            {
                buffer.Add(new ArraySegment<byte>(
                    dataAsBytes, i, Math.Min(chunkSize, dataAsBytes.Length - i)));
            }

            return buffer;
        }

        /// <summary>
        /// Read messages from the queue and transmit them to <paramref name="remoteEndPoint"/>.
        /// </summary>
        /// <param name="remoteEndPoint">The remote endpoint to send data to.</param>
        /// <param name="maxAttempts">The max number of attempts to retry connection after being
        /// rejected. </param>
        /// <param name="chunkSize">The size of each chunk of data to send.</param>
        private async void SendAsync(IPEndPoint remoteEndPoint, int maxAttempts, int chunkSize)
        {
            Debug.Log("Opening message queue.");

            using (Socket pinger = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream,
                    ProtocolType.Tcp))
            {
                while (!pinger.Connected)
                {
                    try
                    {
                        pinger.Connect(remoteEndPoint);
                        await Task.Delay(100);
                    }
                    catch (SocketException se)
                    {
                        if (se.ErrorCode != 10050 && se.ErrorCode != 10061)
                        {
                            Debug.LogError($"While waiting for server to come online, received: " +
                                $"{se.SocketErrorCode} {se.ErrorCode}");
                            Application.Quit(1);
                        }
                    }
                }
            }

            Debug.Log($"Connected to {remoteEndPoint} at {DateTime.Now}");

            while (_messageQueue.QueueComplete)
            {
                (bool readSuccess, string data) = await _messageQueue.DequeueAsync();

                if (!readSuccess)
                {
                    Debug.Log("Failed to read from queue.");
                    continue;
                }
                using (Socket sender = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream,
                    ProtocolType.Tcp))
                {
                    bool connectSuccess = false;
                    for (int tries = 0; tries < maxAttempts; tries++)
                    {
                        try
                        {
                            await sender.ConnectAsync(remoteEndPoint);
                            connectSuccess = true;
                            break;
                        }
                        catch (SocketException se)
                        {
                            Debug.LogError("Socket Exception occurred while trying to connect! " +
                                $"Error: {se.SocketErrorCode}. " +
                                $"Error Code: {se.ErrorCode}.");

                            if (se.ErrorCode != 10061 && se.ErrorCode != 10050
                                && se.ErrorCode != 10057)
                            {
                                // if error is not connection refused or has run out of attempts
                                Debug.LogError("Aborting...");
                                break;
                            }

                            // add polling
                            Debug.Log($"Tried {tries + 1}/{maxAttempts} times. " +
                                "Retrying...");
                            await Task.Delay(100);
                            continue;
                        }
                        catch (ObjectDisposedException ode)
                        {
                            Debug.LogError($"The sender socket has been closed.\n{ode}\n" +
                                "Aborting...");
                        }
                        catch (System.Security.SecurityException se)
                        {
                            Debug.LogError("A caller higher in the call stack does not have " +
                                $"permission for the requested operation.\n{se}");
                        }
                        catch (InvalidOperationException ioe)
                        {
                            Debug.LogError("The socket has been placed in a listening state by " +
                                $"calling Listen(Int32).\n{ioe}");
                        }
                        break;
                    }

                    if (!connectSuccess)
                    {
                        Debug.LogError("Failed to connect. Discarding data.");
                        continue;
                    }

                    try
                    {
                        await sender.SendAsync(ConvertToBuffer(data, chunkSize), SocketFlags.None);
                        Debug.Log($"Sent {data.Length} bytes to {sender.RemoteEndPoint} " +
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
        /// Create a client to manage sending data to a server over a socket.
        /// </summary>
        /// <param name="port">The port to send data to.</param>
        /// <param name="ipString">The IP address to send data to.</param>
        /// <param name="maxRetries">The maximum number of times to retry sending data
        /// after the connection has been refused.</param>
        public Client(int port, string ipAddr, int maxRetries = 3, int chunkSize = 1024)
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
                IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, port);

                Task.Run(() => SendAsync(remoteEndPoint, maxRetries, chunkSize));
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
        /// <param name="data">String to be sent.</param>
        /// <returns>Wheter the data was successfully enqueued.</returns>
        public bool Send(string data)
        {
            return _messageQueue.Enqueue(data);
        }

        /// <summary>
        /// Wait for all messages to finish sending.
        /// </summary>
        public void Close()
        {
            Debug.Log("Sending closing message.");
            _messageQueue.Enqueue(EURScene.ClosingMessage);
            _messageQueue.Close();
            Debug.Log("Closed message queue. When queue is empty, the program will terminate.");
            _completedTransmission.Wait();
        }

        /// <summary>
        /// Wait for all messages to finish sending.
        /// </summary>
        /// <returns>A <see cref="Task"/> that resolves when all the data has finished sending.
        /// </returns>
        public async Task CloseAsync()
        {
            Debug.Log("Sending closing message.");
            _messageQueue.Enqueue(EURScene.ClosingMessage);
            _messageQueue.Close();
            Debug.Log("Closed message queue. When queue is empty, the program will terminate.");
            await Task.Run(_completedTransmission.Wait);
        }
    }
}
