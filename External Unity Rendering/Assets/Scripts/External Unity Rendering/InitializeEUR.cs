using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using UnityEngine;

namespace ExternalUnityRendering
{
    /// <summary>
    /// Class which parses command line arguments and initializes the exporter or renderer
    /// instances.
    /// </summary>
    public class InitializeEUR
    {
        /// <summary>
        /// All the error types which the command line parserr may generate. Used for creating
        /// error messages.
        /// </summary>
        private static readonly Type[] s_parserErrors =
            new Type[]
            {
                typeof(BadFormatConversionError), typeof(BadFormatTokenError),
                typeof(BadVerbSelectedError), typeof(HelpRequestedError),
                typeof(HelpVerbRequestedError) , typeof(InvalidAttributeConfigurationError),
                typeof(MissingRequiredOptionError), typeof(MissingValueOptionError),
                typeof(MutuallyExclusiveSetError), typeof(NamedError), typeof(NoVerbSelectedError),
                typeof(RepeatedOptionError), typeof(SequenceOutOfRangeError),
                typeof(SetValueExceptionError), typeof(TokenError), typeof(UnknownOptionError),
                typeof(VersionRequestedError),
            };

        /// <summary>
        /// List of unity arguments and the number of parameters they take. Used to filter out
        /// the unity player's arguments before parsing.
        /// </summary>
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

        /// <summary>
        /// Dictionary of fail functions and error messages for the renderer. If any of the
        /// functions return true, then the application should exit.
        /// </summary>
        private static readonly Dictionary<Func<ImporterArguments, bool>, string>
            s_rendererFailConditions = new Dictionary<Func<ImporterArguments, bool>, string>
            {
                { (_) => !Application.isBatchMode, "Renderer should only be run in batchmode" },
                { (args) => args.ReceiverIpAddress == null, "Invalid IP address provided" }
            };

        /// <summary>
        /// Dictionary of fail functions and error messages for the exporter. If any of the
        /// functions return true, then the application should exit.
        /// </summary>
        private static readonly Dictionary<Func<ExporterArguments, bool>, string>
            s_exporterFailConditions = new Dictionary<Func<ExporterArguments, bool>, string>
            {
                { (args) => !args.ValidTiming, "Automatic exporter timings provided." },
                { (args) => args.ReceiverIpAddress == null, "Invalid IP address provided." }
            };

        /// <summary>
        /// Removes the unity args and returns the arguments which should be passed to the
        /// renderer/exporter.
        /// </summary>
        /// <param name="args">The array of arguments passed to the program.</param>
        /// <param name="filteredArgs">The arguments to be passed to the renderer.</param>
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

        /// <summary>
        /// Process command line arguments and launch the appropriate mode or exit. Only runs in
        /// Standalone builds.
        /// </summary>
#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void Initialize()
        {
            string[] commandLineArguments = Environment.GetCommandLineArgs();
            FilterArgs(commandLineArguments, out List<string> args);

            Parser parser = new Parser(with => with.HelpWriter = Console.Out);

            ParserResult<object> result =
                parser.ParseArguments<ExporterArguments, ImporterArguments>(args);

            result
                .WithParsed<ExporterArguments>(Start)
                .WithParsed<ImporterArguments>(Start)
                .WithNotParsed((errors) =>
                {
                    StringBuilder sb = new StringBuilder();

                    // For all the errors, match the error with its type and using reflection, get
                    // its properties
                    foreach(Error error in errors)
                    {
                        foreach (Type errorType in s_parserErrors)
                        {
                            if (error.GetType() == errorType)
                            {
                                sb.AppendLine($"Error: {error}");
                                foreach (var property in errorType.GetProperties())
                                {
                                    sb.AppendLine($"{property.Name}: {property.GetValue(error)}");
                                }
                                sb.AppendLine();
                                break;
                            }
                        }
                    }

                    Debug.LogError(sb.ToString());
                    Application.Quit(1);
                });
        }

        /// <summary>
        /// Using the provided renderer arguments, add a <see cref="ImporterManager"/> to the
        /// scene.
        /// </summary>
        /// <param name="args">The parsed renderer arguments.</param>
        private static void Start(ImporterArguments args)
        {
            foreach (KeyValuePair<Func<ImporterArguments, bool>, string> kv
                in s_rendererFailConditions)
            {
                if (kv.Key(args))
                {
                    Debug.LogError(kv.Value);
                    Application.Quit(1);
                }
            }

            ImporterManager rendererManager =
                UnityEngine.Object.FindObjectOfType<ImporterManager>();
            if (rendererManager == null)
            {
                GameObject manager = new GameObject
                {
                    name = nameof(ImporterManager)
                };
                rendererManager = manager.AddComponent<ImporterManager>();
            }
            rendererManager.Arguments = args;
            Debug.Log("Initialized Renderer Import Manager.");
        }

        /// <summary>
        /// Using the provided exporter arguments, add a <see cref="ExporterManager"/> to the
        /// scene.
        /// </summary>
        /// <param name="args">The parsed exporter arguments.</param>
        private static void Start(ExporterArguments args)
        {
            foreach (KeyValuePair<Func<ExporterArguments, bool>, string> kv
                in s_exporterFailConditions)
            {
                if (kv.Key(args))
                {
                    Debug.LogError(kv.Value);
                    Application.Quit(1);
                }
            }

            ExporterManager exportManager =
                UnityEngine.Object.FindObjectOfType<ExporterManager>();
            if (exportManager == null)
            {
                GameObject manager = new GameObject
                {
                    name = nameof(ExporterManager)
                };
                exportManager = manager.AddComponent<ExporterManager>();
            }
            exportManager.Arguments = args;
            Debug.Log("Initialized Physics Export Manager.");
        }
    }
}