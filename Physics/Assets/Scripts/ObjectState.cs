using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneStateExporter
{
    [Serializable]
    public class ObjectState
    {
        public struct tform
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        public string name;
        public tform objectTransform;
        public List<ObjectState> children;

        public ObjectState(Transform transform)
        {
            name = transform.name;
            objectTransform.position = transform.position;
            objectTransform.rotation = transform.rotation;
            objectTransform.scale = transform.localScale;
            children = new List<ObjectState>();
        }

        // call this to init and let JSON.Net handle the rest
        public ObjectState()
        {
            objectTransform = new tform()
            {
                position = Vector3.zero,
                rotation = Quaternion.identity,
                scale = Vector3.zero
            };
            children = new List<ObjectState>();
            name = "";
        }

        public void UpdateTransform(in Transform transform)
        {            
            // update transforms
            transform.position = objectTransform.position;
            transform.rotation = objectTransform.rotation;
            transform.localScale = objectTransform.scale;

            // check if number of children align
            if (transform.childCount != children.Count)
            {
                Debug.LogFormat("Expected: {0} Actual: {1}", transform.childCount, children.Count);
                Debug.LogFormat("Children: {0}", JsonConvert.SerializeObject(children));
                throw new ImportSceneException(transform.name);
            }

            int i = 0;
            foreach (Transform child in transform)
            {
                children[i++].UpdateTransform(child);
            }
        }

        public void UnpackData(in Transform transform)
        {
            // update transforms
            transform.position = objectTransform.position;
            transform.rotation = objectTransform.rotation;
            transform.localScale = objectTransform.scale;

            ///incomplete
            foreach (var child in children)
            {
                var childTransform = transform.Find(child.name);
            }
        }

        public static ObjectState GenerateState(Transform transform)
        {
            var currentObject = new ObjectState(transform);

            foreach (Transform child in transform)
            {
                currentObject.children.Add(GenerateState(child));
            }

            return currentObject;
        }
    }

    [Serializable]
    public class SceneState
    {
        public DateTime exportDate;
        public ObjectState sceneRoot;

        public SceneState()
        {
            exportDate = DateTime.Now;
            sceneRoot = new ObjectState();
        }

        public void AssignSceneRoot(ObjectState root)
        {
            sceneRoot = root;
        }
    }

    public class ImportSceneException : Exception
    {
        // add more for more cases
        public ImportSceneException()
        {
            Debug.LogError("Invalid Hierarchy - Missing child.");
        }

        public ImportSceneException(string message)
            : base(string.Format(
                "Scene State File child count mismatch for : {0}", message))
        {
        }
        public ImportSceneException(string message, Exception inner)
            : base(string.Format(
                "Scene State File child count mismatch for : {0}", message), inner)
        {
        }
    }
}