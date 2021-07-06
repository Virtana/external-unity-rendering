using UnityEngine;
using ExternalUnityRendering;
using System;
using System.Threading.Tasks;

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

    private static void FailAndQuit(string errorMessage)
    {
        Debug.LogError(errorMessage);
        Application.Quit(1);
    }

#if PHYSICS
    private class Time
    {
        public int Milliseconds { get; private set; }

        public Time(string time)
        {
            if (time.Length < 2)
            {
                FailAndQuit("Invalid Time Input");
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
                    FailAndQuit("Invalid modifier. Valid modifiers are: " +
                        "\n\t'm' : minutes" +
                        "\n\t's' : seconds" +
                        "\n\t' ' : milliseconds");
                    break;
            }

            if (!int.TryParse(time, out int number))
            {
                FailAndQuit("Failed to parse time. Check the parameters given.");
            }
            else if (number < 0)
            {
                FailAndQuit("Time cannot be less than 0");
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
        // Keep running while not done and editor is running
        for (int i = 0; i < totalExports && Application.isPlaying; i++)
        {
            exporter.ExportCurrentScene(afterExport, rendererOutputResolution,
                rendererOutputFolder, true);

            // delay is the amount of time that the physics system will calculate for in
            // between renders. Rendering is a blocking task that "freezes" unity time,
            // so this does not represent the real time between two exports.

            await Task.Delay(delay);
        }
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
#if PHYSICS
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

        // TODO add checks for folder path, export type and output resolution
        string[] commandLineArgs = Environment.GetCommandLineArgs();
        for (int i = 0, j = 1; j < commandLineArgs.Length; i++, j++)
        {
            switch (commandLineArgs[i])
            {
                case "-delay":
                    delayArgument = new Time(commandLineArgs[j]);
                    break;
                case "-time":
                    totalTimeArgument = new Time(commandLineArgs[j]);
                    break;
                case "-export":
                    if (!int.TryParse(commandLineArgs[j], out totalExportsArgument))
                    {
                        FailAndQuit("Failed to parse export count.");
                    }
                    else if (totalExportsArgument < 1)
                    {
                        FailAndQuit("Export count is less than 1. No exports will be performed.");
                    }
                    break;
            }
        }

        if (delayArgument != null && totalExportsArgument < 1 && totalTimeArgument != null
            && delayArgument.Milliseconds * totalExportsArgument != totalTimeArgument.Milliseconds)
        {
            FailAndQuit("Inconsistent time arguments given.");
        }

        int totalExports = -1;
        int delay = -1;

        if (delayArgument != null && totalTimeArgument != null)
        {
            totalExports =
                    totalTimeArgument.Milliseconds / delayArgument.Milliseconds;
            if (totalExports < 1)
            {
                FailAndQuit("Provided delay and total time result in a total export count less than 0.");
            }

            delay = delayArgument.Milliseconds;
        }
        else if (delayArgument != null && totalExportsArgument < 1)
        {
            totalExports = totalExportsArgument;
            delay = delayArgument.Milliseconds;
        }
        else if (totalExportsArgument < 1 && totalTimeArgument != null)
        {
            totalExports = totalExportsArgument;
            delay = totalTimeArgument.Milliseconds / totalExportsArgument;

        }
        else
        {
            FailAndQuit("Missing Arguments to export. ");
        }

        // TODO add other options currently partly hardcoded
        ExportLoop(exporter, delay, totalExports, ExportScene.PostExportAction.Transmit,
            default, System.IO.Path.GetFullPath("../"));
#elif RENDERER

#else
        Debug.LogError("Renderer or Physics is not defined. Will not do anything. Exiting...");
        Application.Quit(1);
#endif
    }
}
