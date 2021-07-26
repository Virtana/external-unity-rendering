using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using UnityEngine;

namespace ExternalUnityRendering
{
    public class InitializeEUR
    {
        private static readonly Dictionary<string, int> s_unityStandaloneArgs =
                new Dictionary<string, int>
                {
                    { "batchmode", 0 }, { "disable-gpu-skinning", 0 }, { "force-d3d11", 0 },
                    { "force-d3d11-singlethreaded", 0 }, { "force-d3d12", 0 },
                    { "force-metal", 0 }, { "force-glcore", 0 }, { "force-glcoreXY", 0 },
                    { "force-vulkan", 0 }, { "force-clamped", 0 }, { "force-low-power-device", 0 },
                    { "force-wayland", 0 }, { "nographics", 0 }, { "nolog", 0 },
                    { "no-stereo-rendering", 0 }, { "popupwindow", 0 }, { "screen-fullscreen", 0 },
                    { "screen-height", 0 }, { "screen-width", 0 }, { "screen-quality", 0 },
                    { "single-instance", 0 }, { "window-mode", 0 }, { "force-device-index", 1 },
                    { "parentHWND", 2 }, { "vrmode", 1 }, { "monitor", 1 }, { "logFile", 1},
                    { "quit", 0 }
                };

        private static readonly Dictionary<Func<RendererArguments, bool>, string> s_rendererFailConditions =
            new Dictionary<Func<RendererArguments, bool>, string>
            {
                { (_) => !Application.isBatchMode, "Renderer should only be run in batchmode" },
                { (args) => args.ReceiverIpAddress == null, "Invalid IP address provided" }
            };

        private static readonly Dictionary<Func<PhysicsArguments, bool>, string> s_exporterFailConditions =
            new Dictionary<Func<PhysicsArguments, bool>, string>
            {
                { (args) => !args.ValidTiming, "Automatic exporter timings provided." },
                { (args) => args.ReceiverIpAddress == null, "Invalid IP address provided." }
            };

        private static void FilterArgs(string[] args, out List<string> filteredArgs)
        {
            filteredArgs = new List<string>();

            for (int i = 1; i < args.Length; i++)
            {
                // if the current arg is a unity argument
                if (s_unityStandaloneArgs.TryGetValue(
                    args[i].Substring(1), out int parameters))
                {
                    // get the number of arguments it takes and advance
                    // by that count (to skip the parameter and args
                    i += parameters;
                } else
                {
                    // otherwise add it to the list
                    filteredArgs.Add(args[i]);
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            string[] commandLineArguments = Environment.GetCommandLineArgs();
            FilterArgs(commandLineArguments, out List<string> args);

            Parser parser = new Parser(with => with.HelpWriter = Console.Out);

            var result =
                parser.ParseArguments<PhysicsArguments, RendererArguments>(args);
            result
                .WithParsed<PhysicsArguments>(Start)
                .WithParsed<RendererArguments>(Start)
                .WithNotParsed((errors) =>
                {

                    Type[] errorTypes =
                        new Type[]
                        {
                            typeof(BadFormatConversionError),
                            typeof(BadFormatTokenError),
                            typeof(BadVerbSelectedError),
                            typeof(HelpRequestedError),
                            typeof(HelpVerbRequestedError),
                            typeof(InvalidAttributeConfigurationError),
                            typeof(MissingRequiredOptionError),
                            typeof(MissingValueOptionError),
                            typeof(MutuallyExclusiveSetError),
                            typeof(NamedError),
                            typeof(NoVerbSelectedError),
                            typeof(RepeatedOptionError),
                            typeof(SequenceOutOfRangeError),
                            typeof(SetValueExceptionError),
                            typeof(TokenError),
                            typeof(UnknownOptionError),
                            typeof(VersionRequestedError)
                        };

                    StringBuilder sb = new StringBuilder();

                    foreach(Error error in errors)
                    {
                        foreach (Type errorType in errorTypes)
                        {
                            if (error.GetType() == errorType)
                            {
                                sb.AppendLine($"Error: {error}");
                                foreach (var property in errorType.GetProperties())
                                {
                                    sb.AppendLine($"{property.Name}: {property.GetValue(error)}");
                                }
                                sb.AppendLine();
                            }
                        }
                    }

                    Debug.LogError(sb.ToString());
                    Application.Quit(1);
                });
        }

        private static void Start(RendererArguments args)
        {
            foreach (KeyValuePair<Func<RendererArguments, bool>, string> kv
                in s_rendererFailConditions)
            {
                if (kv.Key(args))
                {
                    Debug.LogError(kv.Value);
                    Application.Quit(1);
                }
            }

            RendererImportManager rendererManager =
                UnityEngine.Object.FindObjectOfType<RendererImportManager>();
            if (rendererManager == null)
            {
                GameObject manager = new GameObject
                {
                    name = nameof(RendererImportManager)
                };
                rendererManager = manager.AddComponent<RendererImportManager>();
            }
            rendererManager.Arguments = args;
            Debug.Log("Initialized Renderer Import Manager.");
        }

        private static void Start(PhysicsArguments args)
        {
            foreach (KeyValuePair<Func<PhysicsArguments, bool>, string> kv
                in s_exporterFailConditions)
            {
                if (kv.Key(args))
                {
                    Debug.LogError(kv.Value);
                    Application.Quit(1);
                }
            }

            PhysicsExportManager physicsManager =
                UnityEngine.Object.FindObjectOfType<PhysicsExportManager>();
            if (physicsManager == null)
            {
                GameObject manager = new GameObject
                {
                    name = nameof(PhysicsExportManager)
                };
                physicsManager = manager.AddComponent<PhysicsExportManager>();
            }
            physicsManager.Arguments = args;
            Debug.Log("Initialized Physics Export Manager.");
        }
    }
}