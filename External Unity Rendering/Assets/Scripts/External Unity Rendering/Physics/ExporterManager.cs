using System.Collections;
using UnityEngine;

namespace ExternalUnityRendering
{
    public class ExporterManager : MonoBehaviour
    {
        /// <summary>
        /// The singleton representing the current <see cref="ExporterManager"/>.
        /// </summary>
        private static ExporterManager _instance = null;

        /// <summary>
        /// The <see cref="ExporterArguments"/> for this manager.
        /// </summary>
        public ExporterArguments Arguments = null;

        /// <summary>
        /// Check if in Editor or _instance is not this, and if true, destroy this instance.
        /// </summary>
        private void Awake()
        {
#if UNITY_EDITOR
            // Cannot have this in the editor.
            // Use the editor scripts instead.
            Destroy(this);
            return;
#endif
#pragma warning disable CS0162 // unreachable code warning when UNITY_EDITOR is set
            if (_instance != null && _instance != this)
            {
                Debug.LogError($"Cannot have multiple instances of {nameof(ExporterManager)}");
                Destroy(this);
                return;
            }

            _instance = this;
#pragma warning restore
        }

        /// <summary>
        /// Get the <see cref="Exporter"/> currently in the scene and if it does not exist, create
        /// it. Then start automatically exporting according to the timings in
        /// <see cref="Arguments"/>.
        /// </summary>
        private void Start()
        {
            Exporter exporter = FindObjectOfType<Exporter>();
            if (exporter == null)
            {
                exporter = gameObject.AddComponent<Exporter>();
            }
            if (Arguments.ExportActions.HasFlag(Exporter.PostExportAction.Transmit))
            {
                exporter.Sender = new TcpIp.Client(Arguments.ReceiverPort,
                    Arguments.ReceiverIpAddress);
            }
            StartCoroutine(ExportLoop(exporter));
        }

        /// <summary>
        /// Export the scene as specified by <see cref="Arguments"/>. If the application is running
        /// in batchmode, quit when finished.
        /// </summary>
        /// <param name="exporter">The exporter to export the current scene with.</param>
        /// <returns>IEnumerator to use for <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/>.
        /// </returns>
        IEnumerator ExportLoop(Exporter exporter)
        {
            exporter.ExportFolder = Arguments.JsonPath;
            float delaySeconds = Arguments.MillisecondsDelay / 1000f;
            for (int i = 0; i < Arguments.Exports && Application.isPlaying; i++)
            {
                exporter.ExportCurrentScene(Arguments.ExportActions, Arguments.RenderResolution,
                    Arguments.RenderPath);

                Debug.Log($"Exported {i+1} out of {Arguments.Exports}.");

                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            if (Arguments.ExportActions.HasFlag(Exporter.PostExportAction.Transmit))
            {
                Debug.Log("Emptied queue and sending closing message.");
                if (Application.isBatchMode)
                {
                    exporter.Sender.Close();
                }
                else
                {
                    yield return new WaitUntil(() => exporter.Sender.IsDone);
                }
            }

            if (Application.isBatchMode)
            {
                Debug.Log("Exiting Physics Instance...");
                Application.Quit(0);
            }
            else
            {
                Debug.Log("Automatic exporting is complete. Keyboard export not yet supported.");
            }
        }

        /// <summary>
        /// When the application is exiting, wait for the exporter to finish sending if data is
        /// still queued.
        /// </summary>
        private void OnApplicationQuit()
        {
            Exporter exporter = FindObjectOfType<Exporter>();
            if (exporter != null
                && Arguments.ExportActions.HasFlag(Exporter.PostExportAction.Transmit)
                && !exporter.Sender.IsDone)
            {
                Debug.Log("Waiting for exporter to close.");
                exporter.Sender.Close();
            }
        }
    }
}
