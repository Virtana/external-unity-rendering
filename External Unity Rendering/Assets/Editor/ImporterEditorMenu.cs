using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    /// <summary>
    /// Class for editor menu options
    /// </summary>
    public class ImporterEditorMenu : MonoBehaviour
    {
        /// <summary>
        /// Editor Function to quickly add an importer to the scene.
        /// </summary>
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

            if (FindObjectOfType<Importer>() == null)
            {
                GameObject importerParent = new GameObject()
                {
                    name = "Importer-" + GUID.Generate()
                };

                importerParent.AddComponent<Importer>();
            }

        }
    }
}
