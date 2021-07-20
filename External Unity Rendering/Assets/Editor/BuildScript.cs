using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using ExternalUnityRendering.PathManagement;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript : MonoBehaviour
{
    private enum BuildConfigurations
    {
        Renderer,
        Physics
    }

    private class BuildArgs
    {
        private string _buildFolder = null;

        [Option('c', "config", HelpText = "Whether to build physics or a renderer instance.",
            Default = BuildConfigurations.Renderer)]
        public BuildConfigurations Config { get; set; }

        [Option('b', "build", HelpText = "The folder to build the project to.", Required = true)]
        public string BuildFolder
        {
            get
            {
                return _buildFolder;
            }
            set
            {
                DirectoryManager pathValidator = new DirectoryManager(value);
                if (Path.GetFullPath(value) == pathValidator.Path)
                {
                    _buildFolder = pathValidator.Path;
                }
            }
        }

        [Option("options", HelpText = "Other Build options to compile with. See " +
            "https://docs.unity3d.com/ScriptReference/BuildOptions.html for valid choices. " +
            "(EnableHeadlessMode is always enabled for Physics, and disabled for Renderer.)",
            Default = BuildOptions.None)]
        public BuildOptions Options
        {
            get;
            set;
        }

        [Option('t', "buildTarget", HelpText = "What platform to build for.",
            Default = BuildTarget.StandaloneLinux64)]
        public BuildTarget Target
        {
            get;
            set;
        }


    }

    private static void PerformBuild(BuildArgs args)
    {
        string outputName = Enum.GetName(typeof(BuildConfigurations), args.Config);

        if (args.Config == BuildConfigurations.Physics)
        {
            args.Options |= BuildOptions.EnableHeadlessMode;
        }
        else
        {
            args.Options &= ~BuildOptions.EnableHeadlessMode;
        }

        // create in the build folder a subfolder holding the executable
        string outputBinary = Path.GetFullPath(Path.Combine(args.BuildFolder, outputName, outputName));

        // append .exe for windows executable
        if (args.Target == BuildTarget.StandaloneWindows || args.Target == BuildTarget.StandaloneWindows64)
        {
            outputBinary += ".exe";
        }

        // get all available scenes, may be customized later
        string[] scenePaths = Directory.GetFiles(Application.dataPath,
            "*.unity", SearchOption.AllDirectories);

        for (int i = 0; i < scenePaths.Length; ++i)
        {
            scenePaths[i] = scenePaths[i].Remove(0, Application.dataPath.Length - 6);
        }

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = outputBinary,
                target = args.Target,
                targetGroup = BuildTargetGroup.Standalone,
                extraScriptingDefines = new string[] { outputName.ToUpperInvariant() },
                options = args.Options
            });

        if (report.summary.result != BuildResult.Succeeded)
        {
            BuildSummary summary = report.summary;
            Debug.LogError($"Build did not succeed. Result was {summary.result}.");
            EditorApplication.Exit(-1);
        } else
        {
            Debug.Log($"Successfully built {args.Config} at {report.summary.buildEndedAt}." +
                $"The build took {report.summary.totalTime}.");
        }
    }

    private static void HandleArgumentErrors(IEnumerable<Error> errors)
    {
        foreach (Error error in errors)
        {
            // cast and handle errors
            if (error is TokenError tokenError)
            {
                Debug.LogError(tokenError.Token);
            }
        }
        EditorApplication.Exit(-1);
    }

    public static void Build()
    {
        List<string> args = new List<string>();

        string[] commandLineArgs = Environment.GetCommandLineArgs();

        // filter unity's arguments
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
                    case "-projectpath":
                    case "-executemethod":
                        skipNext = true;
                        break;
                    default:
                        args.Add(commandLineArgs[i]);
                        skipNext = false;
                        break;
                }
            }
        }

        Console.SetError(Console.Out);
        Parser.Default.ParseArguments<BuildArgs>(args)
            .WithParsed(PerformBuild)
            .WithNotParsed(HandleArgumentErrors);
    }
}
