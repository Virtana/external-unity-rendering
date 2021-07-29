using System.Collections;
using System.Linq;
using System.Net;
using System.Text;

using ExternalUnityRendering.PathManagement;

using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
    /// <summary>
    /// Editor window for testing options and quick export settings.
    /// </summary>
    public class ExporterEditorMenu : EditorWindow
    {
        /// <summary>
        /// Number of times that the exporter will run.
        /// </summary>
        private int _exportCount = 10;

        /// <summary>
        /// The difference in unity time between two exported states.
        /// </summary>
        private int _exportDelay = 1000;

        /// <summary>
        /// The radius for the explosion
        /// </summary>
        private Exporter.PostExportAction _exportActions = Exporter.PostExportAction.Nothing;

        /// <summary>
        /// The path where scene states (json) will be saved if the write to file option is chosen.
        /// </summary>
        private string _exportFolder = null;

        private int _serverPort = 11000;

        private string _serverIpAddress = "localhost";

        /// <summary>
        /// The path where the external rendering instance should render its images to.
        /// </summary>
        private string _renderFolder = System.IO.Path.GetFullPath("../");

        /// <summary>
        /// The resolution of all the rendered images by the external renderer.
        /// </summary>
        private Vector2Int _renderResolution = new Vector2Int(1920, 1080);

        /// <summary>
        /// Scroll position for the current GUI Position.
        /// </summary>
        private Vector2 _scrollPosition = Vector2.zero;

        /// <summary>
        /// Create an <see cref="ExporterEditorMenu"/> and show it.
        /// </summary>
        [MenuItem("Scene State Exporter/Menu")]
        static void Init()
        {
            ExporterEditorMenu window = GetWindow<ExporterEditorMenu>();
            window.Show();
        }

        /// <summary>
        /// Function that is called every time the GUI needs to update. Defines the UI layout.
        /// </summary>
        private void OnGUI()
        {
            GUIStyle style = EditorStyles.label;
            EditorStyles.boldLabel.alignment = TextAnchor.MiddleCenter;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Exporter Settings",
                EditorStyles.boldLabel);

            #region Time Settings
            GUIContent label = new GUIContent("Number of Exports to perform: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _exportCount = EditorGUILayout.IntSlider(label, _exportCount, 1, 100);
            label = new GUIContent("Delay between exports: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _exportDelay = EditorGUILayout.IntSlider(label, _exportDelay, 100, 10000);
            #endregion

            #region Export Options
            GUIContent optionLabel = new GUIContent("Export Options: ");
            GUIContent optionListLabel =
                new GUIContent("None : Attempt to serialize but do nothing with the data.\n" +
                "Transmit: Attempt to transmit over TCP/IP.\n" +
                "WriteToFile: Write to file in a specified folder (or the persistent data path)." +
                "\nLog: Write to the console.");

            EditorGUIUtility.labelWidth = style.CalcSize(optionLabel).x;
            EditorGUILayout.LabelField(optionLabel, optionListLabel,
                EditorStyles.wordWrappedLabel);

            label = new GUIContent("How to export Scene State: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _exportActions = (Exporter.PostExportAction)
                EditorGUILayout.EnumFlagsField(label, _exportActions);

            if (_exportActions.HasFlag(Exporter.PostExportAction.WriteToFile))
            {
                if (GUILayout.Button("Select Export folder"))
                {
                    _exportFolder = EditorUtility.OpenFolderPanel(
                        "Select the folder to export the scene state to.", _exportFolder, "");
                }
            }
            if (_exportActions.HasFlag(Exporter.PostExportAction.Transmit))
            {
                label = new GUIContent("Server/Renderer IP address: ");
                EditorGUIUtility.labelWidth = style.CalcSize(label).x;
                _serverIpAddress = EditorGUILayout.TextField(label, _serverIpAddress);

                label = new GUIContent("Server/Renderer Port: ");
                EditorGUIUtility.labelWidth = style.CalcSize(label).x;
                int providedPort = EditorGUILayout.IntField(label, _serverPort);
                if (1023 < providedPort && providedPort < 65535)
                {
                    _serverPort = providedPort;
                }
            }
            #endregion

            EditorGUILayout.Space();

            #region Renderer Settings
            EditorGUILayout.LabelField("External Renderer Settings", EditorStyles.boldLabel);

            if (GUILayout.Button("Select Render Instance Output Folder"))
            {
                _renderFolder = EditorUtility.OpenFolderPanel(
                    "Select the folder to export the final renders to.", _renderFolder, "");
            }

            label = new GUIContent("Renderer Output Resolution: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _renderResolution =
                EditorGUILayout.Vector2IntField(label, _renderResolution);
            #endregion


            GUILayout.FlexibleSpace();
            EditorGUILayout.Space();

            if (GUILayout.Button("Begin exporting."))
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorUtility.DisplayDialog("Start Playmode", "The Unity Editor " +
                        "is not playing. Due to Unity Limitations, Playmode must be " +
                        "started manually before export.", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(_exportFolder))
                {
                    _exportFolder = Application.persistentDataPath;
                }

                DirectoryManager exportFolder = new DirectoryManager(_exportFolder);
                if (_exportActions.HasFlag(Exporter.PostExportAction.WriteToFile)
                    && exportFolder.Path == Application.persistentDataPath)
                {
                    EditorUtility.DisplayDialog("Invalid Json path", "Failed to validate export " +
                        "json directory.", "OK");
                    return;
                }

                DirectoryManager renderOutputFolder = new DirectoryManager(_renderFolder);
                if (renderOutputFolder.Path == Application.persistentDataPath)
                {
                    EditorUtility.DisplayDialog("Invalid render path", "Failed to get access to " +
                        "the render folder.", "OK");
                    return;
                }

                IPAddress serverIP;

                if (_serverIpAddress.ToLowerInvariant() == "localhost"
                    || string.IsNullOrWhiteSpace(_serverIpAddress) || _serverIpAddress == "::1"
                    || _serverIpAddress == "[::1]")
                {
                    serverIP = IPAddress.Loopback;
                }
                else
                {
                    try
                    {
                        if (!(_serverIpAddress.Count((c) => c == '.') == 3
                            || _serverIpAddress.Count((c) => c == ':') > 2))
                        {
                            throw new System.FormatException("An invalid IP address was specified");
                        }
                        serverIP = IPAddress.Parse(_serverIpAddress);
                    }
                    catch (System.FormatException)
                    {
                        EditorUtility.DisplayDialog("Invalid IP Address.", "Could not parse IP " +
                            $"Address \"{_serverIpAddress}\". Please ensure that this is a valid " +
                            "IP address.", "OK");
                        return;
                    }

                    if (!IPAddress.IsLoopback(serverIP) && !EditorUtility.DisplayDialog(
                        "Non-loopback IP Address.", $"The IP address provided {serverIP} is not " +
                        $"a loopback IP address. The render path {_renderFolder} may not be " +
                        "valid for the location. Do you wish to continue?", "Yes", "No"))
                    {
                        return;
                    }
                }

                StringBuilder options = new StringBuilder();
                options.AppendLine($"Number of Exports: {_exportCount}");
                options.AppendLine($"Delay between Exports: {_exportDelay}");
                options.AppendLine($"Scene State Export: {_exportActions}");
                if (_exportActions.HasFlag(Exporter.PostExportAction.WriteToFile))
                {
                    options.AppendLine($"Json Export Folder: {_exportFolder}");
                }
                if (_exportActions.HasFlag(Exporter.PostExportAction.Transmit))
                {
                    options.AppendLine($"Renderer/Server: {_serverIpAddress}:{_serverPort}");
                }

                options.AppendLine("Renderer Output Folder: " + _renderFolder);
                options.AppendLine("Renderer Output Resolution: " +
                    $"{_renderResolution.x}x{_renderResolution.y}");

                if (EditorUtility.DisplayDialog("Confirm your choices",
                    options.ToString(), "Yes", "No"))
                {
                    _exportLoop = this.StartCoroutine(ExportLoop());
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private EditorCoroutine _exportLoop = null;

        IEnumerator ExportLoop()
        {
            Exporter exporter = FindObjectOfType<Exporter>();
            if (exporter == null)
            {
                exporter = new GameObject().AddComponent<Exporter>();
            }

            float delaySeconds = _exportDelay / 1000;
            int progressID = Progress.Start("Exporting...", $"Exporting {_exportCount} scenes " +
                $"with a delay of {_exportDelay} ms and performing {_exportActions}",
                Progress.Options.Synchronous);

            Progress.RegisterCancelCallback(progressID, () =>
            {
                EditorCoroutineUtility.StopCoroutine(_exportLoop);
                return true;
            });

            for (int i = 0; i < _exportCount; i++)
            {
                if (!Application.isPlaying)
                {
                    Progress.Finish(progressID, Progress.Status.Canceled);
                    yield break;
                }
                Progress.Report(progressID, (float)i / _exportCount,
                    $"Exported {i + 1} scene states so far.");
                exporter.ExportCurrentScene(_exportActions, _renderResolution, _renderFolder);
                yield return new EditorWaitForSeconds(delaySeconds);
            }
            Progress.Finish(progressID, Progress.Status.Succeeded);
        }
    }
}
