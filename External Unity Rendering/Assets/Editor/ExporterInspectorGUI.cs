using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    // TODO Decide if removing
    [CustomEditor(typeof(Exporter))]
    public class ExporterInspectorGUI : Editor
    {
        private string _exportFolder = "";
        private Exporter.PostExportAction _export = Exporter.PostExportAction.Nothing;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.HelpBox("Choose one of the following options:\n\t" +
                "None : Attempt to serialize but do nothing with the data.\n\t" +
                "Transmit: Attempt to transmit over TCP/IP.\n\t" +
                "WriteToFile: Write to file in a specified folder (or the persistent data path).\n\t" +
                "Log: Write to the console.", MessageType.Info);

            _export = (Exporter.PostExportAction)
                EditorGUILayout.EnumFlagsField("How to export Scene State: ", _export);

            // TODO add editor options for different export types
            // could have if statement with the different states and add the options
            if ((_export & Exporter.PostExportAction.WriteToFile)
                == Exporter.PostExportAction.WriteToFile)
            {
                if (GUILayout.Button("Select Export folder"))
                {
                    _exportFolder = EditorUtility.OpenFolderPanel("Select the folder to export the scene state to.",
                        System.IO.Directory.GetCurrentDirectory(), "");
                }
            }

            Exporter currentExporter = target as Exporter;

            if (GUILayout.Button("Export Now"))
            {
                if ((_export & Exporter.PostExportAction.WriteToFile)
                    == Exporter.PostExportAction.WriteToFile)
                {
                    currentExporter.ExportFolder = _exportFolder;
                    if (currentExporter.ExportFolder == Application.persistentDataPath) {
                        Debug.LogError("Invalid render folder given.");
                        return;
                    }
                }
                currentExporter.ExportCurrentScene(_export);
            }
        }
    }
}
