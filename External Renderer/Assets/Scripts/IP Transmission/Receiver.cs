using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace ExternalUnityRendering
{
    public class Receiver
    {
        private IPHostEntry _host = Dns.GetHostEntry("localhost");
        private IPAddress _ipAddress;
        private IPEndPoint _localEndPoint;
        private Socket _listener;

        [Obsolete("To be replaced with Asynchronous Communication.")]
        private readonly byte endMarker = Convert.ToByte('\0');

        public Receiver(int port = 11000)
        {
            // Get Host IP Address that is used to establish a connection
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1
            // If a host has multiple addresses, you will get a list of addresses
            _ipAddress = _host.AddressList[0];
            _localEndPoint = new IPEndPoint(_ipAddress, port);

            try
            {
                // Create a Socket that will use Tcp protocol
                _listener = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                _listener.Bind(_localEndPoint);
                // Specify how many requests a Socket can listen before it gives Receiver busy response.
                // We will listen 1 request at a time
                _listener.Listen(1);

                Debug.Log("Waiting for a connection...");
            }
            catch (Exception e) // remove pokemon exception handle
            {
                Debug.LogError(e.ToString());
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey(true);
        }

        public void RecieveMessage(Action<string> dataReceivedCallback)
        {
            // byte cache for what is recieved
            byte[] bytes = new byte[1024];
            using (MemoryStream cache = new MemoryStream(1000))
            {
                try
                {
                    Socket handler = _listener.Accept();

                    // Incoming data from the client.
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

                    StringBuilder data = new StringBuilder();

                    var reader = new StreamReader(cache);
                    data.Append(reader.ReadToEnd());

                    dataReceivedCallback(data.ToString());

                    // Send status temporarily using "1"
                    byte[] msg = Encoding.ASCII.GetBytes("1");
                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
                catch (Exception e)
                {

                    Debug.LogError(e.ToString());
                }
            }
        }
    }
}
