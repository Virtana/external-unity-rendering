using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildScript : MonoBehaviour
{
    private enum BuildConfigurations
    {
        Renderer = 0,
        Physics = 1
    }

// TODO replace with the kinda hack parser from the init script
// add project path checking and call EditorApplication.Quit(1) if fail
// and choice of output folder
    public static void Build()
    {
        // Make renderer by default
        BuildConfigurations config = 0;
        BuildTarget target = BuildTarget.StandaloneLinux64;
        string buildFolder = "";

        // Filter unity's command line args
        string[] commandLineArgs = Environment.GetCommandLineArgs();
        for (int i = 0, j = 1; i < commandLineArgs.Length; i++, j++)
        {
            switch (commandLineArgs[i])
            {
                case "-b":
                case "--build":
                    if (j == commandLineArgs.Length)
                    {
                        Debug.LogError("Missing Build Folder.");
                        EditorApplication.Exit(1);
                    }
                    buildFolder = commandLineArgs[j];
                    break;
                case "-p":
                case "--physics":
                    config = BuildConfigurations.Physics;
                    break;
                case "-r":
                case "--renderer":
                    config = BuildConfigurations.Renderer;
                    break;
                case "--win":
                    target = BuildTarget.StandaloneWindows;
                    break;
                case "--win64":
                    target = BuildTarget.StandaloneWindows64;
                    break;
            }
        }


        string outputName = Enum.GetName(typeof(BuildConfigurations), config);

        string[] scenePaths = Directory.GetFiles(Application.dataPath,
            "*.unity", SearchOption.AllDirectories);

        for (int i = 0; i < scenePaths.Length; ++i)
        {
            scenePaths[i] = scenePaths[i].Remove(0, Application.dataPath.Length - 6);
        }

        BuildOptions buildOptions =  BuildOptions.None;
        if (config == BuildConfigurations.Physics)
        {
            buildOptions |= BuildOptions.EnableHeadlessMode;
        }

        buildOptions |= BuildOptions.None
            //| BuildOptions.Development
            //| BuildOptions.ConnectWithProfiler
            //| BuildOptions.AllowDebugging
            //| BuildOptions.WaitForPlayerConnection
            //| BuildOptions.BuildScriptsOnly
            //| BuildOptions.EnableDeepProfilingSupport
            //| BuildOptions.ConnectToHost
            ;

        string path = Path.GetFullPath(Path.Combine(buildFolder, outputName));
        if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
        {
            path = Path.Combine(path, outputName + ".exe");
        }
        else
        {
            path = Path.Combine(path, outputName);
        }

        BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenePaths,
            locationPathName = path,
            target = target,
            targetGroup = BuildTargetGroup.Standalone,
            extraScriptingDefines = new string[] { outputName.ToUpperInvariant() },
            options = buildOptions
        });

        // add exit if fail
    }
}
