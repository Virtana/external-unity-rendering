using UnityEngine;
using System.IO;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class BuildScript : MonoBehaviour
{
    // add a better way to detect options
    private static readonly Dictionary<string, BuildConfigurations> _buildSymbols = new Dictionary<string, BuildConfigurations>
    {
        { "-physics", BuildConfigurations.Physics },
        { "-renderer", BuildConfigurations.Renderer },
    };

    private enum BuildConfigurations
    {
        Physics = 1,
        Renderer = 2
    }

// TODO replace with the kinda hack parser from the init script
// add project path checking and call EditorApplication.Quit(1) if fail
    public static void Build()
    {
        // Filter unity's command line args
        string[] args = Environment.GetCommandLineArgs();

        string outputName = null;
        // Make renderer by default
        BuildConfigurations config = 0;

        foreach (string arg in args)
        {
            _buildSymbols.TryGetValue(arg, out config);
        }

        if (config == 0)
        {
            Console.WriteLine("Missing Argument for build type.");
            Console.ResetColor();
            EditorApplication.Exit(1);
        } else {
            outputName = Enum.GetName(typeof(BuildConfigurations), config);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, outputName.ToUpperInvariant());
        }

        string[] scenePaths = Directory.GetFiles(Application.dataPath,
            "*.unity", SearchOption.AllDirectories);

        for (int i = 0; i < scenePaths.Length; ++i)
        {
            scenePaths[i] = scenePaths[i].Remove(0, Application.dataPath.Length - 6);
        }

        BuildPipeline.BuildPlayer(scenePaths,
            Path.GetFullPath(Path.Combine(Application.dataPath,
                "..", "..", "builds", outputName, outputName)
            ),
            BuildTarget.StandaloneLinux64, BuildOptions.None);
    }
}
