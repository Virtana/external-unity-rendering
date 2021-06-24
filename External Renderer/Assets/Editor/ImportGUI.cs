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
            EditorGUILayout.HelpBox("Enter path to import json below.", MessageType.None);
            _importFile = GUILayout.TextField(_importFile);
            EditorGUILayout.HelpBox("Enter path to render images to below.", MessageType.None);
            _renderFolder = GUILayout.TextField(_renderFolder);
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
