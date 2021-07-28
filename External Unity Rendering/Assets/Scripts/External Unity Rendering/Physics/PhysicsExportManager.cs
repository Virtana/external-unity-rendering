using System.Collections;
using UnityEngine;

namespace ExternalUnityRendering
{
    public class PhysicsExportManager : MonoBehaviour
    {
        private static PhysicsExportManager _instance = null;

        public PhysicsArguments Arguments = null;

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
                Debug.LogError("Cannot have multiple instances of PhysicsExportManager");
                Destroy(this);
                return;
            }

            _instance = this;
#pragma warning restore
        }

        private void Start()
        {
            ExportScene exporter = FindObjectOfType<ExportScene>();
            if (exporter == null)
            {
                exporter = gameObject.AddComponent<ExportScene>();
            }
            exporter.Sender = new TcpIp.Client(Arguments.ReceiverPort, Arguments.ReceiverIpAddress);
            StartCoroutine(ExportLoop(exporter));
        }

        IEnumerator ExportLoop(ExportScene exporter)
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

            if (Arguments.ExportActions.HasFlag(ExportScene.PostExportAction.Transmit))
            {
                Debug.Log("Emptied queue and sending closing message.");
                if (Application.isBatchMode)
                {
                    exporter.Sender.FinishTransmissionsAndClose();
                }
                else
                {
                    while (!exporter.Sender.IsDone())
                    {
                        yield return null;
                    }
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

        private void OnApplicationQuit()
        {
            ExportScene exporter = FindObjectOfType<ExportScene>();
            if (exporter != null)
            {
                if (!exporter.Sender.IsDone())
                {
                    Debug.Log("Waiting for exporter to close.");
                    exporter.Sender.FinishTransmissionsAndClose();
                }
            }
        }
    }
}
