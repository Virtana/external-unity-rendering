using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    [CustomEditor(typeof(ImportScene))]
    class ImportGUI : Editor
    {
        private string _importFile = System.IO.Directory.GetCurrentDirectory();
        private string _renderFolder = System.IO.Directory.GetCurrentDirectory();

        // HACK very janky
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Select Import Json"))
            {
                _importFile = EditorUtility.OpenFilePanel("Select the file to import scene state from.",
                    _importFile, "json");
            }

            if (GUILayout.Button("Select Render Folder"))
            {
                _renderFolder = EditorUtility.OpenFolderPanel("Select the folder to export the renders to.",
                    _renderFolder, "");
            }

            ImportScene currentImporter = target as ImportScene;
            if (GUILayout.Button("Import Now"))
            {
                PathManagement.DirectoryManager render = new PathManagement.DirectoryManager(_renderFolder);
                PathManagement.FileManager import = new PathManagement.FileManager(_importFile);

                if (import.Path == null)
                {
                    Debug.LogError("Invalid import path given.");
                } else if (render.Path == Application.persistentDataPath)
                {
                    Debug.LogError("Invalid render folder given.");
                } else
                {
                    currentImporter.ImportCurrentScene(import, render);
                }
            }
        }
    }
}
