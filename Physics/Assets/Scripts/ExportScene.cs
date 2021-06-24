using ExternalUnityRendering.PathManagement;
using ExternalUnityRendering.TcpIp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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

        private Dictionary<ExportType, Func<string, bool>> _exportActions;

        private void Awake()
        {
            _exportFolder = new DirectoryManager();
            _exportActions = new Dictionary<ExportType, Func<string, bool>>()
            {
                {ExportType.None, (state) => { return true; } },
                {ExportType.Transmit, (state) => new Sender().SendAsync(state) },
                {ExportType.WriteToFile, (state) => WriteStateToFile(state) },
                {ExportType.Log, (state) => { Debug.Log($"JSON Data = { state }"); return true; } },
            };
        }

        private bool WriteStateToFile(string state)
        {
            string filename = $"Physics State-{ DateTime.Now:yyyy-MM-dd-HH-mm-ss-UTCzz}.json";
            FileManager file = new FileManager(_exportFolder, filename, true);

            if (file.Path == null)
            {
                Debug.LogError($"Could not create file: {_exportFolder.Path}/{filename}");
                return false;
            }

            file.WriteToFile(state);
            return true;
        }

        // HACK functionality and structure needs to be reworked
        public void ExportCurrentScene(ExportType exportMode,
            Vector2Int renderResolution = default, string renderDirectory = "",
            bool prettyPrint = false)
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

            try
            {
                SceneState.CameraSettings render =
                    new SceneState.CameraSettings(renderResolution, renderDirectory);

                Debug.Log("Exporting...");
                SceneState scene = new SceneState(transform, render);

                Formatting jsonFormat = prettyPrint ? Formatting.Indented : Formatting.None;

                JsonSerializerSettings serializerSettings =
                    new JsonSerializerSettings
                    {
                        // log error if reached a problematic point
                        Error = delegate(object sender, ErrorEventArgs args)
                        {
                            if (args.CurrentObject == args.ErrorContext.OriginalObject)
                            {
                                Debug.LogError(args.ErrorContext.Error.Message);
                                args.ErrorContext.Handled = true;
                            }
                        }
                    };

                string state = JsonConvert.SerializeObject(scene, jsonFormat, serializerSettings);

                // NOTE would have loved to use Linq Aggregate
                // bool success = _exportActions.Aggregate(true, (acc, kv) =>
                //      ((kv.Key & exportMode) == kv.Key) && kv.Value.Invoke(state) && acc);

                bool succeeded = true;

                // for all the functions in export items
                // check if the flag (key) is set, then invoke the function (value)
                // if function returns false (i.e. failed)
                foreach (KeyValuePair<ExportType, Func<string, bool>> item in _exportActions)
                {
                    if ((item.Key & exportMode) == item.Key)
                    {
                        succeeded &= item.Value.Invoke(state);
                    }
                }

                if (succeeded)
                {
                    Debug.Log($"SUCCESS: Completed export at { DateTime.Now }");
                }
                else
                {
                    Debug.LogError($"FAILED: Export at { DateTime.Now } failed to complete fully. " +
                        "See logs for more details.");
                }

            }
            finally
            {
                foreach (GameObject exportObject in exportObjects)
                {
                    exportObject.transform.parent = null;
                }

                Time.timeScale = 1;
            }
        }
    }
}
