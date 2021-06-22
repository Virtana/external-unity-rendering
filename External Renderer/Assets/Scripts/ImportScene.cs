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
        private DirectoryManager _renderFolder = new DirectoryManager();
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

        private FileManager _importFile = null;

        // TODO have anything that accesses this check null or empty
        // TODO: if exception for FileManager fail is implemented
        public string ImportFilePath
        {
            get
            {
                return _importFile?.Path;
            }
            set
            {
                // if the path is valid and exists, save it
                if (System.IO.File.Exists(value))
                {
                    _importFile = new FileManager(value);
                }
            }
        }

        // Represents when the scene was exported.
        // if null, then no import has occured
        public DateTime? ExportTimestamp;

        // TODO add objects to this list based on if they are new in importer.
        private List<GameObject> _importObjects = new List<GameObject>();

        private void Start()
        {
            // Set timeScale to 0. Scene must always be static.
            // will be updated on each import
            Time.timeScale = 0;

            // get all current items in scene
            // NOTE if dynamic scene loading is considered, all new root gameobjects
            // must be added in the function separately.
            Scene currentScene = SceneManager.GetActiveScene();
            currentScene.GetRootGameObjects(_importObjects);

            Receiver client = new Receiver();

            //TODO Client.RecieveMessage blocks the current thread.
            client.RecieveMessage(ImportCurrentScene);
        }

        public void ImportCurrentScene()
        {
            if (_importFile == null || string.IsNullOrEmpty(_importFile.Path))
            {
                Debug.LogError("Cannot Import json file. No file has been assigned.");
                return;
            }

            string json = _importFile.ReadFile();
            ImportCurrentScene(json);
        }

        public void ImportCurrentScene(string json)
        {
            Debug.Log("Beginning Import.");

            if (_importObjects == null || _importObjects.Count == 0)
            {
                Debug.LogWarning("Empty object List.");
                return;
            }

            foreach (GameObject importObject in _importObjects)
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

            Debug.LogFormat("Imported state from {0}! It was generated at {1}", 
                ImportFilePath, ExportTimestamp);

            // TODO test this
            CustomCamera[] cameras = FindObjectsOfType<CustomCamera>();
            foreach (CustomCamera camera in cameras) {
                camera.RenderPath = RenderFolder;
                camera.RenderImage(new Vector2Int(1920,1080));
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
