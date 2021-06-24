using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    public class TestingScript : MonoBehaviour
    {
        [MenuItem("Testing/Prepare Importer")]
        public static void TestImports()
        {
            if (!EditorApplication.isPlaying)
            {
                if (!EditorUtility.DisplayDialog("Scene is not in play mode.",
                    "The scene is not playing. Are you sure you want to continue " +
                    "adding an importer to the scene?", "Yes", "No"))
                {
                    return;
                }
            }

            if (FindObjectOfType<ImportScene>() == null)
            {
                GameObject importerParent = new GameObject()
                {
                    name = "Importer-" + GUID.Generate()
                };

                importerParent.AddComponent<ImportScene>();
            }

        }
    }
}
