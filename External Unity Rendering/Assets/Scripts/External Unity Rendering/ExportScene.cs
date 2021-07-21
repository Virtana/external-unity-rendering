using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExternalUnityRendering.PathManagement;
using ExternalUnityRendering.TcpIp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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

        // TODO add compile options or assign when created
        // for the port etc and exit if fatal error
        public Sender Sender = new Sender();

        /// <summary>
        /// Initializes the state of the Exporter.
        /// </summary>
        private void Awake()
        {
            _exportFolder = new DirectoryManager();
            _exportActions = new Dictionary<PostExportAction, Func<string, bool>>()
            {
                {PostExportAction.Nothing, (state) => { return true; } },
                {PostExportAction.Transmit, (state) => Sender.QueueSend(state) },
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

            _serializer.Converters.Add(new SerializableGameobjectConverter());
            _serializer.Converters.Add(new SerializableSceneConverter());
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

            SerializableScene.CameraSettings render =
                new SerializableScene.CameraSettings(renderResolution, renderDirectory);

            Debug.Log("Exporting...");
            SerializableScene scene = new SerializableScene(transform, render);

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

        private void SerializeAndExport(SerializableScene scene, PostExportAction exportMode, bool prettyPrint)
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

                if (!succeeded || state == null)
                {
                    Debug.Log("Aborting Export. Failed to serialize. Check logs for cause.");
                    return;
                }

                foreach (KeyValuePair<PostExportAction, Func<string, bool>> item in _exportActions)
                {
                    if ((item.Key & exportMode) == item.Key)
                    {
                        bool success = item.Value.Invoke(state);
                        if (item.Key == PostExportAction.Nothing)
                        {
                            continue;
                        }
                        if ((item.Key & exportMode) == PostExportAction.Transmit)
                        {
                            Debug.Log("Queued Data to be transmitted. See logs for status.");
                        }
                        else if (success)
                        {
                            // need to add consideration for async saying nope
                            Debug.Log($"SUCCESS: Completed {item.Key & exportMode} at { DateTime.Now }.");
                        }
                        else
                        {
                            Debug.LogError($"FAILED: {item.Key & exportMode} at { DateTime.Now } failed to complete fully. " +
                                "See logs for more details.");
                        }
                    }
                }
            }
            catch (JsonException je)
            {
                Debug.LogError($"Unexpected JSON Deserialization Error occurred!\n{je}");
            }
        }
    }
}
