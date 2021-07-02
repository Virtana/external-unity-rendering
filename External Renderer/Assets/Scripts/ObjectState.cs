using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters;
using Newtonsoft.Json.UnityConverters.Math;
using Newtonsoft.Json.Converters;
using System.IO;

namespace ExternalUnityRendering
{
    /// <summary>
    /// Class representing a GameObject in Unity.
    /// It is used for Serialization Purposes.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(ObjectStateConverter))]
    public class ObjectState
    {
        /// <summary>
        /// Struct representing the base parameters of the
        /// Transfrom Component.
        /// </summary>
        public struct TransformState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;

            public TransformState(Transform transform)
            {
                Position = transform.position;
                Rotation = transform.rotation;
                Scale = transform.localScale;
            }
        }

        /// <summary>
        /// Name of the GameObject represented.
        /// </summary>
        public string Name;

        /// <summary>
        /// The worldspace transform values (Position, Rotation, Scale) of the
        /// GameObject.
        /// </summary>
        public TransformState ObjectTransform;

        /// <summary>
        /// The list of children of this transform
        /// </summary>
        public List<ObjectState> Children;

        /// <summary>
        /// Create a Default blank ObjectState.
        /// </summary>
        public ObjectState()
        {
            ObjectTransform = new TransformState()
            {
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Scale = Vector3.one
            };
            Children = new List<ObjectState>();
            Name = "";
        }

        /// <summary>
        /// Create an ObjectState representing the GameObject using its
        /// transform.
        /// </summary>
        /// <param name="transform">The transform of the gameObject.</param>
        public ObjectState(Transform transform)
        {
            Name = transform.name;
            ObjectTransform = new TransformState(transform);
            Children = new List<ObjectState>();

            foreach (Transform childTransform in transform)
            {
                Children.Add(new ObjectState(childTransform));
            }
        }

        /// <summary>
        /// Recursively update the gameobjects in the scene using the ObjectState's values
        /// </summary>
        /// <param name="transform"></param>
        public void UnpackData(Transform transform)
        {
            // update transforms
            transform.SetPositionAndRotation(ObjectTransform.Position, ObjectTransform.Rotation);
            transform.localScale = ObjectTransform.Scale;

            foreach (ObjectState child in Children)
            {
                var childTransform = transform.Find(child.Name);
                if (childTransform == null)
                {
                    Debug.LogWarningFormat("Child {0} missing from {1}.",
                        child.Name, transform.name);
                }
                else
                {
                    child.UnpackData(childTransform);
                }
            }
        }
    }

    /// <summary>
    /// Class representing a Scene in Unity.
    /// It is used for Serialization purposes.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(SceneStateConverter))]
    public class SceneState
    {
        /// <summary>
        /// Struct that represents settings for the CustomCameras
        /// </summary>
        public struct CameraSettings
        {
            public Vector2Int RenderSize;
            public string RenderDirectory;

            public CameraSettings(Vector2Int size, string directory)
            {
                RenderSize = size;
                RenderDirectory = directory;
            }
        }

        /// <summary>
        /// SceneState to be serialised and transmitted as a closing signal
        /// for the receiver instance.
        /// </summary>
        public SceneState JsonClosingSignal
        {
            get
            {
                return new SceneState
                {
                    ExportDate = DateTime.Now,
                    ContinueImporting = false
                };
            }
        }

        /// <summary>
        /// Time at which the export was initiated.
        /// </summary>
        public DateTime ExportDate;

        /// <summary>
        /// An object state whose children represent the root objects
        /// </summary>
        public ObjectState SceneRoot;

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
        /// Create a default SceneState with a blank ObjectState.
        /// </summary>
        public SceneState()
            : this(new ObjectState(), new CameraSettings()) { }

        /// <summary>
        /// Create a SceneState with the SceneRoot as <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The objectState whose children represent the
        /// root GameObjects.</param>
        /// <param name="settings">Settings for the renderer instance.</param>
        /// <param name="continueExporting">Whether to halt the Renderer Instance.</param>
        public SceneState(ObjectState root, CameraSettings settings, bool continueExporting = true)
        {
            ExportDate = DateTime.Now;
            SceneRoot = root;
            RendererSettings = settings;
            ContinueImporting = continueExporting;
        }

        /// <summary>
        /// Create a new SceneState and create a new ObjectState using
        /// <paramref name="transform"/> and assign it as the scene root.
        /// </summary>
        /// <param name="transform">The Transform of the root
        /// GameObject.</param>
        public SceneState(Transform transform, CameraSettings settings)
            : this(new ObjectState(transform), settings) { }
    }

    public class SceneStateConverter : PartialConverter<SceneState>
    {
        public override bool CanRead
        {
            get { return false; }
        }

        private readonly ObjectStateConverter _stateConverter = new ObjectStateConverter();
        private readonly Vector2IntConverter _vector2IntConverter = new Vector2IntConverter();
        private readonly IsoDateTimeConverter _dateTimeConverter = new IsoDateTimeConverter();

        protected override void ReadValue(ref SceneState value, string name, JsonReader reader, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        protected override void WriteJsonProperties(JsonWriter writer, SceneState value, JsonSerializer serializer)
        {
            writer.WritePropertyName(nameof(value.ExportDate));
            _dateTimeConverter.WriteJson(writer, value.ExportDate, serializer);

            writer.WritePropertyName(nameof(value.SceneRoot));
            _stateConverter.WriteJson(writer, value.SceneRoot, serializer);

            writer.WritePropertyName(nameof(value.RendererSettings));
            writer.WriteStartObject();
            {
                writer.WritePropertyName(nameof(value.RendererSettings.RenderSize));
                _vector2IntConverter.WriteJson(writer, value.RendererSettings.RenderSize, serializer);

                writer.WritePropertyName(nameof(value.RendererSettings.RenderDirectory));
                writer.WriteValue(value.RendererSettings.RenderDirectory);
            }
            writer.WriteEndObject();

            writer.WritePropertyName(nameof(value.ContinueImporting));
            writer.WriteValue(value.ContinueImporting);
        }
    }

    public class ObjectStateConverter : PartialConverter<ObjectState>
    {
        private readonly Vector3Converter _vector3Converter = new Vector3Converter();
        private readonly QuaternionConverter _quaternionConverter = new QuaternionConverter();

        public override bool CanRead {
            get { return false; }
        }

        protected override void ReadValue(ref ObjectState value, string name, JsonReader reader, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        protected override void WriteJsonProperties(JsonWriter writer, ObjectState value, JsonSerializer serializer)
        {
            writer.WritePropertyName(nameof(value.Name));
            writer.WriteValue(value.Name);

            writer.WritePropertyName(nameof(value.ObjectTransform));
            writer.WriteStartObject();
            {
                writer.WritePropertyName(nameof(value.ObjectTransform.Position));
                _vector3Converter.WriteJson(writer, value.ObjectTransform.Position, serializer);
                writer.WritePropertyName(nameof(value.ObjectTransform.Rotation));
                _quaternionConverter.WriteJson(writer, value.ObjectTransform.Rotation, serializer);
                writer.WritePropertyName(nameof(value.ObjectTransform.Scale));
                _vector3Converter.WriteJson(writer, value.ObjectTransform.Scale, serializer);
            }
            writer.WriteEndObject();

            writer.WritePropertyName(nameof(value.Children));
            writer.WriteStartArray();
            foreach (ObjectState child in value.Children)
            {
                writer.WriteStartObject();
                WriteJsonProperties(writer, child, serializer);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}
