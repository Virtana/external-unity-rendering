using System;
using System.IO;
using System.Threading.Tasks;
using ExternalUnityRendering.PathManagement;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

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
        /// Texture2D which stores the image after it has been rendered.
        /// </summary>
        private Texture2D _image;

        /// <summary>
        /// The graphics format of <see cref="_image"/>. Used for creating the png file.
        /// </summary>
        private GraphicsFormat _imageFormat;

        /// <summary>
        /// RenderTexture which <see cref="_camera"/> renders to.
        /// </summary>
        private RenderTexture _renderTexture;

        /// <summary>
        /// Byte cache for the raw texture data of <see cref="_image"/>. Used to reduce GC allocs.
        /// </summary>
        private byte[] _rawImageCache = null;

        /// <summary>
        /// Executes when the CustomCamera is created. Used for initializing data.
        /// </summary>
        private void Awake()
        {
            // create a new camera directory in the subdirectory renders
            _renderPath = new DirectoryManager(
                Path.Combine(Application.persistentDataPath, name), true);

            // Grab the attached camera, set to false and make it render only to a
            // rendertexture
            _camera = GetComponent<Camera>();
            _camera.enabled = false;
            _camera.forceIntoRenderTexture = true;

            // Create the temp image, cache the format and get a rendertexture
            _image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            _imageFormat = _image.graphicsFormat;
            _renderTexture = RenderTexture.GetTemporary(1920, 1080, 24);
        }

        /// <summary>
        /// Write the image in <see cref="_rawImageCache"/> to the render folder.
        /// </summary>
        /// <param name="render">Bytes of the image to be saved.</param>
        private async void SaveRender(DateTime exportTime, Vector2Int renderSize)
        {
            byte[] png = await Task.Run(() =>
                {
                    return ImageConversion.EncodeArrayToPNG(_rawImageCache, _imageFormat,
                        (uint)renderSize.x, (uint)renderSize.y);
                });

            string filename = $"Render-{renderSize.x}x{renderSize.y}-" +
                $"{exportTime:yyyy-MM-dd-HH-mm-ss-fff-UTCzz}.png";
            // will automatically rename if name collision occurs
            FileManager file = new FileManager(_renderPath, filename, true);

            // if inaccessible, use an auto file
            if (file == null)
            {
                file = new FileManager();
                Debug.LogWarning($@"File {_renderPath.Path}/{filename} could not be " +
                    $"created. Using {file.Path} instead.");
            }

            await file.WriteToFileAsync(png);
            Debug.Log($"Saved render to { file.Path } at { DateTime.Now }.");
        }

        // TODO maybe figure out a way to not have crazy high values that will trigger a
        // out of vram error
        // ensure screenshot size is at least 300x300 in size.
        // TODO find a way to catch out of vram exception

        /// <summary>
        /// Renders the current view of the <see cref="_camera"/>.
        /// </summary>
        /// <param name="exportTime">The time at which the export was generated.</param>
        /// <param name="renderSize">The resolution of the rendered image.</param>
        public void RenderImage(DateTime exportTime,Vector2Int renderSize = default)
        {
            renderSize.Clamp(
                new Vector2Int(300, 300),
                new Vector2Int(int.MaxValue, int.MaxValue));

            // always disabling in case a script enables
            _camera.enabled = false;

            // Resize textures if necessary (currently not)
            if (_renderTexture.width != renderSize.x
                || _renderTexture.height != renderSize.y)
            {
                RenderTexture.ReleaseTemporary(_renderTexture);
                _renderTexture =
                    RenderTexture.GetTemporary(renderSize.x, renderSize.y, 24);
            }
            if (_image.width != renderSize.x
                || _image.height != renderSize.y)
            {
                _image.Resize(renderSize.x, renderSize.y);
            }

            // Set render targets
            _camera.targetTexture = _renderTexture;
            RenderTexture.active = _renderTexture;

            // Render the camera's view.
            _camera.Render();

            // Make a new texture and read the active Render Texture into it.
            _image.ReadPixels(new Rect(0, 0, renderSize.x, renderSize.y), 0, 0);
            _image.Apply();

            // Get the raw texture data directly in the buffer, and copy to
            // the cache, only reallocating when necessary.
            NativeArray<byte> rawImage = _image.GetRawTextureData<byte>();
            if (_rawImageCache?.Length != rawImage.Length)
            {
                _rawImageCache = new byte[rawImage.Length];
            }
            rawImage.CopyTo(_rawImageCache);

            // begin the export
            SaveRender(exportTime, renderSize);
        }
    }
}
