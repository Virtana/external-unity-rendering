using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using ExternalUnityRendering.PathManagement;
using UnityEngine;

namespace ExternalUnityRendering
{
    public class PhysicsExportManager : MonoBehaviour
    {
        private class RuntimeArgs
        {
            private int _millisecondDelay = -1;
            private int _exportCount = -1;
            private int _totalExportTime = -1;
            private Vector2Int _resolution = Vector2Int.zero;
            private string _renderPath = "";
            private ExportScene.PostExportAction _exportActions =
                ExportScene.PostExportAction.Nothing;
            private string _jsonSavePath = null;

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

            public Vector2Int RenderResolution
            {
                get
                {
                    return _resolution;
                }
            }
            [Option('h', "renderHeight", HelpText = "The height of the rendered image.")]
            public int RenderHeight
            {
                set
                {
                    _resolution.y = value;
                }
            }
            [Option('w', "renderWidth", HelpText = "The height of the rendered image.")]
            public int RenderWidth
            {
                set
                {
                    _resolution.x = value;
                }
            }

            [Option('r', "renderPath", HelpText = "The path to render the images to.",
                Required = true)]
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

            public ExportScene.PostExportAction ExportActions
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
                        _exportActions |= ExportScene.PostExportAction.WriteToFile;
                        _jsonSavePath = pathValidator.Path;
                    }
                }
            }
            [Option('t', "transmit", HelpText = "Send data to a renderer instance using TCP/IP.",
                Default = false)]
            public bool Transmit
            {
                set
                {
                    if (value)
                    {
                        _exportActions |= ExportScene.PostExportAction.Transmit;
                    }
                    else
                    {
                        _exportActions &= ~ExportScene.PostExportAction.Transmit;
                    }
                }
            }
            [Option("logExport", HelpText = "Write the Json Scene State to Console.",
                Default = false)]
            public bool LogJson
            {
                set
                {
                    if (value)
                    {
                        _exportActions |= ExportScene.PostExportAction.Log;
                    }
                    else
                    {
                        _exportActions &= ~ExportScene.PostExportAction.Log;
                    }
                }
            }

            [Option('p', "prettyPrint", HelpText = "Whether the json file should be formatted " +
                "or minified.", Default = false)]
            public bool PrettyPrint { get; set; }
        }

        private RuntimeArgs _args = null;

        private ExportScene _exporter = null;

        private void Start()
        {
            if (FindObjectOfType<PhysicsExportManager>() == null)
            {
                Debug.LogError("Cannot have multiple instances of PhysicsExportManager");
                Destroy(this);
            }
            StartCoroutine(ExportLoop());
        }

        IEnumerator ExportLoop()
        {
            Collider[] colliders =
                FindObjectsOfType<Collider>()
                .Where((collider) =>
                {
                    return collider.gameObject.activeInHierarchy
                    && !collider.gameObject.TryGetComponent(out MeshCollider _);
                }).ToArray();

            _exporter.ExportFolder = _args.JsonPath;
            float delaySeconds = _args.MillisecondsDelay / 1000f;
            for (int i = 0; i < _args.Exports && Application.isPlaying; i++)
            {
                foreach (Collider collider in colliders)
                {
                    if (!collider.gameObject.TryGetComponent(out Rigidbody rb))
                    {
                        rb = collider.gameObject.AddComponent<Rigidbody>();
                    }

                    rb.mass = 10;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    rb.AddForce(new Vector3(
                                UnityEngine.Random.Range(-20, 20),
                                UnityEngine.Random.Range(-20, 20),
                                UnityEngine.Random.Range(-20, 20)),
                                ForceMode.Impulse);
                }

                _exporter.ExportCurrentScene(_args.ExportActions, _args.RenderResolution,
                    _args.RenderPath, _args.PrettyPrint);
                Debug.Log($"Exported {i} out of {_args.Exports}.");

                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            if (_args.ExportActions.HasFlag(ExportScene.PostExportAction.Transmit))
            {
                Debug.Log("Emptied queue and sending closing message.");
                _exporter.Sender.FinishTransmissionsAndClose();
            }

            Debug.Log("Exiting Physics Instance...");
            Application.Quit(0);
        }

        private static void ProcessArgs(RuntimeArgs args)
        {
            if (!args.ValidTiming)
            {
                Debug.LogError("Invalid export timings provided.");
                Application.Quit(-1);
            }

            ExportScene exporter = FindObjectOfType<ExportScene>();
            GameObject manager =
                exporter != null
                ? exporter.gameObject
                : null ?? new GameObject
                {
                    name = "Physics Export Manager"
                };

            if (exporter == null)
            {
                exporter = manager.AddComponent<ExportScene>();
            }

            PhysicsExportManager physicsManager = FindObjectOfType<PhysicsExportManager>();
            if (physicsManager == null)
            {
                physicsManager = manager.AddComponent<PhysicsExportManager>();
            }
            physicsManager._args = args;
            physicsManager._exporter = exporter;
            Debug.Log("Initialized Physics Export Manager.");
        }

        // if physics is not defined, this is the renderer, so do not process physics runtime args
#if PHYSICS
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
#endif
        private static void Initialize()
        {
            List<string> args = new List<string>();

            string[] commandLineArgs = Environment.GetCommandLineArgs();

            bool skipNext = false;
            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                if (skipNext)
                {
                    skipNext = false;
                }
                else
                {
                    switch (commandLineArgs[i].ToLowerInvariant())
                    {
                        case "-quit":
                        case "-batchmode":
                        case "-nographics":
                            skipNext = false;
                            break;
                        case "-logfile":
                            skipNext = true;
                            break;
                        default:
                            args.Add(commandLineArgs[i]);
                            skipNext = false;
                            break;
                    }
                }
            }

            // Parser will handle logging failed arguments
            // so close the application if necessary
            Parser.Default.ParseArguments<RuntimeArgs>(args)
                .WithParsed(ProcessArgs)
                .WithNotParsed((_) => Application.Quit(0));
        }
    }
}
