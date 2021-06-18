using ExternalUnityRendering.CameraUtilites;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    public class TestingScript : MonoBehaviour
    {
        static public int Runs = 10;
        [MenuItem("Testing/Import+Images")]
        public static void TestImports()
        {
            CustomCamera cam = FindObjectOfType<CustomCamera>();
            cam.StartCoroutine(ImportMany());
        }

        static IEnumerator ImportMany()
        {
            GameObject obj = new GameObject();
            ImportScene import = obj.AddComponent<ImportScene>();

            import.RenderFolder = @"D:\Virtana\Planning\Import";
            // TODO make sure this is legit somehow first instead of logging a bunch
            // and doing nothing
            // TODO ensure this is safe.
            for (int i = 0; i < Runs; i++)
            {
                import.ImportFilePath = string.Format(@"D:\Virtana\Planning\Scene State ({0}).json", i + 1);
                import.ImportCurrentScene();
                yield return new WaitForSecondsRealtime(1f);
            }
        }
    }
}
