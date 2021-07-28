using ExternalUnityRendering.PathManagement;
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
        /// <see cref="Texture2D"/> holding the rendered image from the <see cref="_camera"/>.
        /// </summary>
        private Texture2D _renderedImage = null;

        /// <summary>
        /// <see cref="RenderTexture"/> which the <see cref="_camera"/> renders to.
        /// </summary>
        private RenderTexture _renderTexture = null;

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
            _camera.enabled = false;
            _renderedImage = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            _renderTexture = RenderTexture.GetTemporary(1920, 1080, 24);
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

            _camera.targetTexture = _renderTexture;

            if (_renderTexture.width != renderSize.x
                || _renderTexture.height != renderSize.y)
            {
                RenderTexture.ReleaseTemporary(_renderTexture);
                _renderTexture =
                    RenderTexture.GetTemporary(renderSize.x, renderSize.y, 24);
            }
            if (_renderedImage.width != renderSize.x
                || _renderedImage.height != renderSize.y)
            {
                _renderedImage.Resize(renderSize.x, renderSize.y);
            }

            _camera.targetTexture = _renderTexture;
            RenderTexture.active = _renderTexture;

            // Render the camera's view.
            _camera.Render();

            // Make a new texture and read the active Render Texture into it.
            _renderedImage.ReadPixels(new Rect(0, 0, renderSize.x, renderSize.y), 0, 0);
            _renderedImage.Apply();

#if UNITY_EDITOR
            if (Camera.main == _camera)
            {
                _camera.enabled = true;
                _camera.targetTexture = null;
            }
#endif
            // now image holds the image in texture2d form
            byte[] png = _renderedImage.EncodeToPNG();
            SaveRender(png, exportTime);
        }
    }
}
