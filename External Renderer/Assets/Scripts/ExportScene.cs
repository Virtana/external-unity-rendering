using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SceneStateExporter
{
#if UNITY_EDITOR
    [CustomEditor(typeof(ExportScene))]
    public class QuickEditorExport : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.HelpBox("Click Export Now to export now.", MessageType.None);

            ExportScene currentExporter = target as ExportScene;
            if (GUILayout.Button("Export Now"))
            {
                currentExporter.ExportCurrentScene();
            }
        }
    }
#endif

    // compile freeze
    //;=;

    public class ExportScene : MonoBehaviour
    {
        private static GameObject exporter;
        public string saveFilePath = @"D:\Virtana\obj.json";

        public static void Export()
        {
            if (exporter == null)
            {
                Debug.LogError("No exporter found.");
                return;
            }

            exporter.GetComponent<ExportScene>().ExportCurrentScene();
        }

        // add function to grab active gameobjects
        void Awake()
        {
            Debug.Log("Exporter Awake");
            exporter = gameObject;
        }

        public void ExportCurrentScene()
        {
            Debug.Log("Beginning Export.");
            // possibly implement a pause engine state here


            // get all current items in scene except the exporter
            Scene currentScene = SceneManager.GetActiveScene();
            List<GameObject> exportObjects = new List<GameObject>();
            currentScene.GetRootGameObjects(exportObjects);
            exportObjects.RemoveAll((obj) => gameObject == obj);


            if (exportObjects == null || exportObjects.Count == 0)
            {
                Debug.LogWarning("Empty object List.");
                return;
            }

            foreach (var gObj in exportObjects)
            {
                gObj.transform.SetParent(transform, true);
            }

            Debug.Log("Exporting...");
            var currentState = ObjectState.GenerateState(transform);

            Debug.Log("Serializing...");
            var state = JsonConvert.SerializeObject(currentState);
            Debug.Log(state);

            // add validation
            System.IO.File.WriteAllText(saveFilePath, state);

            // put items back in place
            //foreach (var gObj in exportObjects)
            //{
            //    gObj.transform.parent = null;
            //}
            Debug.LogFormat("Saved state to {0}!", saveFilePath);
        }
    }

    public class Exporter
    {
        [RuntimeInitializeOnLoadMethod]
        public static void InitializeExporter()
        {
            Debug.Log("Triggered Exporter Generation");
            // Generate Uniquely Named Object
            GameObject root = new GameObject("Exporter-" + Guid.NewGuid());

            root.AddComponent<ExportScene>();
        }
    }
}