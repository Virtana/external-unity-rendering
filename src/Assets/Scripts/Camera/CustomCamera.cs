using ExternalUnityRendering.PathManagement;
using System;
using System.IO;
using UnityEngine;

namespace ExternalUnityRendering.CameraUtilites
{
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
                    _renderPath = new DirectoryManager(Path.Combine(value, "Renders", name));
                }
            }
        }

        /// <summary>
        /// Executes when the CustomCamera is created. Used for initializing data.
        /// </summary>
        private void Awake()
        {
            // create a new camera directory in the subdirectory renders
            _renderPath = new DirectoryManager(Path.Combine("Renders", name), true);

            // Importer will attach this to cameras right before importing.
            // so can delete if accidentally attached
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                Debug.LogError("Missing Camera! Custom Cameras can only be added to " +
                    "gameobjects with Camera Components. Destroying..");
                Destroy(this);
            }

            // TODO Uncomment this and provide some sort of control to turn it on
            // should be off by default
            // _camera.enabled = false;
        }

        /// <summary>
        /// Write the image in <paramref name="render"/> to the render folder.
        /// </summary>
        /// <param name="render">Bytes of the image to be saved.</param>
        private void SaveRender(byte[] render)
        {
            string filename = $"Render-{ DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff-UTCzz}.png";
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
        public void RenderImage(Vector2Int renderSize = default)
        {
            // TODO maybe figure out a way to not have crazy high values that will trigger a
            // out of vram error
            // ensure screenshot size is at least 300x300 in size.
            renderSize.Clamp(
                new Vector2Int(300, 300),
                new Vector2Int(int.MaxValue, int.MaxValue));

            _camera.enabled = false;
            RenderTexture renderTexture = new RenderTexture(renderSize.x, renderSize.y, 24);
            _camera.targetTexture = renderTexture;

            // Render the camera's view.
            _camera.Render();
            RenderTexture.active = renderTexture;

            // Make a new texture and read the active Render Texture into it.
            Texture2D image = new Texture2D(renderSize.x, renderSize.y, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, renderSize.x, renderSize.y), 0, 0);
            image.Apply();

            // Replace the original active Render Texture.
            _camera.targetTexture = null;
            RenderTexture.active = null;

            // add check to only enable if needed
            _camera.enabled = true;

            // now image holds the image in texture2d form
            byte[] png = ImageConversion.EncodeToPNG(image);

            Destroy(image);
            Destroy(renderTexture);

            // create a filename for the render
            SaveRender(png);
        }
    }
}
