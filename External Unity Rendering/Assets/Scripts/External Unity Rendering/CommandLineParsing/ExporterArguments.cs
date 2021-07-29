using System.Linq;
using System.Net;

using CommandLine;

using ExternalUnityRendering.PathManagement;

using UnityEngine;

namespace ExternalUnityRendering
{
    /// <summary>
    /// Class representing arguments for the exporter instance.
    /// </summary>
    [Verb("physics", aliases: new string[] { "exporter", "export" },
         HelpText = "Run the physics instance.")]
    public class ExporterArguments
    {
        private int _millisecondDelay = -1;
        private int _exportCount = -1;
        private int _totalExportTime = -1;
        private Vector2Int _resolution = Vector2Int.zero;
        private string _renderPath = null;
        private Exporter.PostExportAction _exportActions =
            Exporter.PostExportAction.Nothing;
        private string _jsonSavePath = null;
        private string _ipAddress = null;

        /// <summary>
        /// Whether the timing values provided are valid.
        /// </summary>
        public bool ValidTiming
        {
            get
            {
                if (_millisecondDelay > 10 && _totalExportTime > 10 && _exportCount > 0
                    && _millisecondDelay * _exportCount == _totalExportTime)
                {
                    return false;
                }

                return
                    (_millisecondDelay > 10 && _exportCount > 0) ||
                    (_totalExportTime > 10 && _exportCount > 0) ||
                    (_millisecondDelay > 10 && _totalExportTime > 10);
            }
        }

        /// <summary>
        /// Delay between exports in milliseconds. (Read Only)
        /// </summary>
        public int MillisecondsDelay
        {
            get
            {
                if (_millisecondDelay < 0)
                {
                    return Mathf.FloorToInt(_totalExportTime / _exportCount);
                }
                return _millisecondDelay;
            }
        }
        /// <summary>
        /// Delay between exports. (Write Only)
        /// </summary>
        [Option('d', "delay", HelpText = "The delay between exports of the scene.")]
        public string Delay
        {
            set
            {
                if (value.Length < 2)
                {
                    return;
                }

                char valueModifier = value[value.Length - 1];

                if (char.IsDigit(valueModifier))
                {
                    valueModifier = ' ';
                }
                else
                {
                    value = value.Remove(value.Length - 1);
                }

                int timeModifier;
                switch (valueModifier)
                {
                    case ' ':
                        timeModifier = 1;
                        break;
                    case 's':
                        timeModifier = 1000;
                        break;
                    case 'm':
                        timeModifier = 60 * 1000;
                        break;
                    default:
                        return;
                }

                if (int.TryParse(value, out int number) && number > 0)
                {
                    _millisecondDelay = number * timeModifier;
                }
            }
        }
        /// <summary>
        /// Total amount of time to spend delaying. Equal to
        /// <see cref="Exports"/> times <see cref="MillisecondsDelay"/>.
        /// </summary>
        [Option('s', "totalTime", HelpText = "The total time to export for.")]
        public string TotalExportTime
        {
            set
            {
                if (value.Length < 2)
                {
                    return;
                }

                char valueModifier = value[value.Length - 1];

                if (char.IsDigit(valueModifier))
                {
                    valueModifier = ' ';
                }
                else
                {
                    value = value.Remove(value.Length - 1);
                }

                int timeModifier;
                switch (valueModifier)
                {
                    case ' ':
                        timeModifier = 1;
                        break;
                    case 's':
                        timeModifier = 1000;
                        break;
                    case 'm':
                        timeModifier = 60 * 1000;
                        break;
                    default:
                        return;
                }

                if (int.TryParse(value, out int number) && number > 0)
                {
                    _totalExportTime = number * timeModifier;
                }
            }
        }
        /// <summary>
        /// Number of exports to make.
        /// </summary>
        [Option('e', "exportCount", HelpText = "The number of exports to make.")]
        public int Exports
        {
            get
            {
                if (_exportCount < 0)
                {
                    return Mathf.FloorToInt(_totalExportTime / _millisecondDelay);
                }
                return _exportCount;
            }
            set
            {
                if (value > 0)
                {
                    _exportCount = value;
                }
            }
        }

        /// <summary>
        /// Render Resolution to save for the importer. (Read Only)
        /// </summary>
        public Vector2Int RenderResolution
        {
            get
            {
                return _resolution;
            }
        }

        /// <summary>
        /// Height of the rendered image.
        /// </summary>
        [Option('h', "renderHeight", HelpText = "The height of the rendered image.")]
        public int RenderHeight
        {
            set
            {
                _resolution.y = value;
            }
        }

        /// <summary>
        /// Width of the rendered image.
        /// </summary>
        [Option('w', "renderWidth", HelpText = "The height of the rendered image.")]
        public int RenderWidth
        {
            set
            {
                _resolution.x = value;
            }
        }

        // TODO set _renderpath to a relative path, only check if valid name
        // TODO return absolute path if localhost, otherwise relative.
        /// <summary>
        /// Path to where renders will be saved.
        /// </summary>
        [Option('r', "renderPath", HelpText = "The path to render the images to.")]
        public string RenderPath
        {
            get
            {
                return _renderPath;
            }
            set
            {
                DirectoryManager pathValidator = new DirectoryManager(value);
                if (System.IO.Path.GetFullPath(value) == pathValidator.Path)
                {
                    _renderPath = pathValidator.Path;
                }
            }
        }

        /// <summary>
        /// What the exporter should do after serializing the scene.
        /// </summary>
        public Exporter.PostExportAction ExportActions
        {
            get
            {
                return _exportActions;
            }
        }

        [Option("writeToFile", HelpText = "The path to save json file states to.")]
        public string JsonPath
        {
            get
            {
                return _jsonSavePath ?? Application.persistentDataPath;
            }
            set
            {
                DirectoryManager pathValidator = new DirectoryManager(value);
                if (System.IO.Path.GetFullPath(value) == pathValidator.Path)
                {
                    _exportActions |= Exporter.PostExportAction.WriteToFile;
                    _jsonSavePath = pathValidator.Path;
                }
            }
        }

        /// <summary>
        /// Where json files should be saved.
        /// </summary>
        [Option('t', "transmit", HelpText = "Send data to a renderer instance using TCP/IP.",
            Default = false)]
        public bool Transmit
        {
            set
            {
                if (value)
                {
                    _exportActions |= Exporter.PostExportAction.Transmit;
                }
                else
                {
                    _exportActions &= ~Exporter.PostExportAction.Transmit;
                }
            }
        }

        /// <summary>
        /// Whether to write the serialised state to the console.
        /// </summary>
        [Option("logExport", HelpText = "Write the Json Scene State to Console.",
            Default = false)]
        public bool LogJson
        {
            set
            {
                if (value)
                {
                    _exportActions |= Exporter.PostExportAction.Log;
                }
                else
                {
                    _exportActions &= ~Exporter.PostExportAction.Log;
                }
            }
        }

        /// <summary>
        /// The port to transmit data to.
        /// </summary>
        [Option('p', "port", HelpText = "Port to connect to.", Default = (ushort)11000)]
        public ushort ReceiverPort { get; set; }

        /// <summary>
        /// The IP address that the exporter should transmit states to.
        /// </summary>
        [Option('i', "interface", HelpText = "IP Address to connect to.", Default = "localhost")]
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
    }
}
