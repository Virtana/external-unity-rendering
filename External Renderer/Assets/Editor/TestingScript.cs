using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SceneStateExporter;

public class TestingScript : MonoBehaviour
{
    static public int Runs = 10;
    [MenuItem("Testing/Import+Images")]
    public static void TestImports()
    {
        CustomCamera cam = FindObjectOfType<CustomCamera>();
        cam.StartCoroutine(ImportMany(cam));
    }

    static IEnumerator ImportMany(CustomCamera cam)
    {
        GameObject obj = new GameObject();
        ImportScene import = obj.AddComponent<ImportScene>();

        import.ImageSaveFolder = @"D:\Virtana\Planning\Import";

        for (int i = 0; i < Runs; i++)
        {
            import.ImportFilePath = string.Format(@"D:\Virtana\Planning\obj ({0}).json", i + 1);
            import.ImportCurrentScene();
            yield return new WaitForSecondsRealtime(1f);
        }
    }
}
