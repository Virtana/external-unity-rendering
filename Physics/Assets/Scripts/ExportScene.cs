using ExternalUnityRendering.PathManagement;
using ExternalUnityRendering.TcpIp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExternalUnityRendering
{
    public class ExportScene : MonoBehaviour
    {
        // Currently being used for testing write to file functionality only
        public enum ExportType
        {
            Transmit,
            WriteToFile,
            Both
        };
        
        private DirectoryManager _exportFolder;

        public string ExportFolder
        {
            get
            {
                return _exportFolder.Path;
            }
            set
            {
                _exportFolder = new DirectoryManager(value);
            }
        }

        private void Start()
        {
            _exportFolder = new DirectoryManager();
        }

        private void WriteStateToFile(string state)
        {
            FileManager file = new FileManager(_exportFolder, 
                $"Physics State-{ DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff-UTCzz}.json");
            file.WriteToFile(state);
        }

        // HACK functionality and structure needs to be reworked
        public void ExportCurrentScene(ExportType exportMode = ExportType.Transmit)
        {
            // pauses the state of the Unity
            Time.timeScale = 0; 

            Debug.Log("Beginning Export.");

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

            foreach (GameObject exportObject in exportObjects)
            {
                exportObject.transform.SetParent(transform, true);
            }

            Debug.Log("Exporting...");
            SceneState scene = new SceneState(transform);
            string state = JsonConvert.SerializeObject(scene, Formatting.Indented);

            if (exportMode == ExportType.Transmit || exportMode == ExportType.Both)
            {
                Sender sender = new Sender();
                sender.Send(state);
            }

            if (exportMode == ExportType.Both || exportMode == ExportType.WriteToFile)
            {
                WriteStateToFile(state);
            }

            Debug.Log($"Export succeeded at { DateTime.Now }");
            
            foreach (GameObject exportObject in exportObjects)
            {
                exportObject.transform.parent = null;
            }

            Time.timeScale = 1;
        }
    }

    // Extends Export ObjectState Construction
    public partial class ObjectState
    {
        /// <summary>
        /// Create an ObjectState representing the GameObject using its 
        /// transform.
        /// </summary>
        /// <param name="transform">The transform of the gameObject.</param>
        public ObjectState(Transform transform)
        {
            Name = transform.name;
            ObjectTransform = new TransformState(transform);
            Children = new List<ObjectState>();

            foreach (Transform childTransform in transform)
            {
                Children.Add(new ObjectState(childTransform));
            }
        }
    }

    // Extends SceneState Adding Export Object Construction
    public partial class SceneState
    {
        /// <summary>
        /// Create a new SceneState and create a new ObjectState using 
        /// <paramref name="transform"/> and assign it as the scene root.
        /// </summary>
        /// <param name="transform">The Transform of the root 
        /// GameObject.</param>
        public SceneState(Transform transform)
            : this(new ObjectState(transform))
        {
        }
    }
}
