using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    [CustomEditor(typeof(ImportScene))]
    class ImportGUI : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.HelpBox("Click Import Now to Import now.", MessageType.None);

            ImportScene currentImporter = target as ImportScene;
            if (GUILayout.Button("Import Now"))
            {
                currentImporter.ImportCurrentScene();
            }
        }
    }
}
