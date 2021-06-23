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
        [Flags]
        public enum ExportType
        {
            // None is intended for testing serialization errors
            None = 0,
            Transmit = 1,
            WriteToFile = 2,
            Log = 4
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

        private void Awake()
        {
            _exportFolder = new DirectoryManager();
        }

        private void WriteStateToFile(string state)
        {
            FileManager file = new FileManager(_exportFolder, 
                $"Physics State-{ DateTime.Now:yyyy-MM-dd-HH-mm-ss-UTCzz}.json", true);
            file.WriteToFile(state);
        }

        // HACK functionality and structure needs to be reworked
        // TODO add options for receiver
        public void ExportCurrentScene(ExportType exportMode = ExportType.Log, bool prettyPrint = false)
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

            Formatting jsonFormat = prettyPrint ? Formatting.Indented : Formatting.None;
            string state = JsonConvert.SerializeObject(scene, jsonFormat);

            if ((exportMode & ExportType.Log) == ExportType.Log)
            {
                Debug.Log($"JSON Data = { state }");
            }

            if ((exportMode & ExportType.Transmit) == ExportType.Transmit)
            {
                Sender sender = new Sender();
                sender.SendAsync(state);
            }

            if ((exportMode & ExportType.WriteToFile) == ExportType.WriteToFile)
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
