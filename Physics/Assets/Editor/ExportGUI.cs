using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    // TODO improve the functionality/ call
    [CustomEditor(typeof(ExportScene))]
    public class ExportGUI : Editor
    {
        private string _exportFolder = "";
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // maybe add a filename detection?
            EditorGUILayout.HelpBox("Enter the folder to export to below.", MessageType.None);
            _exportFolder = GUILayout.TextField(_exportFolder);
            ExportScene currentExporter = target as ExportScene;
            if (GUILayout.Button("Export Now"))
            {
                currentExporter.ExportFolder = _exportFolder;

                if (currentExporter.ExportFolder == Application.persistentDataPath)
                {
                    Debug.LogError("Invalid render folder given.");
                } else
                {
                    currentExporter.ExportCurrentScene(ExportScene.ExportType.WriteToFile, true);
                }
            }
        }
    }
}
