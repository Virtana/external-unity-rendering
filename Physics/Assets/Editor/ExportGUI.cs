using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    // TODO improve the functionality/ call
    [CustomEditor(typeof(ExportScene))]
    public class ExportGUI : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.HelpBox("Click Export Now to export now.", MessageType.None);

            ExportScene currentExporter = target as ExportScene;
            if (GUILayout.Button("Export Now"))
            {
                currentExporter.ExportCurrentScene(ExportScene.ExportType.WriteToFile);
            }
        }
    }
}
