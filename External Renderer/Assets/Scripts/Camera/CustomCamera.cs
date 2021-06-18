using ExternalUnityRendering.PathManagement;
using System;
using System.Collections;
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
        /// Renderer coroutine used to take continuous screenshots at intervals.
        /// Used as a parameter for StopCoroutine.
        /// </summary>
        private Coroutine _rendererCoroutine;

        private DirectoryManager _renderPath;
        public string RenderPath
        {
            get
            {
                return _renderPath.Path;
            }
            set
            {
                // TODO name all cameras according to heirarchy
                // HACK cant tell apart cameras yet, so just let them all write to the same folder
                _renderPath = new DirectoryManager($@"{ value }\Renders\{ this.name }");
            }
        }

        private void Awake()
        {
            // TODO add name parent concatenation for all cameras
            // create a new camera directory in the subdirectory renders
            _renderPath = new DirectoryManager($@"Renders\{ this.name }", true);
        }

        private void OnEnable()
        {
            SaveCamera();
            if (_camera == null)
            {
                enabled = false;
            }
        }

        // TODO Remove this and just directly call getcomponent CustomCamera is 
        // automatically added to objects with cameras so check should not be needed
        /// <summary>
        /// Create an internal reference to the Camera component attached to
        /// this Gameobject. 
        /// </summary>
        public void SaveCamera()
        {
            _camera = GetComponent<Camera>();

            if (_camera == null)
            {
                Debug.LogError("Could not find camera");
            };
        }

        /// <summary>
        /// Write the image in <paramref name="render"/> to the render folder.
        /// </summary>
        /// <param name="data">Bytes of the image to be saved.</param>
        private void SaveRender(byte[] render)
        {
            FileManager file = new FileManager(_renderPath,
                $"Render-{ DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff-UTCzz}.png", true);

            // will automatically rename if name collision occurs
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

            Destroy(renderTexture);

            // Replace the original active Render Texture.
            _camera.targetTexture = null;
            RenderTexture.active = null;
            _camera.enabled = true;

            // now image holds the image in texture2d form
            byte[] png = ImageConversion.EncodeToPNG(image);

            // create a filename for the render
            SaveRender(png);
        }

        // TODO determine the need for this. Physics exports multiple states and
        // importer renders on each state. Exporter probably should have this functionality.
        /// <summary>
        /// Coroutine that renders an image every 
        /// [<paramref name="delay"/>] seconds.
        /// </summary>
        /// <param name="renderSize">The resolution of the rendered image.</param>
        /// <param name="delay">The interval between each render.</param>
        /// <returns></returns>
        private IEnumerator RendererCoroutine(Vector2Int renderSize, float delay)
        {
            while (true)
            {
                RenderImage(renderSize);
                yield return new WaitForSecondsRealtime(delay);
            }
        }

        // TODO same as above
        /// <summary>
        /// Render the view of the camera repeatedly.
        /// </summary>
        /// <param name="delay">The interval between each render.</param>
        /// <param name="renderSize">The resolution of the rendered image.</param>
        public void StartIntervalRendering(float delay = 2f,
            Vector2Int renderSize = default)
        {
            _rendererCoroutine =
                StartCoroutine(RendererCoroutine(renderSize, delay));
        }

        // TODO same as above
        /// <summary>
        /// Stop rendering the view of the camera if StartIntervalRendering was called.
        /// </summary>
        public void StopIntervalRendering()
        {
            if (_rendererCoroutine != null)
            {
                StopCoroutine(_rendererCoroutine);
            }
            else
            {
                Debug.Log("No Screenshot Coroutine is active.");
            }
        }

        /// <summary>
        /// When the runtime loads, add a CustomCamera component to all camera 
        /// gameobjects that don't already have it.
        /// </summary>
        [Obsolete("Importer will handle attaching if necessary", true)]
        //[RuntimeInitializeOnLoadMethod]
        public static void AttachToCameras()
        {
            Camera[] cameras = FindObjectsOfType<Camera>();

            if (cameras.Length == 0)
            {
                // If cam is empty, then no cameras were found.
                Debug.LogError("Missing Camera! Importer cannot render from this.");
                return;
            }

            foreach (Camera camera in cameras)
            {
                // add this behaviour
                if (camera.gameObject.GetComponent<CustomCamera>() == null)
                {
                    camera.gameObject.AddComponent<CustomCamera>();
                }
            }
        }
    }
}
