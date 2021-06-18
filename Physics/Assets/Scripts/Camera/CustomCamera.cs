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
                _renderPath = new DirectoryManager(value);
            }
        }

        private void Start()
        {
            _renderPath = new DirectoryManager();
        }

        private void OnEnable()
        {
            SaveCamera();
            if (_camera == null)
            {
                enabled = false;
            }
        }

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
        /// Write the image in <paramref name="render"/> to the render path.
        /// </summary>
        /// <param name="data">Bytes of the image to be saved.</param>
        private void SaveRender(byte[] render)
        {
            FileManager file = new FileManager(_renderPath, 
                $"Render-{ DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff-UTCzz}.png");
            file.WriteToFile(render);
            Debug.Log($"Saved render to { file.Path } at { DateTime.Now }.");
        }

        /// <summary>
        /// Renders the current view of the Camera.
        /// </summary>
        /// <param name="renderSize">The resolution of the rendered image.</param>
        public void RenderImage(Vector2Int renderSize = default)
        {
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

        /// <summary>
        /// Stop rendering the view of the camera if StartIntervalRendering 
        /// was called.
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

        // TODO Add multicam support.
        /// <summary>
        /// On runtime load, attach the customCamera to the first gameobject
        /// found with a camera component.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        public static void AttachToCamera()
        {
            Camera cam = FindObjectOfType<Camera>();

            if (cam != null)
            {
                // add this behaviour
                cam.gameObject.AddComponent<CustomCamera>();
                return;
            }

            // If cam is null, then no cameras were found.
            Debug.LogError("Missing Camera!");
        }
    }
}
