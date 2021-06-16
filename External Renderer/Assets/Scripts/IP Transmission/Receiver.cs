using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SceneStateExporter
{
    public class Receiver
    {
    // https://www.c-sharpcorner.com/article/socket-programming-in-C-Sharp/
        private IPHostEntry _host = Dns.GetHostEntry("localhost");
        private IPAddress _ipAddress;
        private IPEndPoint _localEndPoint;
        private Socket _listener;

        public Receiver(Action<string> onDataReceived, int port = 11000)
        {
            // Get Host IP Address that is used to establish a connection
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1
            // If a host has multiple addresses, you will get a list of addresses
            _ipAddress = _host.AddressList[0];
            _localEndPoint = new IPEndPoint(_ipAddress, port);

            try {
                // Create a Socket that will use Tcp protocol
                _listener = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                _listener.Bind(_localEndPoint);
                // Specify how many requests a Socket can listen before it gives Receiver busy response.
                // We will listen 1 request at a time
                _listener.Listen(1);

                Console.WriteLine("Waiting for a connection...");

                RecieveMessages(onDataReceived);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey(true);
        }

        private void RecieveMessages(Action<string> _dataReceivedCallback)
        {
            try
            {
                Socket handler = _listener.Accept();

                // Incoming data from the client.
                StringBuilder data = new StringBuilder();
                byte[] bytes = null;
                string temp = null;
                while (true)
                {
                    bytes = new byte[1024];
                    int bytesReceived = handler.Receive(bytes);
                    temp = Encoding.ASCII.GetString(bytes, 0, bytesReceived);
                    if (temp.IndexOf('\0') > -1)
                    {
                        data.Append(temp.Split('\0', 1)[0]);
                        break;
                    }
                }

                _dataReceivedCallback(data.ToString());

                byte[] msg = Encoding.ASCII.GetBytes("1");
                handler.Send(msg);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
