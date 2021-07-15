﻿using ExternalUnityRendering.PathManagement;
using System;
using System.IO;
using UnityEngine;

namespace ExternalUnityRendering.CameraUtilites
{
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent]
    public class CustomCamera : MonoBehaviour
    {
        /// <summary>
        /// Camera component connected to the current GameObject.
        /// Used for rendering.
        /// </summary>
        private Camera _camera;

        /// <summary>
        /// DirectoryManager managing this custom camera's output path
        /// </summary>
        private DirectoryManager _renderPath;

        /// <summary>
        /// Path where this custom camera will output renders.
        /// </summary>
        public string RenderPath
        {
            get
            {
                return _renderPath.Path;
            }
            set
            {
                // HACK Export to same folders for same name
                // Can't differentiate between folders created by this and folders
                // created by something else
                if (!string.IsNullOrEmpty(value))
                {
                    _renderPath = new DirectoryManager(Path.Combine(value, name));
                }
            }
        }

        /// <summary>
        /// Executes when the CustomCamera is created. Used for initializing data.
        /// </summary>
        private void Awake()
        {
            // create a new camera directory in the subdirectory renders
            _renderPath = new DirectoryManager(
                Path.Combine(Application.persistentDataPath, name), true);

            // Importer will attach this to cameras right before importing.
            _camera = GetComponent<Camera>();

            // TODO Uncomment this and provide some sort of control to turn it on
            // should be off by default
            _camera.enabled = false;
            image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            _camera.targetTexture = RenderTexture.GetTemporary(1920, 1080, 24);
        }

        /// <summary>
        /// Write the image in <paramref name="render"/> to the render folder.
        /// </summary>
        /// <param name="render">Bytes of the image to be saved.</param>
        private void SaveRender(byte[] render, DateTime exportTime)
        {
            string filename = $"Render-{ exportTime:yyyy-MM-dd-HH-mm-ss-fff-UTCzz}.png";
            // will automatically rename if name collision occurs
            FileManager file = new FileManager(_renderPath, filename, true);

            // if inaccessible, use an auto file
            if (file == null)
            {
                file = new FileManager();
                Debug.LogError($@"File {_renderPath.Path}/{filename} could not be " +
                    $"created. Using {file.Path} instead.");
            }

            file.WriteToFile(render);
            Debug.Log($"Saved render to { file.Path } at { DateTime.Now }.");
        }

        Texture2D image = null;

        /// <summary>
        /// Renders the current view of the Camera.
        /// </summary>
        /// <param name="renderSize">The resolution of the rendered image.</param>
        public void RenderImage(DateTime exportTime,Vector2Int renderSize = default)
        {
            // TODO maybe figure out a way to not have crazy high values that will trigger a
            // out of vram error
            // ensure screenshot size is at least 300x300 in size.
            renderSize.Clamp(
                new Vector2Int(300, 300),
                new Vector2Int(int.MaxValue, int.MaxValue));

            _camera.enabled = false; // always disabling in case a script enables

            if (_camera.targetTexture.width != renderSize.x
                || _camera.targetTexture.height != renderSize.y)
            {
                RenderTexture.ReleaseTemporary(_camera.targetTexture);
                _camera.targetTexture = RenderTexture.GetTemporary(1920, 1080, 24);
            }
            if (image.width != renderSize.x
                || image.height != renderSize.y)
            {
                image.Resize(renderSize.x, renderSize.y);
            }

            // Render the camera's view.
            _camera.Render();
            RenderTexture.active = _camera.targetTexture;

            // Make a new texture and read the active Render Texture into it.
            image.ReadPixels(new Rect(0, 0, renderSize.x, renderSize.y), 0, 0);
            image.Apply();

#if UNITY_EDITOR
            _camera.enabled = true;
#endif
            // now image holds the image in texture2d form
            byte[] png = image.EncodeToPNG();
            SaveRender(png, exportTime);
        }
    }
}
