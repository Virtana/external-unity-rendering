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
    public class ImportScene : MonoBehaviour
    {
        private static GameObject importer;
        public string saveFilePath = @"D:\Virtana\obj.json";

        public static void Export()
        {
            if (importer == null)
            {
                Debug.LogError("No importer found.");
                return;
            }

            importer.GetComponent<ImportScene>().ImportCurrentScene();
        }

        // add function to grab active gameobjects
        void Awake()
        {
            Debug.Log("Exporter Awake");
            importer = gameObject;
        }

        public void ImportCurrentScene()
        {
            Debug.Log("Beginning Import.");
            // possibly implement a pause engine state here

            // check if file exists

            // get all current items in scene
            Scene currentScene = SceneManager.GetActiveScene();
            List<GameObject> importObjects = new List<GameObject>();
            currentScene.GetRootGameObjects(importObjects);
            if (importObjects == null || importObjects.Count == 0)
            {
                Debug.LogWarning("Empty object List.");
                return;
            }

            foreach (var gObj in importObjects)
            {
                gObj.transform.SetParent(transform, true);
            }

            string json = System.IO.File.ReadAllText(saveFilePath);
            Debug.Log(json);

            Debug.Log("Deserializing...");
            var state = JsonConvert.DeserializeObject<ObjectState>(json);

            Debug.Log("Importing...");
            state.UpdateTransform(transform);

            // put items back in place
            foreach (var gObj in importObjects)
            {
                gObj.transform.parent = null;
            }
            Debug.LogFormat("Saved state to {0}!", saveFilePath);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ImportScene))]
    public class QuickEditorImport : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.HelpBox("Click Import Now to Import now.", MessageType.None);

            ImportScene currentExporter = target as ImportScene;
            if (GUILayout.Button("Import Now"))
            {
                currentExporter.ImportCurrentScene();
            }
        }
    }
#endif

    public class Importer
    {
        //[RuntimeInitializeOnLoadMethod]
        public static void InitializeImporter()
        {
            Debug.Log("Triggered Importer Generation");
            // Generate Uniquely Named Object
            GameObject root = new GameObject("Importer-" + Guid.NewGuid());

            root.AddComponent<ImportScene>();
        }
    }

    public class ImportSceneException : Exception
    {
        // add more for more cases
        public ImportSceneException()
        {
            Debug.LogError("Invalid Hierarchy - Missing child.");
        }

        public ImportSceneException(string message)
            : base(string.Format(
                "Scene State File child count mismatch for : {0}", message))
        {
        }
        public ImportSceneException(string message, Exception inner)
            : base(string.Format(
                "Scene State File child count mismatch for : {0}", message), inner)
        {
        }
    }
}