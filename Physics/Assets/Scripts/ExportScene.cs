using ExternalUnityRendering.PathManagement;
using ExternalUnityRendering.TcpIp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExternalUnityRendering
{
    /// <summary>
    /// Component that manages exporting the scene.
    /// </summary>
    public class ExportScene : MonoBehaviour
    {
        /// <summary>
        /// Flags for what the Exporter should do after serializing the scene.
        /// </summary>
        [Flags]
        public enum PostExportAction
        {
            /// <summary>
            /// Do nothing with the data. Intended for testing serialization errors.
            /// </summary>
            Nothing = 0,

            /// <summary>
            /// Transmit the data to the external renderer asynchronously.
            /// </summary>
            Transmit = 1,

            /// <summary>
            /// Write the state to a file.
            /// </summary>
            WriteToFile = 2,

            /// <summary>
            /// Log the state to the console. Intended for debugging and development testing.
            /// </summary>
            Log = 4
        };

        /// <summary>
        /// Manager for the folder where states will be written to file.
        /// </summary>
        private DirectoryManager _exportFolder;

        /// <summary>
        /// Property to manage the export folder.
        /// </summary>
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

        private readonly JsonSerializer _serializer = new JsonSerializer();

        /// <summary>
        /// Dictionary relating the PostExportActions to the actions they represent.
        /// </summary>
        private Dictionary<PostExportAction, Func<string, bool>> _exportActions;

        /// <summary>
        /// Initializes the state of the Exporter.
        /// </summary>
        private void Awake()
        {
            _exportFolder = new DirectoryManager();
            _exportActions = new Dictionary<PostExportAction, Func<string, bool>>()
            {
                {PostExportAction.Nothing, (state) => { return true; } },
                {PostExportAction.Transmit, (state) => new Sender().SendAsync(state) },
                {PostExportAction.WriteToFile, (state) => WriteStateToFile(state) },
                {PostExportAction.Log, (state) => { Debug.Log($"JSON Data = { state }"); return true; } },
            };


            _serializer.NullValueHandling = NullValueHandling.Ignore;
            _serializer.Error += delegate (object sender, ErrorEventArgs args)
            {
                if (args.CurrentObject == args.ErrorContext.OriginalObject)
                {
                    Debug.LogError(args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            };

            _serializer.Converters.Add(new ObjectStateConverter());
            _serializer.Converters.Add(new SceneStateConverter());
        }

        /// <summary>
        /// Helper function to write the JSON state of the scene to file.
        /// </summary>
        /// <param name="state">The serialized scene state in JSON format.</param>
        /// <returns>Whether or not the operation was successful.</returns>
        private bool WriteStateToFile(string state)
        {
            string filename = $"Physics State-{ DateTime.Now:yyyy-MM-dd-HH-mm-ss-UTCzz}.json";
            FileManager file = new FileManager(_exportFolder, filename, true);

            if (file.Path == null)
            {
                Debug.LogError($"Could not create file: {_exportFolder.Path}/{filename}");
                return false;
            }

            return file.WriteToFile(state);
        }

        /// <summary>
        /// Export the current scene state.
        /// </summary>
        /// <param name="exportMode">What to do with the state of the scene.</param>
        /// <param name="renderResolution">The resolution for the renders produced by
        /// the external renderer.</param>
        /// <param name="renderDirectory">The directory to write the JSON files to
        /// if the WriteToFile flag is set.</param>
        /// <param name="prettyPrint">Whether to format the JSON to be more human
        /// readable.</param>
        public void ExportCurrentScene(PostExportAction exportMode,
            Vector2Int renderResolution = default, string renderDirectory = "",
            bool prettyPrint = false)
        {
            // ensure this gameobject is a root object.
            transform.parent = null;

            // pause Unity Physics calculations
            Time.timeScale = 0;

            Debug.Log("Beginning Export.");

            // get all current items in scene except the exporter
            Scene currentScene = SceneManager.GetActiveScene();
            List<GameObject> exportObjects = new List<GameObject>();
            currentScene.GetRootGameObjects(exportObjects);
            exportObjects.RemoveAll((obj) => gameObject == obj);

            // if there are no items to export, do nothing.
            if ((exportObjects?.Count ?? 0) == 0)
            {
                Debug.LogWarning("Empty object List.");
                return;
            }

            // set every other object as a child of this gameobject to
            foreach (GameObject exportObject in exportObjects)
            {
                exportObject.transform.SetParent(transform, true);
            }

            SceneState.CameraSettings render =
                new SceneState.CameraSettings(renderResolution, renderDirectory);

            Debug.Log("Exporting...");
            SceneState scene = new SceneState(transform, render);

            Task.Run(() =>
            {
                SerializeAndExport(scene, exportMode, prettyPrint);
            });

            // unparent all gameobject so that exporting can be preformed
            // again in the event of an unexpectedexception.
            foreach (GameObject exportObject in exportObjects)
            {
                exportObject.transform.parent = null;
            }

            Time.timeScale = 1;
        }

        private void SerializeAndExport(SceneState scene, PostExportAction exportMode, bool prettyPrint)
        {
            try
            {
                _serializer.Formatting = prettyPrint ? Formatting.Indented : Formatting.None;

                bool succeeded = true;
                _serializer.Error += delegate (object sender, ErrorEventArgs args)
                {
                    succeeded = false;
                };

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                using (System.IO.StringWriter sw = new System.IO.StringWriter(sb))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    _serializer.Serialize(writer, scene);
                }

                string state = sb.ToString();

                if (!succeeded || state == null || !state.EndsWith("}"))
                {
                    Debug.Log("Aborting Export. Failed to serialize. Check logs for cause.");
                    return;
                }

                // NOTE a fancier way is Linq Aggregate
                // bool success = _exportActions.Aggregate(true, (acc, kv) =>
                //      ((kv.Key & exportMode) == kv.Key) && kv.Value.Invoke(state) && acc);

                // for all the functions in export items
                // check if the flag (key) is set, then invoke the function (value)
                // if function returns false (i.e. failed)
                foreach (KeyValuePair<PostExportAction, Func<string, bool>> item in _exportActions)
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
            catch (JsonException je)
            {
                Debug.LogError($"Unexpected JSON Deserialization Error occurred!\n{je}");
            }
        }
    }
}
