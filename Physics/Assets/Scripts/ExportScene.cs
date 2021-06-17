using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using ExternalUnityRendering.PathManagement;

namespace ExternalUnityRendering
{
    public class ExportScene : MonoBehaviour
    {
        public enum ExportType
        {
            Transmit,
            WriteToFile,
            Both
        };

        [SerializeField]
        private DirectoryManager _exportFolder;

        private void WriteStateToFile(string state)
        {
            FileManager file = new FileManager(_exportFolder, "obj.json");
            file.WriteToFile(state);
        }

        // Function subject to change
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

            if (exportMode == ExportType.Both|| exportMode == ExportType.WriteToFile)
            {
                WriteStateToFile(state);
            }

            Debug.Log($"State transmission succeeded at { DateTime.Now.ToString() }");
            
            foreach (GameObject exportObject in exportObjects)
            {
                exportObject.transform.parent = null;
            }

            Time.timeScale = 1;
        }

        private void Start()
        {
            _exportFolder = new DirectoryManager();
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
