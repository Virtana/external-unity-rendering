using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

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
                Debug.LogError(e.ToString());
            }
        }

        // TODO: investigate how to handle send issues
        // Investigate what happens if byte array is too small
        // switch byte array to more IList<ArraySegment<byte>> 
        // and using socket error code
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
    }
}
