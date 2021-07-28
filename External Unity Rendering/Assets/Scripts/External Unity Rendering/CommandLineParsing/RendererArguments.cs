using System.Linq;
using System.Net;
using CommandLine;
using ExternalUnityRendering.PathManagement;

namespace ExternalUnityRendering
{
    // TODO Add option for importing json files from a folder
    /// <summary>
    /// Class storing arguments for the renderer.
    /// </summary>
    [Verb("renderer", aliases: new string[] { "render" },
        HelpText = "Receive data using TCP/IP or read from a list of json files.")]
    public class RendererArguments
    {
        private string _ipAddress = null;
        private DirectoryManager _renderDirectory = null;

        /// <summary>
        /// The port the renderer should listen on for incoming messages.
        /// </summary>
        [Option('p', "port", HelpText = "Port for the renderer to listen on. If not specified," +
            "defaults to 11000", Default = (ushort)11000)]
        public ushort ReceiverPort { get; set; }

        /// <summary>
        /// The address the renderer should listen on incoming messages.
        /// </summary>
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

        // TODO if !localhost, log a warning if not specified
        /// <summary>
        /// The Path where the renderer should save renders to. Overrides the path in the json file.
        /// </summary>
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

        /// <summary>
        /// A <see cref="DirectoryManager"/> for <see cref="RenderPath"/>
        /// </summary>
        public DirectoryManager RenderDirectory
        {
            get
            {
                return _renderDirectory;
            }
        }
    }
}
