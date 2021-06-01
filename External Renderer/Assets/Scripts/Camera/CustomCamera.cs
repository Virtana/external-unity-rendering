using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomCamera : MonoBehaviour
{
    private Camera cam;
    private Coroutine _screenshotCoroutine;

    private string generateScreenshotPath(string folder)
    {
        try
        {
            // will throw exception if bad
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
                folder, e);
            folder = Path.GetFullPath("./");
        }
        return string.Format("{1}Screenshot-{0:yyyy-MM-dd_HH-mm-ss}.png",
                System.DateTime.Now, folder);
    }

    private void takeScreenshot(string folder, Vector2Int screenshotSize)
    {
        cam.enabled = false;
        var renderTexture = new RenderTexture(screenshotSize.x, screenshotSize.y, 24);
        cam.targetTexture = renderTexture;

        // Render the camera's view.
        cam.Render();
        RenderTexture.active = renderTexture;

        // Make a new texture and read the active Render Texture into it.
        Texture2D image = new Texture2D(screenshotSize.x, screenshotSize.y, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, screenshotSize.x, screenshotSize.y), 0, 0);
        image.Apply();

        Destroy(renderTexture);

        // Replace the original active Render Texture.
        cam.targetTexture = null;
        RenderTexture.active = null;
        cam.enabled = true;

        // now image holds the image in texture2d form
        byte[] png = ImageConversion.EncodeToPNG(image);

        var filename = generateScreenshotPath(folder);

        File.Open(filename, FileMode.OpenOrCreate).Close();
        File.WriteAllBytes(filename, png);
        Debug.LogFormat("Screenshot saved to {0}.", filename);
    }

    IEnumerator screenshotCoroutine(string folder, Vector2Int screenshotSize, float delay)
    {
        while (true)
        {
            takeScreenshot(folder, screenshotSize);
            yield return new WaitForSecondsRealtime(delay);
        }
    }


    public void SaveCamera()
    {
        cam = this.GetComponent<Camera>();
        if (cam == null) Debug.LogError("broke");
    }

    private void OnEnable()
    {
        SaveCamera();
        if (cam == null)
        {
            this.GetComponent<CustomCamera>().enabled = false;
        }
    }


    public void Screenshot(string folder, Vector2Int screenshotSize = default(Vector2Int))
    {
        if (screenshotSize.x > 300 && screenshotSize.y > 300)
        {
            // Set default size
            screenshotSize = new Vector2Int(Screen.width, Screen.height);
        }

        takeScreenshot(folder, screenshotSize);
    }

    public void TakeContinuousScreenshots(string folder, 
        Vector2Int screenshotSize = default(Vector2Int), float delay = 2f)
    {
        _screenshotCoroutine = 
            StartCoroutine(screenshotCoroutine(folder, screenshotSize, delay));
    }

    public void StopContinuousScreenshots() {
        if (_screenshotCoroutine != null)
        {
            StopCoroutine(_screenshotCoroutine);
        } else
        {
            Debug.Log("No Screenshot Coroutine is active.");
        }   
    }
}
