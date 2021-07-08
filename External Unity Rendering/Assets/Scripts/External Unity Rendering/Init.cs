//#define PHYSICS
//#define RENDERER
using System;
using System.Threading.Tasks;
using ExternalUnityRendering;
using ExternalUnityRendering.PathManagement;
using ExternalUnityRendering.TcpIp;
using Newtonsoft.Json;
using UnityEngine;

// cant use init script functionality in editor play mode
// use editor gui functions instead

// Set to !UNITY_EDITOR when not testing code
#if UNITY_2017_1_OR_NEWER //!UNITY_EDITOR
public class Init : MonoBehaviour
{
    // TODO Initialize list
    // Check if exporter/importer exists
    // add if not
    // if importer just exist
    // if exporter then search commandline options for timing
    // then quit
    // may need to add a way to check for physics terminating then
    // check for importer being done then exit (throw signal or something??)

    private static void Exit(string message, int exitCode = 1)
    {
        if (exitCode != 0)
        {
            Debug.LogError(message);
        }
        else
        {
            Debug.Log(message);
        }
        Application.Quit(exitCode);
    }

    private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs ccea)
    {
        ccea.Cancel = true;
        Exit($"Received {ccea.SpecialKey}.", 0);
    }

#if PHYSICS
    private class Time
    {
        public int Milliseconds { get; private set; }

        public Time(string time)
        {
            if (time.Length < 2)
            {
                Exit("Invalid Time Input");
            }

            // get last character
            char timeModifier = time[time.Length - 1];

            if (char.IsDigit(timeModifier))
            {
                timeModifier = ' ';
            } else
            {
                time = time.Remove(time.Length - 1);
            }

            switch (timeModifier)
            {
                case ' ':
                    Milliseconds = 1;
                    break;
                case 's':
                    Milliseconds = 1000;
                    break;
                case 'm':
                    Milliseconds = 60 * 1000;
                    break;
                default:
                    Exit("Invalid modifier. Valid modifiers are: " +
                        "\n\t'm' : minutes" +
                        "\n\t's' : seconds" +
                        "\n\t' ' : milliseconds");
                    break;
            }

            if (!int.TryParse(time, out int number))
            {
                Exit($"Failed to parse time. Given {time}.");
            }
            else if (number < 0)
            {
                Exit("Time cannot be less than 0");
            }
            else
            {
                Milliseconds *= number;
            }
        }
    }

    private static async void ExportLoop(ExportScene exporter, int delay, int totalExports,
        ExportScene.PostExportAction afterExport, Vector2Int rendererOutputResolution,
        string rendererOutputFolder)
    {
        Collider[] colliders = FindObjectsOfType<Collider>();

        foreach (Collider hit in colliders)
        {
            // addforce etc has no effect on inactive GameObjects
            if (!hit.gameObject.activeInHierarchy)
            {
                continue;
            }

            // Handle non-convex mesh collider with non-kinematic rigidbody error
            if (hit.gameObject.TryGetComponent(out MeshCollider _))
            {
                // meshcolliders are used with items that should be static
                // in this test so skip for now otherwise assign out meshcollider
                // and set mesh.convex to true
                continue;
            }

            if (!hit.gameObject.TryGetComponent(out Rigidbody rb))
            {
                rb = hit.gameObject.AddComponent<Rigidbody>();
            }

            rb.mass = 10;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.AddForce(new Vector3(
                        UnityEngine.Random.Range(-50, 50),
                        UnityEngine.Random.Range(-50, 50),
                        UnityEngine.Random.Range(-50, 50)),
                        ForceMode.Impulse);
        }

        exporter._sender.Send();
        // Keep running while not done and application is running
        for (int i = 0; i < totalExports && Application.isPlaying; i++)
        {
            exporter.ExportCurrentScene(afterExport, rendererOutputResolution,
                rendererOutputFolder);

            // delay is the amount of time that the physics system will calculate for in
            // between renders. Rendering is a blocking task that "freezes" unity time,
            // so this does not represent the real time between two exports.

            await Task.Delay(delay);
        }

        if ((afterExport & ExportScene.PostExportAction.Transmit) == ExportScene.PostExportAction.Transmit)
        {
            new Sender().Send(JsonConvert.SerializeObject(new SerializableScene()
            {
                ContinueImporting = false
            }));
        }

        Sender.FinishTransmissionsAndClose();
        Sender.Handle.WaitOne();
        Debug.Log("Emptied queue and closing programs.");

        Exit("Completed Execution.", 0);
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        // TODO change to intercept
        // https://docs.unity3d.com/ScriptReference/ILogHandler.html
        // #:~:text=taking%20this%20survey.-,ILogHandler,-interface%20in%20UnityEngine
        Console.CancelKeyPress += Console_CancelKeyPress;

#if PHYSICS
        string[] commandLineArgs = Environment.GetCommandLineArgs();

        foreach (Camera camera in FindObjectsOfType<Camera>())
        {
            camera.enabled = false;
        }

        ExportScene exporter = FindObjectOfType<ExportScene>();
        if (exporter == null)
        {
            GameObject obj = new GameObject
            {
                name = System.Guid.NewGuid().ToString()
            };

            exporter = obj.AddComponent<ExportScene>();
        }

        // HACK very hacky way of handling and parsing the arguments
        Time delayArgument = null;
        Time totalTimeArgument = null;
        int totalExportsArgument = -1;
        ExportScene.PostExportAction exportAction = ExportScene.PostExportAction.Nothing;
        Vector2Int renderResolution = new Vector2Int();
        DirectoryManager renderPath = new DirectoryManager();
        DirectoryManager jsonPath = new DirectoryManager();

        // TODO add checks for folder path, export type and output resolution

        for (int i = 0, j = 1; i < commandLineArgs.Length; i++, j++)
        {
            switch (commandLineArgs[i])
            {
                case "--delay":
                    if (j == commandLineArgs.Length)
                    {
                        Exit("Missing Delay argument. If it is not needed, exclude it.");
                    }
                    delayArgument = new Time(commandLineArgs[j]);
                    break;
                case "--time":
                    if (j == commandLineArgs.Length)
                    {
                        Exit("Missing time argument. If it is not needed, exclude it.");
                    }
                    totalTimeArgument = new Time(commandLineArgs[j]);
                    break;
                case "--export":
                    if (j == commandLineArgs.Length)
                    {
                        Exit("Missing export count argument. If it is not needed, exclude it.");
                    }
                    else if (!int.TryParse(commandLineArgs[j], out totalExportsArgument))
                    {
                        Exit($"Failed to parse export count. Given {commandLineArgs[j]}");
                    }
                    else if (totalExportsArgument < 1)
                    {
                        Exit("Export count is less than 1. No exports will be performed.");
                    }
                    break;
                case "-h":
                case "--height":
                    if (j == commandLineArgs.Length)
                    {
                        Exit("Missing height argument.");
                    }
                    else if (!int.TryParse(commandLineArgs[j], out int height))
                    {
                        Exit($"Failed to parse height. Given {commandLineArgs[j]}");
                    }
                    else
                    {
                        renderResolution.y = height;
                    }
                    break;
                case "-w":
                case "--width":
                    if (j == commandLineArgs.Length)
                    {
                        Exit("Missing width argument.");
                    }
                    else if (!int.TryParse(commandLineArgs[j], out int width))
                    {
                        Exit($"Failed to parse width. Given {commandLineArgs[j]}");
                    }
                    else
                    {
                        renderResolution.x = width;
                    }
                    break;
                case "-r":
                case "--renderPath":
                    if (j == commandLineArgs.Length)
                    {
                        Exit("Missing render path argument.");
                    }
                    renderPath.Path = commandLineArgs[j];
                    break;
                case "--writeToFile":
                    exportAction |= ExportScene.PostExportAction.WriteToFile;
                    if (j == commandLineArgs.Length)
                    {
                        Exit("Missing path to write json files to.");
                    }
                    else if (!commandLineArgs[j].StartsWith("-"))
                    {
                        jsonPath.Path = commandLineArgs[j];
                        if (jsonPath.Path == Application.persistentDataPath)
                        {
                            Debug.Log($"Writing to {Application.persistentDataPath}");
                        }
                    }
                    break;
                case "--logExport":
                    exportAction |= ExportScene.PostExportAction.Log;
                    break;
                case "--transmit":
                    exportAction |= ExportScene.PostExportAction.Transmit;
                    break;
            }
        }

        if (delayArgument != null && totalExportsArgument > 0 && totalTimeArgument != null
            && delayArgument.Milliseconds * totalExportsArgument != totalTimeArgument.Milliseconds)
        {
            Exit("Inconsistent time arguments given.");
        }

        int totalExports = -1;
        int delay = -1;

        if (delayArgument != null && totalTimeArgument != null)
        {
            totalExports =
                    totalTimeArgument.Milliseconds / delayArgument.Milliseconds;
            if (totalExports < 1)
            {
                Exit("Provided delay and total time result in a total export count less than 0.");
            }

            delay = delayArgument.Milliseconds;
        }
        else if (delayArgument != null && totalExportsArgument > 0)
        {
            totalExports = totalExportsArgument;
            delay = delayArgument.Milliseconds;
        }
        else if (totalExportsArgument > 0 && totalTimeArgument != null)
        {
            totalExports = totalExportsArgument;
            delay = totalTimeArgument.Milliseconds / totalExportsArgument;
        }
        else
        {
            Exit("Missing Arguments to export.");
        }

        Debug.Log($"TOTAL EXPORTS <[-:=|=:-]> {totalExports}");

        // TODO add other options currently partly hardcoded
        exporter.ExportFolder = jsonPath.Path;
        ExportLoop(exporter, delay, totalExports, exportAction,
            renderResolution, renderPath.Path);
#elif RENDERER
        if (FindObjectOfType<ImportScene>() == null)
        {
            GameObject obj = new GameObject
            {
                name = Guid.NewGuid().ToString()
            };

            obj.AddComponent<ImportScene>();
        }
#else
        // TODO FIX render folder go to good one time blank default
        Debug.LogError("Renderer or Physics is not defined. Will not do anything. Exiting...");
        Application.Quit(1);
#endif
    }
}
#endif
