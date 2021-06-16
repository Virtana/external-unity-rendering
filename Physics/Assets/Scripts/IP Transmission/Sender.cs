using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SceneStateExporter
{
    public class Sender
    {
        private IPHostEntry _host = Dns.GetHostEntry("localhost");
        private IPAddress _ipAddress;
        private IPEndPoint _remoteEndPoint;
        private Socket _sender;

        public Sender(int port = 11000)
        {
            try
            {
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
                Console.WriteLine(e.ToString());
            }
        }

        public void Send(string data) {
            byte[] bytes = new byte[1024];
            // Connect the socket to the remote endpoint. Catch any errors.
            try
            {
                // Connect to Remote EndPoint
                _sender.Connect(_remoteEndPoint);

                Console.WriteLine("Socket connected to {0}",
                    _sender.RemoteEndPoint.ToString());

                // Encode the data string into a byte array.
                byte[] msg = Encoding.ASCII.GetBytes($"{data}\0");

                // Send the data through the socket.
                int bytesSent = _sender.Send(msg);

                // Receive the response from the remote device.
                int bytesRec = _sender.Receive(bytes);
                if (Encoding.ASCII.GetString(bytes, 0, bytesRec)
                    != "1")
                {
                    Console.WriteLine("an error occured.");
                }

                // Release the socket.
                _sender.Shutdown(SocketShutdown.Both);
                _sender.Close();

            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
        }
    }
}
