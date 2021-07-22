using System.Linq;
using System.Net;
using CommandLine;

namespace ExternalUnityRendering
{
    // TODO
    // Add exclusive option for pointing to a folder and looping over the *.json contents
    // importing all
    [Verb("renderer", aliases: new string[] { "render" }, HelpText = "Receive data using TCP/IP or read from a list of json files.")]
    public class RendererArguments
    {
        private string _ipAddress = null;

        [Option('p', "port", HelpText = "Port for the renderer to listen on. If not specified," +
            "defaults to 11000", Default = (ushort)11000)]
        public ushort ReceiverPort { get; set; }
        [Option('i', "interface", HelpText = "IP Address to listen on.", Default = "localhost")]
        public string ReceiverIpAddress
        {
            get
            {
                return _ipAddress;
            }
            set
            {
                try
                {
                    IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                    if (Dns.GetHostAddresses(value).Any((hostIP) =>
                        localIPs.Any((localIP) =>
                            hostIP.Equals(localIP)) || IPAddress.IsLoopback(hostIP)))
                    {
                        _ipAddress = value;
                    }
                }
                catch
                {
                    // Do nothing
                }
            }
        }
    }
}
