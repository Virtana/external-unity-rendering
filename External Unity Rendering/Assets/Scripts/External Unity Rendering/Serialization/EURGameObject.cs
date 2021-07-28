using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace ExternalUnityRendering.Serialization
{
    /// <summary>
    /// Class representing a GameObject in Unity.
    /// It is used for Serialization Purposes.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(EURGameObjectConverter))]
    public class EURGameObject
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
        public List<EURGameObject> Children;

        /// <summary>
        /// Create a Default blank ObjectState.
        /// </summary>
        public EURGameObject()
        {
            ObjectTransform = new TransformState()
            {
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Scale = Vector3.one
            };
            Children = new List<EURGameObject>();
            Name = "";
        }

        /// <summary>
        /// Create an ObjectState representing the GameObject using its
        /// transform.
        /// </summary>
        /// <param name="transform">The transform of the gameObject.</param>
        public EURGameObject(Transform transform)
        {
            Name = transform.name;
            ObjectTransform = new TransformState(transform);
            Children = new List<EURGameObject>();

            foreach (Transform childTransform in transform)
            {
                Children.Add(new EURGameObject(childTransform));
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

            foreach (EURGameObject child in Children)
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
}
