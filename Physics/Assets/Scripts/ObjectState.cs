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
    public partial class ObjectState
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
    }

    /// <summary>
    /// Class representing a Scene in Unity.
    /// It is used for Serialization purposes.
    /// </summary>
    [Serializable]
    public partial class SceneState
    {
        /// <summary>
        /// Time at which the export was initiated.
        /// </summary>
        public DateTime exportDate;

        /// <summary>
        /// An object state whose children represent the root objects 
        /// </summary>
        public ObjectState sceneRoot;

        /// <summary>
        /// Create a default SceneState with a blank ObjectState.
        /// </summary>
        public SceneState() : this (new ObjectState())
        {
        }

        /// <summary>
        /// Create a SceneState with the SceneRoot as <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The objectState whose children represent the 
        /// root GameObjects.</param>
        public SceneState(ObjectState root)
        {
            exportDate = DateTime.Now;
            sceneRoot = root;
        }
    }
}
