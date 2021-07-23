using ExternalUnityRendering.TcpIp;
using UnityEngine;

namespace ExternalUnityRendering
{
    class RendererImportManager : MonoBehaviour
    {
        private static RendererImportManager _instance = null;

        public RendererArguments Arguments = null;

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

        private void Start()
        {
            ImportScene importer = FindObjectOfType<ImportScene>();
            if (importer == null)
            {
                GameObject obj = new GameObject
                {
                    name = "SceneStateImporter"
                };

                importer = obj.AddComponent<ImportScene>();
            }

            Receiver receiver = new Receiver(Arguments.ReceiverPort, Arguments.ReceiverIpAddress);
            Debug.Log("Awaiting Messages...");

            int importCount = 0;
            receiver.ProcessCallback((state) =>
            {
                bool continueImporting = importer.ImportCurrentScene(state);
                System.Console.WriteLine($"Imported {++importCount} scenes so far.");
                return continueImporting;
            });

            Debug.Log($"Saved a total of {importCount - 2} scenes to disk. (false positives occur due to open and close signal).");

            RenderTexture.active = null;
            Application.Quit(0);
        }
    }
}
