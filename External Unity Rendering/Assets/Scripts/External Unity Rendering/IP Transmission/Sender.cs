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
        private struct SendState
        {
            public SocketError errorCode;
            public Socket socket;
            public List<ArraySegment<byte>> data;
            public SocketFlags flags;
        }

        private readonly Channel<string> _dataToSend =
            Channel.CreateBounded<string>(new BoundedChannelOptions(10) {
                SingleReader = true,
                SingleWriter = true
            });
        private readonly IPHostEntry _host;
        private readonly IPAddress _ipAddress;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly int _maxAttempts;
        private readonly int _chunkSize = 50;
        private readonly Mutex _awaitCompletion = new Mutex(true);

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

        private async void InitializeSender()
        {
            while (await _dataToSend.Reader.WaitToReadAsync())
            {
                // add fail check
                _dataToSend.Reader.TryRead(out string item);

                // Create a TCP/IP socket.
                Socket sender = null;
                try
                {
                    sender = new Socket(_ipAddress.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);
                }
                catch (SocketException se)
                {
                    Debug.Log($"The socket was not able to be created as the combination of addressFamily, " +
                        $"socketType, and protocolType results in an invalid socket.\n{se}");
                }

                if (sender == null)
                {
                    Debug.LogError("Cannot send data. No socket was assigned during initializaiton. " +
                        "An error may have occured. Try reassigning the socket.");
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

                if (_dataToSend.Reader.CanCount)
                {
                    Debug.Log($"{_dataToSend.Reader.Count} items left in the queue.");
                }
            }

            Debug.Log("Completing...");
            _awaitCompletion.ReleaseMutex();
        }

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

        public bool QueueSend(string data)
        {
            return _dataToSend.Writer.TryWrite(data);
        }

        public void FinishTransmissionsAndClose()
        {
            Debug.Log("Setting queue to closed.");
            _dataToSend.Writer.Complete();
            _awaitCompletion.WaitOne();
            Debug.Log("Closed queue. When queue is empty, the program will terminate.");
        }
    }
}
