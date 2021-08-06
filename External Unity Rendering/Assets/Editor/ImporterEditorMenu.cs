using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

using ExternalUnityRendering.PathManagement;

using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExternalUnityRendering.UnityEditor
{
    /// <summary>
    /// Class for editor menu options
    /// </summary>
    public class ImporterEditorMenu : EditorWindow
    {
        public Importer SceneImporter = null;

        /// <summary>
        /// Port to receive the scene states on.
        /// </summary>
        private int _listeningPort = 11000;

        /// <summary>
        /// IP address to listen for scene states on.
        /// </summary>
        private string _listeningIpAddress = "localhost";

        /// <summary>
        /// The path where the external rendering instance should render its images to.
        /// </summary>
        private string _renderFolder = System.IO.Path.GetFullPath("../");

        /// <summary>
        /// Scroll position for the current GUI Position.
        /// </summary>
        private Vector2 _scrollPosition = Vector2.zero;

        /// <summary>
        /// The coroutine managing server importing.
        /// </summary>
        private EditorCoroutine _importerCoroutine = null;

        #region GUI Labels
        private const string _portLabel = "Listening Port: ";
        private const string _ipAddressLabel = "Listening IP address: ";
        private const string _renderPathLabel = "Select the folder to export the final renders to.";
        #endregion

        /// <summary>
        /// Editor Function to quickly add an importer to the scene.
        /// </summary>
        [MenuItem("External Rendering/Importer Menu")]
        private static void Init()
        {
            GetWindow<ImporterEditorMenu>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Manage Importer");
        }

        public void OnGUI()
        {
            // HACK using largest label to size all labels
            EditorGUIUtility.labelWidth = Mathf.Max(EditorStyles.label.CalcSize(
                new GUIContent(_renderPathLabel)).x, 0.25f * position.width);
            EditorStyles.boldLabel.alignment = TextAnchor.MiddleCenter;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Importer Settings", EditorStyles.boldLabel);

            #region Importer Options
            _listeningIpAddress = EditorGUILayout.TextField(_ipAddressLabel, _listeningIpAddress);
            int providedPort = EditorGUILayout.IntField(_portLabel, _listeningPort);
            if (1023 < providedPort && providedPort < 65535)
            {
                _listeningPort = providedPort;
            }

            EditorGUILayout.Space();
            #endregion

            #region Renderer Settings
            if (GUILayout.Button("Select Render Instance Output Folder"))
            {
                _renderFolder = EditorUtility.OpenFolderPanel(_renderPathLabel, _renderFolder,
                    "Renders");
            }

            EditorGUILayout.Space();
            #endregion

            #region Validate Options and Begin importing
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Launch Importer Server"))
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorUtility.DisplayDialog("Start Playmode", "The Unity Editor " +
                        "is not playing. Due to Unity Limitations, Playmode must be " +
                        "started manually before importer.", "OK");
                    return;
                }

                DirectoryManager renderFolder = new DirectoryManager(_renderFolder);
                if (renderFolder.Path == Application.persistentDataPath)
                {
                    EditorUtility.DisplayDialog("Invalid render path", "Failed to get access to " +
                        "the render folder.", "OK");
                    return;
                }

                IPAddress serverIP;

                if (_listeningIpAddress.ToLowerInvariant() == "localhost"
                    || string.IsNullOrWhiteSpace(_listeningIpAddress)
                    || _listeningIpAddress == "::1" || _listeningIpAddress == "[::1]")
                {
                    serverIP = IPAddress.Loopback;
                }
                else
                {
                    try
                    {
                        if (!(_listeningIpAddress.Count((c) => c == '.') == 3
                            || _listeningIpAddress.Count((c) => c == ':') > 2))
                        {
                            throw new System.FormatException("An invalid IP address was specified");
                        }
                        serverIP = IPAddress.Parse(_listeningIpAddress);
                    }
                    catch (System.FormatException)
                    {
                        EditorUtility.DisplayDialog("Invalid IP Address.", "Could not parse IP " +
                            $"Address \"{_listeningIpAddress}\". Please ensure that this is a " +
                            $"valid IP address.", "OK");
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

                if (EditorUtility.DisplayDialog("Confirm your choices", "Listening IP Address: " +
                    $"{_listeningIpAddress}:{_listeningPort}\nRender Path: {_renderFolder}",
                    "Yes", "No"))
                {
                    if (SceneImporter == null)
                    {
                        SceneImporter = new GameObject("Importer").AddComponent<Importer>();
                    }
                    _importerCoroutine = this.StartCoroutine(LaunchImporter(renderFolder));
                }
            }
            #endregion

            EditorGUILayout.EndScrollView();
        }

        private IEnumerator LaunchImporter(DirectoryManager renderPath)
        {
            TcpIp.Server receiver = new TcpIp.Server(_listeningPort, _listeningIpAddress);
            Debug.Log("Awaiting Messages...");

            int importCount = 0;
            Task serverRunning =
                receiver.ProcessCallbackAsync((state) =>
                {
                    bool continueImporting = SceneImporter.ImportCurrentScene(state, renderPath);
                    System.Console.WriteLine($"Imported {++importCount} scenes so far.");
                    return continueImporting;
                });
            while (Application.isPlaying && !serverRunning.IsCompleted)
            {
                yield return null;
            }

            Debug.Log($"Saved a total of {importCount - 1} scenes to disk. (false positive " +
                "occurs due to close signal).");
            RenderTexture.active = null;
        }
    }

    [CustomEditor(typeof(Importer))]
    public class ImporterInspectorMenu : Editor
    {
        private ImporterEditorMenu editorMenu;
        public override VisualElement CreateInspectorGUI()
        {
            editorMenu = EditorWindow.GetWindow<ImporterEditorMenu>();
            return base.CreateInspectorGUI();
        }
        public override void OnInspectorGUI()
        {
            editorMenu.SceneImporter = target as Importer;
            editorMenu.OnGUI();
        }
    }
}
