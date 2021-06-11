using System.Collections;
using System.IO;
using UnityEngine;

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

    private void OnEnable()
    {
        SaveCamera();
        if (_camera == null)
        {
            this.GetComponent<CustomCamera>().enabled = false;
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
    /// Generate a valid rendered image output path. 
    /// </summary>
    /// <param name="folder">The folder in which to save the image. If 
    /// invalid, defaults to current directory.</param>
    /// <returns>Returns a string in the format of
    /// [<paramref name="folder"/>]/Screenshot-[DateTime].png</returns>
    private string GenerateRenderPath(string folder)
    {
        try
        {
            // This will throw an exception if the
            // folder is invalid
            var dir = new DirectoryInfo(folder);
            if (!dir.Exists)
            {
                dir.Create();
            }
            folder = dir.FullName + Path.DirectorySeparatorChar;
        }
        catch (System.Exception e)
        {
            Debug.LogErrorFormat("Invalid Path \"{0}\". \n Threw {1}.",
                folder, e.Message);
            // Default to current Directory if provided one is invalid.
            folder = Directory.GetCurrentDirectory();
        }

        return string.Format("{1}Screenshot-{0:yyyy-MM-dd_HH-mm-ss}.png",
                System.DateTime.Now, folder);
    }

    /// <summary>
    /// Renders the current view of the Camera.
    /// </summary>
    /// <param name="folder">The folder in which to save the render.</param>
    /// <param name="renderSize">The resolution of the rendered image.</param>
    public void RenderImage(string folder = "", 
        Vector2Int renderSize = default(Vector2Int))
    {
        // ensure screenshot size is at least 300x300 in size.
        renderSize.Clamp(
            new Vector2Int(300, 300), 
            new Vector2Int(int.MaxValue, int.MaxValue));

        _camera.enabled = false;
        var renderTexture = new RenderTexture(renderSize.x, renderSize.y, 24);
        _camera.targetTexture = renderTexture;

        // Render the camera's view.
        _camera.Render();
        RenderTexture.active = renderTexture;

        // Make a new texture and read the active Render Texture into it.
        var image = new Texture2D(renderSize.x, renderSize.y, TextureFormat.RGB24, false);
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
        var filename = GenerateRenderPath(folder);

        File.Open(filename, FileMode.OpenOrCreate).Close();
        File.WriteAllBytes(filename, png);
        Debug.LogFormat("Screenshot saved to {0}.", filename);
    }

    /// <summary>
    /// Coroutine that renders an image every 
    /// [<paramref name="delay"/>] seconds.
    /// </summary>
    /// <param name="folder">Folder in which to save the render.</param>
    /// <param name="renderSize">The resolution of the rendered image.</param>
    /// <param name="delay">The interval between each render.</param>
    /// <returns></returns>
    private IEnumerator RendererCoroutine(string folder, 
        Vector2Int renderSize, float delay)
    {
        while (true)
        {
            RenderImage(folder, renderSize);
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    /// <summary>
    /// Render the view of the camera repeatedly.
    /// </summary>
    /// <param name="delay">The interval between each render.</param>
    /// <param name="folder">Folder in which to save the render.</param>
    /// <param name="renderSize">The resolution of the rendered image.</param>
    public void StartIntervalRendering(float delay = 2f, string folder = "",
        Vector2Int renderSize = default(Vector2Int))
    {
        _rendererCoroutine = 
            StartCoroutine(RendererCoroutine(folder, renderSize, delay));
    }

    /// <summary>
    /// Stop rendering the view of the camera if StartIntervalRendering 
    /// was called.
    /// </summary>
    public void StopIntervalRendering() {
        if (_rendererCoroutine != null)
        {
            StopCoroutine(_rendererCoroutine);
        } else
        {
            Debug.Log("No Screenshot Coroutine is active.");
        }   
    }

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
