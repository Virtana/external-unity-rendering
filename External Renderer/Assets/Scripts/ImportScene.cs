using ExternalUnityRendering.CameraUtilites;
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
    public class ImportScene : MonoBehaviour
    {
        // Represents when the scene was exported.
        // if null, then no import has occured
        public DateTime? ExportTimestamp;

        private void Awake()
        {
            // Set timeScale to 0. Scene must always be static.
            // will be updated on each import
            Time.timeScale = 0;

            Debug.Log("Awaiting Messages?");
            Receiver client = new Receiver();

            // non blocking async function
            client.ReceiveMessages((state) => ImportCurrentScene(state));
        }

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

        // By default returns true. If fail, exporter just needs to ping server
        // and check if connection is refused. if not, then send a mostly blank object with
        // continue importing as false
        public bool ImportCurrentScene(string json, DirectoryManager renderPath = null)
        {
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
                        }
                    }
                };

                SceneState state = JsonConvert.DeserializeObject<SceneState>(json, serializerSettings);

                if (state == null)
                {
                    Debug.LogError("Failed to deserialize!");
                    return true;
                }

                state.SceneRoot.UnpackData(transform);
                ExportTimestamp = state.ExportDate;
                SceneState.CameraSettings settings = state.RendererSettings;

                // Reassign renderpath if override was provided
                settings.RenderDirectory = renderPath?.Path ?? settings.RenderDirectory;


                bool continueImporting = state.ContinueImporting;

                Debug.LogFormat($"Imported state that was generated at { ExportTimestamp }." +
                    $"Camera settings are:\n\t{settings.RenderDirectory}\n\t" +
                    $"Resolution: {settings.RenderSize.x}x{settings.RenderSize.y}");

                foreach (CustomCamera camera in customCameras)
                {
                    camera.RenderPath = settings.RenderDirectory;
                    camera.RenderImage(settings.RenderSize);
                }
                return continueImporting;
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
