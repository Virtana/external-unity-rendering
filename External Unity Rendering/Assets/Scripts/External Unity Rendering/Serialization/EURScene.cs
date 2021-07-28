using System;
using UnityEngine;
using Newtonsoft.Json;

namespace ExternalUnityRendering.Serialization
{
    /// <summary>
    /// Class representing a Scene in Unity.
    /// It is used for Serialization purposes.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(EURSceneConverter))]
    public class EURScene
    {
        /// <summary>
        /// An empty serialized scene with <see cref="ContinueImporting"/> set to false.
        /// </summary>
        [JsonIgnore]
        public static string ClosingMessage
        {
            get
            {
                EURScene closingMessage = new EURScene
                {
                    ContinueImporting = false
                };
                return JsonConvert.SerializeObject(closingMessage);
            }
        }

        /// <summary>
        /// Struct that represents settings for a <see cref="CameraUtilites.CustomCamera"/>
        /// </summary>
        public struct CameraSettings
        {
            /// <summary>
            /// Resolution for the <see cref="CameraUtilites.CustomCamera"/>
            /// to render.
            /// </summary>
            public Vector2Int RenderSize;

            /// <summary>
            /// String path where renders should be saved.
            /// </summary>
            public string RenderDirectory;

            public CameraSettings(Vector2Int size, string directory)
            {
                RenderSize = size;
                RenderDirectory = directory;
            }
        }

        /// <summary>
        /// Time at which the export was initiated.
        /// </summary>
        public DateTime ExportDate;

        /// <summary>
        /// An object state whose children represent the root objects
        /// </summary>
        public EURGameObject SceneRoot;

        /// <summary>
        /// Settings for the cameras to use while rendering.
        /// </summary>
        public CameraSettings RendererSettings;

        /// <summary>
        /// Indicator for whether the rendering instance should continue
        /// running after importing this object.
        /// </summary>
        public bool ContinueImporting;

        /// <summary>
        /// Create a default SerializableScene with a blank ObjectState.
        /// </summary>
        public EURScene()
            : this(new EURGameObject(), new CameraSettings()) { }

        /// <summary>
        /// Create a SerializableScene with the SceneRoot as <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The objectState whose children represent the
        /// root GameObjects.</param>
        /// <param name="settings">Settings for the renderer instance.</param>
        /// <param name="continueExporting">Whether to halt the Renderer Instance.</param>
        public EURScene(EURGameObject root, CameraSettings settings, bool continueExporting = true)
        {
            ExportDate = DateTime.Now;
            SceneRoot = root;
            RendererSettings = settings;
            ContinueImporting = continueExporting;
        }

        /// <summary>
        /// Create a new SerializableScene and create a new ObjectState using
        /// <paramref name="transform"/> and assign it as the scene root.
        /// </summary>
        /// <param name="transform">The Transform of the root
        /// GameObject.</param>
        public EURScene(Transform transform, CameraSettings settings)
            : this(new EURGameObject(transform), settings) { }
    }
}
