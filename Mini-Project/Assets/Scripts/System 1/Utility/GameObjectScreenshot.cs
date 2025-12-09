using UnityEngine;
using System.IO;

public class GameObjectScreenshot : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera screenshotCamera;
    
    [Header("Target Settings")]
    public GameObject targetObject; // Your target GameObject
    
    [Header("Image Settings")]
    public int imageWidth = 1920;
    public int imageHeight = 1080;
    public bool transparentBackground = false;
    
    [Header("Save Settings")]
    public string folderName = "Screenshots";
    public string filePrefix = "screenshot_";

    void Start()
    {
        // Set up camera for transparent background if needed
        if (transparentBackground && screenshotCamera != null)
        {
            screenshotCamera.clearFlags = CameraClearFlags.SolidColor;
            screenshotCamera.backgroundColor = new Color(0, 0, 0, 0);
        }
    }

    // Call this for auto-generated filename
    public void Capture()
    {
        CaptureScreenshot(filePrefix + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
    }

    // Internal method with filename parameter
    void CaptureScreenshot(string filename)
    {
        if (screenshotCamera == null)
        {
            Debug.LogError("Screenshot camera is not assigned!");
            return;
        }

        // Create RenderTexture
        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
        screenshotCamera.targetTexture = rt;

        // Render the camera's view
        screenshotCamera.Render();

        // Read pixels from RenderTexture
        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
        screenshot.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        screenshot.Apply();

        // Clean up
        screenshotCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt); // Only destroys the temporary RenderTexture
        
        // Encode and save
        byte[] bytes = screenshot.EncodeToPNG();
        
        // Create directory if it doesn't exist
        string folderPath = Path.Combine(Application.dataPath, folderName);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, filename);
        File.WriteAllBytes(filePath, bytes);

        Debug.Log($"Screenshot saved to: {filePath}");
        
        // Clean up texture (only the temporary Texture2D, not your GameObject!)
        Destroy(screenshot);
    }

    // Call this with a custom name
    public void CaptureWithCustomName(string customName)
    {
        CaptureScreenshot(customName + ".png");
    }
}