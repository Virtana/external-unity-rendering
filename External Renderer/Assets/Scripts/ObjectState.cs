using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExternalUnityRendering
{
    /// <summary>
    /// Class representing a GameObject in Unity.
    /// It is used for Serialization Purposes.
    /// </summary>
    [Serializable]
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

        // Variables representing the properties
        public string Name;
        public TransformState ObjectTransform;
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
    public class SceneState
    {
        /// <summary>
        /// Struct that represents the general settings for the cameras
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
        public SceneState(ObjectState root, CameraSettings settings)
        {
            ExportDate = DateTime.Now;
            SceneRoot = root;
            RendererSettings = settings;
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
}
