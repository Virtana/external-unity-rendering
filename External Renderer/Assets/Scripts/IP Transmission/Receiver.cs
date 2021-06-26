using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace ExternalUnityRendering.TcpIp
{
    public class Receiver
    {
        /// <summary>
        /// The host that this receiver should listen on.
        /// </summary>
        private readonly IPHostEntry _host;

        /// <summary>
        /// The IP address that this receiver should listen on.
        /// </summary>
        private readonly IPAddress _ipAddress;

        /// <summary>
        /// The endpoint that this receiver should bind to and listen on.
        /// </summary>
        private readonly IPEndPoint _localEndPoint;

        /// <summary>
        /// The socket that will listen and accept connections.
        /// </summary>
        private readonly Socket _listener;

        // TODO dispatch multiple listeners to collect data
        // Each listener would write received data into a system.collections.concurrent
        // maybe and have receivemessage check if there is data ready and run the callback
        // hopefully won't die.

        /// <summary>
        /// Initialise a receiver and bind and listen on the socket.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="ipAddr">The IP address to listen on.</param>
        public Receiver(int port = 11000, string ipAddr = "localhost")
        {
            try
            {
                // Get Host IP Address that is used to establish a connection
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1
                // If a host has multiple addresses, you will get a list of addresses
                _host = Dns.GetHostEntry(ipAddr);
                _ipAddress = _host.AddressList[0];
                _localEndPoint = new IPEndPoint(_ipAddress, port);
                // Create a Socket that will use Tcp protocol
                _listener = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                _listener.Bind(_localEndPoint);
                _listener.Listen(5);

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

        /// <summary>
        /// Begin receiving data asynchronously.
        /// </summary>
        /// <param name="dataReceivedCallback">Func to be called each time data is received.
        /// Returns whether the system should continue receiving data.</param>
        public async void ReceiveMessages(Func<string, bool> dataReceivedCallback)
        {
            string data = "";
            // byte cache for what is recieved
            byte[] bytes = new byte[1024];
            
            bool continueReceiving = true;
            using (MemoryStream cache = new MemoryStream())
            {
                while (continueReceiving)
                {
                    Socket handler = await _listener.AcceptAsync();
                    bool success = await Task.Run(() =>
                    {
                        try
                        {
                            do
                            {
                                // Incoming data from the client.
                                int bytesReceived = handler.Receive(bytes);
                                cache.Write(bytes, 0, bytesReceived);

                            // until the client disconnects
                            } while (!(handler.Poll(1, SelectMode.SelectRead) && handler.Available == 0));

                            handler.Close();

                            cache.Seek(0, SeekOrigin.Begin);

                            using (StreamReader reader = new StreamReader(cache))
                            {
                                data = reader.ReadToEnd();
                            }

                            return true;
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

                        return false;
                    });
                    if (success)
                    {
                        continueReceiving = dataReceivedCallback(data);
                    }
                }
            }

            // Exit the application in runtime, stop playing in editor.
            // If server is off, no point in render instance.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
