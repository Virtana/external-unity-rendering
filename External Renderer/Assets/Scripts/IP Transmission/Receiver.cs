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
                Debug.Log(e.ToString());
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

                                StringBuilder stringBuilder = new StringBuilder();

                                using (StreamReader reader = new StreamReader(cache))
                                {
                                    stringBuilder.Append(reader.ReadToEnd());
                                }

                                // TODO implement responses
                                // Send status temporarily using "1"
                                // byte[] msg = Encoding.ASCII.GetBytes("1");
                                // handler.Send(msg);
                                handler.Close();
                                return stringBuilder.ToString();
                            }
                            catch (Exception e)
                            {
                                Debug.Log(e.ToString());
                                return "";
                            }
                        }
                    });
                dataReceivedCallback(data);
            }
        }
    }
}
