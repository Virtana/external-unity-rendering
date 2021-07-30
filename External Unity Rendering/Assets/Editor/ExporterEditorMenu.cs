using System.Collections;
using System.Linq;
using System.Net;
using System.Text;

using ExternalUnityRendering.PathManagement;

using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExternalUnityRendering.UnityEditor
{
    /// <summary>
    /// Editor window for testing options and quick export settings.
    /// </summary>
    public class ExporterEditorMenu : EditorWindow
    {
        public Exporter SceneExporter = null;

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

        /// <summary>
        /// Port to send the scene states to.
        /// </summary>
        private int _serverPort = 11000;

        /// <summary>
        /// IP address to send scene states to.
        /// </summary>
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
        /// Whether to send the closing signal after finished exporting. Does not affect cancelled
        /// export loops.
        /// </summary>
        private bool _sendClosingMsg = false;

        /// <summary>
        /// The loop that manages the exporting.
        /// </summary>
        private EditorCoroutine _exportLoop = null;

        #region GUI Labels
        private const string _exportCountLabel = "Number of Exports to perform: ";
        private const string _exportDelayLabel = "Delay between exports: ";
        private const string _exportOptionsExplanationLabel = "Export Options: ";
        private const string _exportOptionsExplanation = "None: Attempt to serialize but do " +
            "nothing with the data.\nTransmit: Attempt to transmit over TCP/IP.\nWriteToFile: " +
            "Write to file in a specified folder (or the persistent data path).\nLog: Write to " +
            "the console.";
        private const string _exportOptionsLabel = "How to export Scene State: ";
        private const string _ipAddressLabel = "Server IP address: ";
        private const string _portLabel = "Server Port: ";
        private const string _sendClosingMsgLabel = "Send closing message: ";
        private const string _outputResLabel = "Renderer Output Resolution: ";
        #endregion

        /// <summary>
        /// Create an <see cref="ExporterEditorMenu"/> and show it.
        /// </summary>
        [MenuItem("Scene State Exporter/Menu")]
        private static void Init()
        {
            GetWindow<ExporterEditorMenu>().Show();
        }

        private void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ReloadVariables;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= ReloadVariables;
        }

        /// <summary>
        /// Refresh all of the editor variables.
        /// </summary>
        private void ReloadVariables()
        {
            minSize =
                new Vector2(EditorStyles.label.CalcSize(new GUIContent(_exportCountLabel)).x * 2,
                minSize.y);
            _exportCount = 10;
            _exportDelay = 1000;
            _exportActions = Exporter.PostExportAction.Nothing;
            _exportFolder = null;
            _serverPort = 11000;
            _serverIpAddress = "localhost";
            _renderFolder = System.IO.Path.GetFullPath("../");
            _renderResolution = new Vector2Int(1920, 1080);
            _scrollPosition = Vector2.zero;
            _sendClosingMsg = false;
            _exportLoop = null;
        }

        /// <summary>
        /// Function that is called every time the GUI needs to update. Defines the UI layout.
        /// </summary>
        public void OnGUI()
        {
            // HACK using largest label to size all labels
            EditorGUIUtility.labelWidth = Mathf.Max(EditorStyles.label.CalcSize(
                new GUIContent(_exportCountLabel)).x, 0.25f * position.width);
            EditorStyles.boldLabel.alignment = TextAnchor.MiddleCenter;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Exporter Settings", EditorStyles.boldLabel);

            #region Time Settings
            _exportCount = EditorGUILayout.IntSlider(_exportCountLabel, _exportCount, 1, 100);
            _exportDelay = EditorGUILayout.IntSlider(_exportDelayLabel, _exportDelay, 100, 10000);
            EditorGUILayout.Space();
            #endregion

            #region Export Options
            EditorGUILayout.LabelField(_exportOptionsExplanationLabel, _exportOptionsExplanation,
                EditorStyles.wordWrappedLabel);
            _exportActions = (Exporter.PostExportAction)EditorGUILayout.EnumFlagsField(
                _exportOptionsLabel, _exportActions);
            if (_exportActions.HasFlag(Exporter.PostExportAction.WriteToFile) && GUILayout.Button("Select Export folder"))
            {
                _exportFolder = EditorUtility.OpenFolderPanel(
                    "Select the folder to export the scene state to.", _exportFolder, "");
            }
            if (_exportActions.HasFlag(Exporter.PostExportAction.Transmit))
            {
                _serverIpAddress = EditorGUILayout.TextField(_ipAddressLabel, _serverIpAddress);
                int providedPort = EditorGUILayout.IntField(_portLabel, _serverPort);
                if (1023 < providedPort && providedPort < 65535)
                {
                    _serverPort = providedPort;
                }
                _sendClosingMsg = EditorGUILayout.Toggle(_sendClosingMsgLabel, _sendClosingMsg);
            }
            EditorGUILayout.Space();
            #endregion

            #region Renderer Settings
            EditorGUILayout.LabelField("External Renderer Settings", EditorStyles.boldLabel);

            if (GUILayout.Button("Select Render Instance Output Folder"))
            {
                _renderFolder = EditorUtility.OpenFolderPanel(
                    "Select the folder to export the final renders to.", _renderFolder, "Renders");
            }

            _renderResolution = EditorGUILayout.Vector2IntField(_outputResLabel, _renderResolution);
            EditorGUILayout.Space();
            #endregion

            #region Validate Options and Begin exporter
            GUILayout.FlexibleSpace();

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
                    if (SceneExporter == null)
                    {
                        SceneExporter = FindObjectOfType<Exporter>();
                        if (SceneExporter == null)
                        {
                            SceneExporter = new GameObject().AddComponent<Exporter>();
                        }
                    }

                    _exportLoop = this.StartCoroutine(ExportLoop());
                }
            }
            #endregion

            EditorGUILayout.EndScrollView();
        }

        public IEnumerator ExportLoop()
        {
            SceneExporter.Sender = new TcpIp.Client(_serverPort, _serverIpAddress);

            float delaySeconds = _exportDelay / 1000;
            int progressID = Progress.Start("Exporting...", $"Exporting {_exportCount} scenes " +
                $"with a delay of {_exportDelay} ms and performing {_exportActions}",
                Progress.Options.Synchronous | Progress.Options.Sticky);

            Progress.RegisterCancelCallback(progressID, () =>
            {
                if (_sendClosingMsg)
                {
                    Debug.LogWarning("Cancelled export loop. Will not send closing message.");
                }
                EditorCoroutineUtility.StopCoroutine(_exportLoop);
                return true;
            });

            for (int i = 0; i < _exportCount; i++)
            {
                if (!Application.isPlaying)
                {
                    Progress.Cancel(progressID);
                    yield break;
                }
                Progress.Report(progressID, (float)i / _exportCount,
                    $"Exported {i} scene states so far.");
                SceneExporter.ExportCurrentScene(_exportActions, _renderResolution, _renderFolder);
                yield return new EditorWaitForSeconds(delaySeconds);
            }

            Progress.Report(progressID, 1f, $"Exported {_exportCount} scene states so far.");
            if (_sendClosingMsg)
            {
                System.Threading.Tasks.Task close = SceneExporter.Sender.CloseAsync();
                yield return new WaitUntil(() => close.IsCompleted); // non blocking wait for close
            }

            Progress.Finish(progressID);
        }
    }

    [CustomEditor(typeof(Exporter))]
    public class ExporterInspectorMenu : Editor
    {
        private ExporterEditorMenu editorMenu;
        public override VisualElement CreateInspectorGUI()
        {
            editorMenu = EditorWindow.GetWindow<ExporterEditorMenu>();
            return base.CreateInspectorGUI();
        }
        public override void OnInspectorGUI()
        {
            editorMenu.SceneExporter = target as Exporter;
            editorMenu.OnGUI();
        }
    }
}
