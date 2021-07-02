using ExternalUnityRendering.CameraUtilites;
using ExternalUnityRendering.PathManagement;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ExternalUnityRendering.UnityEditor
{
#if PHYSICS && UNITY_EDITOR
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
        private int _millisecondsDelay = 1000;

        /// <summary>
        /// The radius for the explosion
        /// </summary>
        private ExportScene.PostExportAction _exportType = ExportScene.PostExportAction.Nothing;

        /// <summary>
        /// The path where scene states (json) will be saved if the write to file
        /// option is chosen.
        /// </summary>
        private string _exportFolder = System.IO.Path.GetFullPath("../");

        /// <summary>
        /// Whether to use an explosion (true) or randomly applied force (false).
        /// </summary>
        private bool _useExplosion = true;

        /// <summary>
        /// The point in worldspace coordinates where the explosive force emanates from.
        /// </summary>
        private Vector3 _explosionOrigin = Vector3.zero;

        /// <summary>
        /// The radius of the explosion. Outside of this radius, the explosion has no
        /// effect.
        /// </summary>
        private float _explosionRadius = 10;

        /// <summary>
        /// The force of the explosion.
        /// </summary>
        private float _explosionForce = 1;

        /// <summary>
        /// A modifier to the y-axis position of the source of the explosion. Used to
        /// offset the source of the explosion to give the effect of lifting objects.
        /// </summary>
        private float _explosionUpwardsModifier = 10;

        /// <summary>
        /// The type of random force to apply to the objects in the sceme.
        /// </summary>
        private ForceMode _forceType = ForceMode.Impulse;

        /// <summary>
        /// Each component of the directional force will be a random value
        /// between these two limits.
        /// </summary>
        private Vector2 _minMaxForce = new Vector2(-10, 10);

        /// <summary>
        /// Whether the exporter should render images. Meant for testing only.
        /// </summary>
        private bool _exportTestRenders = false;

        /// <summary>
        /// The path where exporter images would be rendered to.
        /// </summary>
        private string _renderFolder = System.IO.Path.GetFullPath("../");

        /// <summary>
        /// The resolution of all of the rendered images by the exporter.
        /// </summary>
        private Vector2Int _renderResolution = new Vector2Int(1920, 1080);

        /// <summary>
        /// The path where the external rendering instance should render its images to.
        /// </summary>
        private string _rendererOutputFolder = System.IO.Path.GetFullPath("../");

        /// <summary>
        /// The resolution of all the rendered images by the external renderer.
        /// </summary>
        private Vector2Int _rendererOutputResolution = new Vector2Int(1920, 1080);

        /// <summary>
        /// Initiator for the window to appear.
        /// </summary>
        [MenuItem("Exporter Testing/Menu")]
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
            // TODO make scrollable between top label and button
            // HACK manually resizing each element

            // Get/set some values needed for the GUI
            GUIStyle style = EditorStyles.label;
            EditorStyles.boldLabel.alignment = TextAnchor.MiddleCenter;

            EditorGUILayout.LabelField("Configure the following before exporting.",
                EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Exporter & Testing Settings",
                EditorStyles.boldLabel);

            GUIContent label = new GUIContent("Number of Exports to perform: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _exportCount = EditorGUILayout.IntSlider(label, _exportCount, 1, 100);
            label = new GUIContent("Delay between exports: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _millisecondsDelay = EditorGUILayout.IntSlider(label, _millisecondsDelay, 100, 10000);

            GUIContent optionLabel = new GUIContent("Export Options: ");
            GUIContent optionListLabel =
                new GUIContent("None : Attempt to serialize but do nothing with the data.\n" +
                "Transmit: Attempt to transmit over TCP/IP.\n" +
                "WriteToFile: Write to file in a specified folder (or the persistent data path).\n" +
                "Log: Write to the console.");

            EditorGUIUtility.labelWidth = style.CalcSize(optionLabel).x;
            EditorGUILayout.LabelField(optionLabel, optionListLabel,
                EditorStyles.wordWrappedLabel);

            label = new GUIContent("How to export Scene State: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _exportType = (ExportScene.PostExportAction)
                EditorGUILayout.EnumFlagsField(label, _exportType);

            if ((_exportType & ExportScene.PostExportAction.WriteToFile)
                == ExportScene.PostExportAction.WriteToFile)
            {
                if (GUILayout.Button("Select Export folder"))
                {
                    _exportFolder = EditorUtility.OpenFolderPanel(
                        "Select the folder to export the scene state to.", _exportFolder, "");
                }
            }

            EditorGUILayout.Space();

            _useExplosion = EditorGUILayout.BeginToggleGroup("Use Explosion", _useExplosion);
            label = new GUIContent("Explosion Source: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _explosionOrigin = EditorGUILayout.Vector3Field(label, _explosionOrigin);
            label = new GUIContent("Explosion Radius: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _explosionRadius = EditorGUILayout.Slider(label, _explosionRadius, 5, 100);
            label = new GUIContent("Explosive Force: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _explosionForce = EditorGUILayout.Slider(label, _explosionForce, 10, 500);
            label = new GUIContent("Upwards Force Modifier: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _explosionUpwardsModifier =
                EditorGUILayout.Slider(label, _explosionUpwardsModifier, 10, 500);
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();

            _useExplosion = !EditorGUILayout.BeginToggleGroup("Use Random Force", !_useExplosion);
            label = new GUIContent("Force Type: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _forceType = (ForceMode)EditorGUILayout.EnumPopup(label, _forceType);

            label = new GUIContent("Random Force Limits: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            GUIContent limits =
                new GUIContent($"Min: {_minMaxForce.x:.0} Max: {_minMaxForce.y:.0}");
            EditorGUILayout.LabelField(label, limits);

            EditorGUILayout.MinMaxSlider(ref _minMaxForce.x, ref _minMaxForce.y, -100, 100);
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();

            _exportTestRenders =
                EditorGUILayout.BeginToggleGroup("Export Renders", _exportTestRenders);
            if (GUILayout.Button("Select Render Folder"))
            {
                _renderFolder = EditorUtility.OpenFolderPanel(
                    "Select the folder to export the renders to.", _renderFolder, "");
            }

            label = new GUIContent("Render Resolution: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _renderResolution = EditorGUILayout.Vector2IntField(label, _renderResolution);
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("External Renderer Settings", EditorStyles.boldLabel);

            if (GUILayout.Button("Select Render Instance Output Folder"))
            {
                _rendererOutputFolder = EditorUtility.OpenFolderPanel(
                    "Select the folder to export the final renders to.", _rendererOutputFolder, "");
            }

            label = new GUIContent("Renderer Output Resolution: ");
            EditorGUIUtility.labelWidth = style.CalcSize(label).x;
            _rendererOutputResolution =
                EditorGUILayout.Vector2IntField(label, _rendererOutputResolution);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Explode and begin exporting."))
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorUtility.DisplayDialog("Start Playmode", "The Unity Editor " +
                        "is not playing. Due to Unity Limitations, Playmode must be " +
                        "started manually before export.", "OK");
                    return;
                }
                // validate options and trigger explode
                // also have confirm dialog showing the options

                if (string.IsNullOrEmpty(_exportFolder))
                {
                    _exportFolder = System.IO.Directory.GetCurrentDirectory();
                }

                DirectoryManager exportFolder = new DirectoryManager(_exportFolder);
                if (((_exportType & ExportScene.PostExportAction.WriteToFile)
                    == ExportScene.PostExportAction.WriteToFile)
                    && exportFolder.Path == Application.persistentDataPath)
                {
                    Debug.Log("Failed to get access to the export folder.");
                    return;
                }

                DirectoryManager renderFolder = new DirectoryManager(_renderFolder);
                if (_exportTestRenders && renderFolder.Path == Application.persistentDataPath)
                {
                    Debug.Log("Failed to get access to the render folder.");
                    return;
                }

                DirectoryManager renderOutputFolder = new DirectoryManager(_rendererOutputFolder);
                if (_exportTestRenders && renderOutputFolder.Path == Application.persistentDataPath)
                {
                    Debug.Log("Failed to get access to the renderer ouput folder.");
                    return;
                }

                StringBuilder options = new StringBuilder();
                options.AppendLine("Number of Exports: " + _exportCount);
                options.AppendLine("Delay between Exports: " + _millisecondsDelay);
                options.AppendLine("Scene State Export: " + _exportType);
                if ((_exportType & ExportScene.PostExportAction.WriteToFile)
                    == ExportScene.PostExportAction.WriteToFile)
                {
                    options.AppendLine("Export Folder: " + _exportFolder);
                }

                if (_useExplosion)
                {
                    options.AppendLine("Explosion Source: " + _explosionOrigin);
                    options.AppendLine("Explosion Radius: " + _explosionRadius);
                    options.AppendLine("Explosive Force: " + _explosionForce);
                    options.AppendLine("Explosive Force Upwards Modifier: "
                        + _explosionUpwardsModifier);
                }
                else
                {
                    options.AppendLine("Force Type: " + _forceType);
                    options.AppendLine("Force Limits:\n\tMin:" +
                        $"{_minMaxForce.x}\n\tMax: {_minMaxForce.y}");
                }

                options.AppendLine("Test Render Images: " + _exportTestRenders);
                if (_exportTestRenders)
                {
                    options.AppendLine("Test Render Folder: " + _renderFolder);
                    options.AppendLine("Test Render Resolution: " +
                        $"{_renderResolution.x}x{_renderResolution.y}");
                }

                options.AppendLine("Renderer Output Folder: " + _renderFolder);
                options.AppendLine("Renderer Output Resolution: " +
                    $"{_renderResolution.x}x{_renderResolution.y}");

                if (EditorUtility.DisplayDialog("Confirm your choices",
                    options.ToString(), "Yes", "No"))
                {
                    ExplodeAndRecord();
                }
            }
        }

        /// <summary>
        /// Create an exportScene object if none exists, apply the chosen force
        /// (explosion or normal), and export the scene a chosen number of times
        /// at a specified interval.
        /// </summary>
        private async void ExplodeAndRecord()
        {
            // Grab the exporter if it exists
            ExportScene export = FindObjectOfType<ExportScene>();
            if (export == null)
            {
                GameObject gameObject = new GameObject
                {
                    name = "Exporter-" + GUID.Generate()
                };
                export = gameObject.AddComponent<ExportScene>();
            }

            // All folders should be valid if being used.
            export.ExportFolder = _exportFolder;

            Camera[] cameras = FindObjectsOfType<Camera>();

            if (cameras.Length == 0)
            {
                // If cam is empty, then no cameras were found.
                Debug.LogError("Missing Camera! Importer cannot render from this.");
                return;
            }

            List<CustomCamera> customCameras = new List<CustomCamera>();

            if (_exportTestRenders)
            {
                foreach (Camera camera in cameras)
                {
                    // add custom cameras if they don't already have and save them
                    if (!camera.TryGetComponent(out CustomCamera customCamera))
                    {
                        customCamera = camera.gameObject.AddComponent<CustomCamera>();
                    }
                    customCamera.RenderPath = _renderFolder;
                    customCameras.Add(customCamera);
                }
            }

            Collider[] colliders;
            if (_useExplosion)
            {
                // Using overlap sphere to avoid excess calculation. Explosion has
                // extremely little effect outside of the radius, so this will cut
                // the unnecessary calculations.
                colliders = Physics.OverlapSphere(_explosionOrigin, _explosionRadius);
            }
            else
            {
                colliders = FindObjectsOfType<Collider>();
            }

            foreach (Collider hit in colliders)
            {
                // addforce etc has no effect on inactive GameObjects
                if (!hit.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // Handle non-convex mesh collider with non-kinematic rigidbody error
                if (hit.gameObject.TryGetComponent(out MeshCollider _))
                {
                    // meshcolliders are used with items that should be static
                    // in this test so skip for now otherwise assign out meshcollider
                    // and set mesh.convex to true
                    continue;
                }

                if (!hit.gameObject.TryGetComponent(out Rigidbody rb))
                {
                    rb = hit.gameObject.AddComponent<Rigidbody>();
                }

                rb.mass = 10;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                if (_useExplosion)
                {
                    rb.AddExplosionForce(_explosionForce, _explosionOrigin,
                        _explosionRadius, _explosionUpwardsModifier, ForceMode.Impulse);
                }
                else
                {
                    rb.AddForce(new Vector3(
                            Random.Range(_minMaxForce.x, _minMaxForce.y),
                            Random.Range(_minMaxForce.x, _minMaxForce.y),
                            Random.Range(_minMaxForce.x, _minMaxForce.y)),
                        _forceType);
                }
            }

            // Keep running while not done and editor is running
            for (int i = 0; i < _exportCount && EditorApplication.isPlaying; i++)
            {
                export.ExportCurrentScene(_exportType, _rendererOutputResolution,
                    _rendererOutputFolder, true);

                // should do nothing if customcameras empty
                foreach (CustomCamera cam in customCameras)
                {
                    cam.RenderImage(_renderResolution);
                }

                // _millisecondsDelay is the amount of time that the physics system will
                // calculate for in between renders. Rendering is a blocking task that "freezes"
                // unity time, so this does not represent the real time between two exports.
                await Task.Delay(_millisecondsDelay);
            }

            Debug.Log("Finished Export Loop.");
        }
    }
#endif
}
