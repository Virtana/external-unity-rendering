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
            Debug.LogWarning("This function is incomplete. It will do nothing.");
            return;
            // TODO properly implement testing
        }

        // NOTE: this currently does not import.
        static IEnumerator ImportMany()
        {
            GameObject obj = new GameObject();
            ImportScene import = obj.AddComponent<ImportScene>();

            // TODO add in folder choice here
            import.RenderFolder = @"D:\Virtana\Planning\Import"; 

            // TODO add a way of importing paths correctly
            string[] paths = { };

            //for (int i = 0; i < Runs; i++)
            foreach (string path in paths)
            {
                import.ImportCurrentScene(new PathManagement.FileManager(path));
                yield return new WaitForSecondsRealtime(1f);
            }
        }
    }
}
