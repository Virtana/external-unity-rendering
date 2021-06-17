using Newtonsoft.Json;
using System.Collections.Generic;
using System;
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

    public class ExportScene : MonoBehaviour
    {
        public string ExportPath = @"D:\Virtana\obj.json";

        // Function subject to change
        public void ExportCurrentScene()
        {
            // pauses the state of the Unity
            Time.timeScale = 0; 

            Debug.Log("Beginning Export.");

            // let sender initialize
            Sender sender = new Sender();

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

            Sender sender = new Sender();
            sender.Send(state);

            Debug.Log($"State transmission succeeded at { DateTime.Now.ToString() }");
            
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
