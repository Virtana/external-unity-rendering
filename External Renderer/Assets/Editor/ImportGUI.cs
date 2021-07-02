using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    /// <summary>
    /// Class allowing quick imports + renders of scenes from the editor.
    /// </summary>
    [CustomEditor(typeof(ImportScene))]
    class ImportGUI : Editor
    {
        /// <summary>
        /// The path to the file to be imported.
        /// </summary>
        private string _importFile = System.IO.Directory.GetCurrentDirectory();

        /// <summary>
        /// The path to the folder to render to.
        /// </summary>
        private string _renderFolder = System.IO.Directory.GetCurrentDirectory();

        // HACK very janky

        /// <summary>
        /// Method that is called every time the inspector needs to be updated.
        /// </summary>
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
                    return;
                }

                // TODO add checkboxes for use datapath or json path or an assigned path
                if (render.Path == Application.persistentDataPath)
                {
                    Debug.LogError("Invalid render folder given. Using path specified in JSON file.");
                    return;
                }
                currentImporter.ImportCurrentScene(import, render);
            }
        }
    }
}
