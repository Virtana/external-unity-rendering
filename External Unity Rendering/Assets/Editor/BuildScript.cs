using UnityEngine;
using System.IO;
using UnityEditor;

public class BuildScript : MonoBehaviour
{
    public static void BuildPhysics()
    {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "PHYSICS" );
        Build("Physics");
    }

    public static void BuildRenderer()
    {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "RENDERER");
        Build("Renderer");
    }

    static void Build(string buildname)
    {
        string[] scenePaths = Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories);
        for (int i = 0; i < scenePaths.Length; ++i)
        {
            scenePaths[i] = scenePaths[i].Remove(0, Application.dataPath.Length - 6);
        }
        BuildPipeline.BuildPlayer(scenePaths,
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../", buildname)),
            BuildTarget.StandaloneLinux64, BuildOptions.None);
    }
}
