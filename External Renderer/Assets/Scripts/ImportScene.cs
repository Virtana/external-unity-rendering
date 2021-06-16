using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SceneStateExporter
{
#if UNITY_EDITOR
    [CustomEditor(typeof(ImportScene))]
    public class QuickEditorImport : Editor
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
#endif

    public class ImportScene : MonoBehaviour
    {
        // make editor only / debugging options
        public string ImportFilePath = @"D:\Virtana\obj.json";
        public string ImageSaveFolder = @"D:\Virtana\Planning";
        // editor function
        public void ImportCurrentScene()
        {
            ImportCurrentScene(System.IO.File.ReadAllText(ImportFilePath));
        }

        // Represents when the scene was exported.
        // if null, then no import has occured
        public DateTime? ExportTimestamp;

        private void Awake()
        {
            Receiver client = new Receiver();

#warning Client.RecieveMessage blocks the current thread.
            client.RecieveMessage(ImportCurrentScene); 
        }

        // TODO:
        // implement a pause engine state here
        // ^ May not be necessary as this is singlethreaded and blocking
        // may be a concern if jobs are used.
        // add debug mode with file (and automatic filename checking)
        // check if file exists
        // try some sort of better error handling + make a CustomSerialisation
        // settings to Log the error and later ping a server
        // communicate failstate to server
        // add sending the image path or include in state object
        // ^ or implement saving ing Application.persistentDataPath

        public void ImportCurrentScene(string json)
        {
            Debug.Log("Beginning Import.");
            
            // get all current items in scene
            Scene currentScene = SceneManager.GetActiveScene();
            List<GameObject> importObjects = new List<GameObject>();
            currentScene.GetRootGameObjects(importObjects);
            if (importObjects == null || importObjects.Count == 0)
            {
                Debug.LogWarning("Empty object List.");
                return;
            }

            foreach (GameObject importObject in importObjects)
            {
                importObject.transform.SetParent(transform, true);
            }

            Debug.Log("Deserializing...");

            SceneState state = JsonConvert.DeserializeObject<SceneState>(json);
 
            if (state == null)
            {
                Debug.LogError("Failed to deserialize!");
                return;
            }

            state.sceneRoot.UnpackData(transform);
            ExportTimestamp = state.exportDate;

            // put items back in place
            foreach (GameObject importObject in importObjects)
            {
                importObject.transform.parent = null;
            }
            Debug.LogFormat("Imported state from {0}! It was generated at {1}", 
                ImportFilePath, ExportTimestamp);

            // TODO test this
            CustomCamera[] cameras = FindObjectsOfType<CustomCamera>();
            foreach (CustomCamera camera in cameras) {
                camera.RenderImage(ImageSaveFolder, new Vector2Int(1920,1080));
            }
            
            // FindObjectOfType<CustomCamera>()
            //    .RenderImage(ImageSaveFolder, new Vector2Int(1920,1080));
        }
    }

    public partial class ObjectState
    {
        public void UnpackData(Transform transform)
        {
            // update transforms
            transform.position = ObjectTransform.Position;
            transform.rotation = ObjectTransform.Rotation;
            transform.localScale = ObjectTransform.Scale;

            foreach (ObjectState child in Children)
            {
                var childTransform = transform.Find(child.Name);
                if (childTransform == null)
                {
                    Debug.LogWarningFormat("Child {0} missing from {1}.",
                        child.Name, transform.name);
                }
                else
                {
                    child.UnpackData(childTransform);
                }
            }
        }
    }
}
