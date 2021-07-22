using System;
using System.Collections.Generic;

using ExternalUnityRendering.CameraUtilites;
using ExternalUnityRendering.PathManagement;
using ExternalUnityRendering.Serialization;
using ExternalUnityRendering.TcpIp;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExternalUnityRendering
{
    /// <summary>
    /// Component that manages importing a scene and rendering images.
    /// </summary>
    public class ImportScene : MonoBehaviour
    {
#if UNITY_EDITOR
        /// <summary>
        /// Initialise a receiver if in editor
        /// </summary>
        private void Awake()
        {
            new Receiver().ProcessCallbackAsync((state) => ImportCurrentScene(state));
            Debug.Log("Awaiting Messages?");
        }
#endif

        /// <summary>
        /// Import data from a file.
        /// </summary>
        /// <param name="importFile">The file to import data from.</param>
        /// <param name="renderFolder">The folder to render images to. If null, uses the path specified in the JSON file.</param>
        public void ImportCurrentScene(FileManager importFile, DirectoryManager renderFolder)
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

            ImportCurrentScene(json, renderFolder);
        }

        /// <summary>
        /// Import a scene from a json string.
        /// </summary>
        /// <param name="json">JSON holding data to import.</param>
        /// <param name="renderPath">The path to render the scene to. If null, reads the
        /// render path from file and uses that.</param>
        /// <returns>Whether the receiver should continue receiving data or exit.</returns>
        public bool ImportCurrentScene(string json, DirectoryManager renderPath = null)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("Empty json string provided. Will not attempt to deserialize.");
                return true;
            }

            Debug.Log("Beginning Import.");

            // TODO add objects to this list based on if they are new in importer.
            List<GameObject> importObjects = new List<GameObject>();
            Scene currentScene = SceneManager.GetActiveScene();
            currentScene.GetRootGameObjects(importObjects);
            importObjects.RemoveAll((obj) => obj == gameObject);

            if (importObjects == null || importObjects.Count == 0)
            {
                Debug.LogWarning("Empty object List.");
                return true;
            }

            Camera[] cameras = FindObjectsOfType<Camera>();
            List<CustomCamera> customCameras = new List<CustomCamera>();

            // add custom cameras to all cameras in the scene and save them
            foreach (Camera camera in cameras)
            {
                if (!camera.gameObject.TryGetComponent(out CustomCamera customCamera))
                {
                    customCamera = camera.gameObject.AddComponent<CustomCamera>();
                }
                customCameras.Add(customCamera);
            }

            if (cameras.Length == 0)
            {
                // If cam is empty, then no cameras were found.
                Debug.LogError("Missing Camera! Importer cannot render with no cameras.");
                return true;
            }

            foreach (GameObject importObject in importObjects)
            {
                importObject.transform.SetParent(transform, true);
            }

            Debug.Log("Deserializing...");

            try
            {
                bool failed = false;
                JsonSerializerSettings serializerSettings =
                new JsonSerializerSettings
                {
                    // log error if reached a problematic point
                    Error = delegate (object sender, ErrorEventArgs args)
                    {
                        if (args.CurrentObject == args.ErrorContext.OriginalObject)
                        {
                            Debug.LogError(args.ErrorContext.Error.Message);
                            args.ErrorContext.Handled = true;
                            failed = true;
                        }
                    }
                };

                // add check if blank state exists and return immediately
                // or replace blank state with null and add that as an exit now
                EURScene state = JsonConvert.DeserializeObject<EURScene>(json, serializerSettings);

                if (failed || state == null)
                {
                    Debug.Log(json);
                    Debug.Log("Failed to deserialize.");
                    return true;
                }

                if (!state.ContinueImporting)
                {
                    return false;
                }

                state.SceneRoot.UnpackData(transform);
                DateTime exportTimestamp = state.ExportDate;
                EURScene.CameraSettings settings = state.RendererSettings;

                // Reassign renderpath if override was provided
                settings.RenderDirectory = renderPath?.Path ?? settings.RenderDirectory;

                Debug.Log($"Imported state that was generated at { exportTimestamp }." +
                    $"Camera settings are:\n\t{settings.RenderDirectory}\n\t" +
                    $"Resolution: {settings.RenderSize.x}x{settings.RenderSize.y}");

                foreach (CustomCamera camera in customCameras)
                {
                    camera.RenderPath = settings.RenderDirectory;
                    camera.RenderImage(exportTimestamp, settings.RenderSize);
                }
                return true;
            }
            catch (JsonException je)
            {
                Debug.LogError($"Unexpected JSON Deserialization Error occurred!\n{je}");
                return true;
            }
            finally
            {
                // unparent in case new objects get added
                foreach (GameObject importObject in importObjects)
                {
                    importObject.transform.parent = null;
                }
            }
        }
    }
}
