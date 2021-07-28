using ExternalUnityRendering.TcpIp;
using UnityEngine;

namespace ExternalUnityRendering
{
    /// <summary>
    /// Manager for importing the Scene states in batchmode.
    /// </summary>
    class ImporterManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton representing the current <see cref="ImporterManager"/>.
        /// </summary>
        private static ImporterManager _instance = null;

        /// <summary>
        /// The <see cref="ImporterArguments"/> for this manager.
        /// </summary>
        public ImporterArguments Arguments = null;

        /// <summary>
        /// Check if in Editor or _instance is not this, and if true, destroy this instance.
        /// </summary>
        private void Awake()
        {
#if UNITY_EDITOR
            Destroy(this);
            return;
#endif
#pragma warning disable CS0162 // unreachable code warning when UNITY_EDITOR is set
            if (_instance != null && _instance != this)
            {
                Debug.LogError("Cannot have multiple instances of RendererImportManager.");
                Destroy(this);
                return;
            }
            _instance = this;
#pragma warning restore
        }

        /// <summary>
        /// Get the <see cref="Importer"/> currently in the scene and if it does not exist, create
        /// it. Then start automatically listening for messages to import. Performs blocking import
        /// and exit when complete.
        /// </summary>
        private void Start()
        {
            Importer importer = FindObjectOfType<Importer>();
            if (importer == null)
            {
                GameObject obj = new GameObject
                {
                    name = "SceneStateImporter"
                };

                importer = obj.AddComponent<Importer>();
            }

            Server receiver = new Server(Arguments.ReceiverPort, Arguments.ReceiverIpAddress);
            Debug.Log("Awaiting Messages...");

            int importCount = 0;
            receiver.ProcessCallback((state) =>
            {
                bool continueImporting = importer.ImportCurrentScene(state, Arguments.RenderDirectory);
                System.Console.WriteLine($"Imported {++importCount} scenes so far.");
                return continueImporting;
            });

            Debug.Log($"Saved a total of {importCount - 2} scenes to disk. (false positives occur due to open and close signal).");

            RenderTexture.active = null;
            Application.Quit(0);
        }
    }
}
