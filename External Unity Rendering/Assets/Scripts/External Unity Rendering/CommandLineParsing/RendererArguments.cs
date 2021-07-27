using System.Linq;
using System.Net;
using CommandLine;
using ExternalUnityRendering.PathManagement;

namespace ExternalUnityRendering
{
    // TODO
    // Add exclusive option for pointing to a folder and looping over the *.json contents
    // importing all
    [Verb("renderer", aliases: new string[] { "render" }, HelpText = "Receive data using TCP/IP or read from a list of json files.")]
    public class RendererArguments
    {
        private string _ipAddress = null;
        private DirectoryManager _renderDirectory = null;

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
                    // if IP is valid local IP or remote IP and save it
                    IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                    if ((Dns.GetHostAddresses(value).Any((hostIP) =>
                            localIPs.Any((localIP) =>
                                hostIP.Equals(localIP)) || IPAddress.IsLoopback(hostIP)))
                        || (!string.IsNullOrWhiteSpace(value) && value.Count(c => c == '.') == 3 &&
                        IPAddress.TryParse(value, out IPAddress _)))
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

        [Option('r', "renderPath", HelpText = "The path to render the images to.")]
        public string RenderPath
        {
            set
            {
                DirectoryManager pathValidator = new DirectoryManager(value);
                if (System.IO.Path.GetFullPath(value) == pathValidator.Path)
                {
                    _renderDirectory = pathValidator;
                }
            }
        }

        public DirectoryManager RenderDirectory
        {
            get
            {
                return _renderDirectory;
            }
        }
    }
}
