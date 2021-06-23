using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    [CustomEditor(typeof(ImportScene))]
    class ImportGUI : Editor
    {
        private string _importFile = "";
        private string _renderFolder = "";
        // HACK very janky
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Select Import Json"))
            {
                _importFile = EditorUtility.OpenFilePanel("Select the file to import scene state from.",
                    System.IO.Directory.GetCurrentDirectory(), "json");
            }

            if (GUILayout.Button("Select Render Folder"))
            {
                _renderFolder = EditorUtility.OpenFolderPanel("Select the folder to export the renders to.",
                    System.IO.Directory.GetCurrentDirectory(), "");
            }

            ImportScene currentImporter = target as ImportScene;
            if (GUILayout.Button("Import Now"))
            {
                currentImporter.RenderFolder = _renderFolder;
                PathManagement.FileManager import = new PathManagement.FileManager(_importFile);

                if (import.Path == null)
                {
                    Debug.LogError("Invalid import path given.");
                } else if (currentImporter.RenderFolder == Application.persistentDataPath)
                {
                    Debug.LogError("Invalid render folder given.");
                } else
                {
                    currentImporter.ImportCurrentScene(import);
                }
            }
        }
    }
}
