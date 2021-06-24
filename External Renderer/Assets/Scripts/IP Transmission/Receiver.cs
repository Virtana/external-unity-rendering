using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExternalUnityRendering.TcpIp
{
    public class Receiver
    {
        private IPHostEntry _host = Dns.GetHostEntry("localhost");
        private IPAddress _ipAddress;
        private IPEndPoint _localEndPoint;
        private Socket _listener;

        // TODO dispatch multiple listeners to collect data
        // Each listener would write received data into a system.collections.concurrent
        // maybe and have receivemessage check if there is data ready and run the callback
        // There is a chance Accept may act wonky

        public Receiver(int port = 11000)
        {
            try
            {
                // Get Host IP Address that is used to establish a connection
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1
                // If a host has multiple addresses, you will get a list of addresses
                _ipAddress = _host.AddressList[0];
                _localEndPoint = new IPEndPoint(_ipAddress, port);
                // Create a Socket that will use Tcp protocol
                _listener = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                _listener.Bind(_localEndPoint);
                // Specify how many requests a Socket can listen before it gives Receiver busy response.
                // We will listen 1 request at a time
                _listener.Listen(1);

                Debug.Log("Waiting for a connection...");
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

        public async void ReceiveMessage(Action<string> dataReceivedCallback)
        {
            string data = "";
            while (true)
            {
                data =
                    await Task.Run(() =>
                    {
                        // byte cache for what is recieved
                        byte[] bytes = new byte[1024];
                        using (MemoryStream cache = new MemoryStream())
                        {
                            try
                            {
                                Socket handler = _listener.Accept();

                                while (true)
                                {
                                    int bytesReceived = handler.Receive(bytes);
                                    cache.Write(bytes, 0, bytesReceived);

                                    // check if the client disconnected
                                    if (handler.Poll(1, SelectMode.SelectRead) && handler.Available == 0)
                                    {
                                        break;
                                    }
                                }

                                cache.Seek(0, SeekOrigin.Begin);

                                StringBuilder stringBuilder = new StringBuilder();

                                using (StreamReader reader = new StreamReader(cache))
                                {
                                    stringBuilder.Append(reader.ReadToEnd());
                                }

                                handler.Close();
                                return stringBuilder.ToString();
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
                                Debug.LogError("An error has occurred that prevents writing to the "+
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

                            return "";
                        }
                    });
                dataReceivedCallback(data);
            }
        }
    }
}
