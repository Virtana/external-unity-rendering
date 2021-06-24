using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace ExternalUnityRendering.TcpIp
{
    public class Sender
    {
        private readonly IPHostEntry _host;
        private readonly IPAddress _ipAddress;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly Socket _sender;
        private readonly int _maxRetries;

        private readonly int _chunkSize = 50;

        // Helper function to chunk data for sending
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

        public Sender(int port = 11000, string ipString = "localhost", int maxRetries = 3)
        {
            try
            {
                _maxRetries = maxRetries;
                // Connect to a Remote server
                // Get Host IP Address that is used to establish a connection
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1
                // If a host has multiple addresses, you will get a list of addresses
                _ipAddress = _host.AddressList[0];
                _remoteEndPoint = new IPEndPoint(_ipAddress, port);

                // Create a TCP/IP  socket.
                _sender = new Socket(_ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        public void Send(string data) {
            byte[] bytes = new byte[1024];
            // Connect the socket to the remote endpoint. Catch any errors.
            try
            {
                // Connect to Remote EndPoint
                _sender.Connect(_remoteEndPoint);

                Debug.LogFormat("Socket connected to {0}",
                    _sender.RemoteEndPoint.ToString());

                // Encode the data string into a byte array.
                byte[] msg = Encoding.ASCII.GetBytes($"{data}");

                // Send the data through the socket.
                int bytesSent = _sender.Send(msg);

                // HACK disconnect then reconnect to signal end of transmission
                _sender.Shutdown(SocketShutdown.Both);
                _sender.Disconnect(true);
                _sender.Connect(_remoteEndPoint);

                // Receive the response from the remote device.
                // may be problematic after disconnect
                int bytesRec = _sender.Receive(bytes);
                if (Encoding.ASCII.GetString(bytes, 0, bytesRec) != "1")
                {
                    Debug.LogError("an error occured.");
                }

                // Release the socket.
                _sender.Shutdown(SocketShutdown.Both);
                _sender.Close();

            }
            catch (ArgumentNullException ane)
            {
                Debug.LogErrorFormat("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Debug.LogErrorFormat("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Unexpected exception : {0}", e.ToString());
            }
        }

        // TODO add struct with callback for handling responses.
        private struct SendState
        {
            public SocketError errorCode;
            public Socket socket;
            public List<ArraySegment<byte>> data;
            public SocketFlags flags;
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
                Debug.LogWarning("Transmission is not completed. Data may not be " +
                    "handled correctly.");
            }

            Socket socket = state.socket;
            socket.EndSend(result);
            socket.Close();
        }

        public void SendAsync(string data)
        {
            if (_sender == null)
            {
                Debug.LogError("Cannot send data. No socket was assigned during initializaiton. " +
                    "An error may have occured. Try reassigning the socket.");
                return;
            }

            int retryCount = 0;
            while (retryCount < _maxRetries)
            {
                try
                {
                    _sender.Connect(_remoteEndPoint);
                    break;
                }
                catch (SocketException se)
                {
                    Debug.LogError("Socket Exception occurred while trying to connect! " +
                        $"Retrying {++retryCount}/{_maxRetries}. " +
                        $"Error: {se.SocketErrorCode}. " +
                        $"Error Code: {se.ErrorCode}.");
                    if (se.ErrorCode != 10061) // if not ConnectionRefused quit
                    {
                        Debug.LogError("Aborting...");
                        return;
                    }                    
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
                    socket = _sender,
                    data = ConvertToBuffer(data),
                    flags = SocketFlags.None
                };

            try
            {
                _sender.BeginSend(state.data, state.flags, out state.errorCode, SendDataCallback, state);
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
    }
}
