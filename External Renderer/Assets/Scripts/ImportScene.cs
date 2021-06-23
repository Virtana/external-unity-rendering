using ExternalUnityRendering.CameraUtilites;
using ExternalUnityRendering.PathManagement;
using ExternalUnityRendering.TcpIp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExternalUnityRendering
{
    public class ImportScene : MonoBehaviour
    {
        private DirectoryManager _renderFolder;
        public string RenderFolder
        {
            get
            {
                return _renderFolder.Path;
            }
            set
            {
                // Propery will handle failure
                _renderFolder = new DirectoryManager(value);
            }
        }

        // Represents when the scene was exported.
        // if null, then no import has occured
        public DateTime? ExportTimestamp;

        private void Awake()
        {
            _renderFolder = new DirectoryManager();
            // Set timeScale to 0. Scene must always be static.
            // will be updated on each import
            Time.timeScale = 0;

            Debug.Log("Awaiting Messages?");
            Receiver client = new Receiver();

            // non blocking async function
            client.ReceiveMessage(ImportCurrentScene);
        }

        // Refactored to allow for the caller to manage where the data files
        // this is meant for testing purposes
        public void ImportCurrentScene(FileManager importFile)
        {
            if (importFile == null || string.IsNullOrEmpty(importFile.Path))
            {
                Debug.LogError("Cannot Import json file. No file has been assigned.");
                return;
            }

            string json = importFile.ReadFile();

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"No data in file { importFile.Path }");
                return;
            }
            ImportCurrentScene(json);
        }

        public void ImportCurrentScene(string json)
        {
            Debug.Log("Beginning Import.");

            // TODO add objects to this list based on if they are new in importer.
            List<GameObject> importObjects = new List<GameObject>();
            Scene currentScene = SceneManager.GetActiveScene();
            currentScene.GetRootGameObjects(importObjects);
            importObjects.RemoveAll((obj) => obj == gameObject);

            Camera[] cameras = FindObjectsOfType<Camera>();
            List<CustomCamera> customCameras = new List<CustomCamera>();

            if (cameras.Length == 0)
            {
                // If cam is empty, then no cameras were found.
                Debug.LogError("Missing Camera! Importer cannot render from this.");
                return;
            }

            // add custom cameras to all cameras in the scene and save them
            foreach (Camera camera in cameras)
            {
                CustomCamera customCam = camera.gameObject.GetComponent<CustomCamera>();
                if (customCam == null)
                {
                    customCam = camera.gameObject.AddComponent<CustomCamera>();
                }
                customCameras.Add(customCam);
            }

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

            Debug.LogFormat($"Imported state that was generated at { ExportTimestamp }");

            // TODO test this
            foreach (CustomCamera camera in customCameras) {
                camera.RenderPath = RenderFolder;
                camera.RenderImage(new Vector2Int(1920,1080));
            }

            // FindObjectOfType<CustomCamera>()
            //    .RenderImage(ImageSaveFolder, new Vector2Int(1920,1080));

            foreach (GameObject importObject in importObjects)
            {
                importObject.transform.parent = null;
            }
            // unparent in case new objects get added
        }
    }

    public partial class ObjectState
    {
        // TODO when implemented adding new objects, also remove existing missing objects
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
