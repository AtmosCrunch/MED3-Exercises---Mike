using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndRoomVisualEffects : MonoBehaviour
{
    public enum EffectType
    {
        FadeToBlack,
        ScreenGlitch,
        EverythingFalls,
        AllEffects // Combines multiple effects
    }
    
    [Header("Effect Settings")]
    [Tooltip("Which visual effect to play")]
    public EffectType effectType = EffectType.AllEffects;
    
    [Header("Fade to Black")]
    [Tooltip("How long the fade takes")]
    public float fadeDuration = 2f;
    
    [Header("Screen Glitch")]
    [Tooltip("How long the glitch effect lasts")]
    public float glitchDuration = 2f;
    
    [Tooltip("How intense the glitch is")]
    [Range(0f, 1f)]
    public float glitchIntensity = 0.5f;
    
    [Header("Everything Falls")]
    [Tooltip("How long to wait before making everything fall")]
    public float fallDelay = 0.5f;
    
    private Canvas fadeCanvas;
    private Image fadeImage;
    private Material glitchMaterial;
    private Camera mainCamera;
    
    void Awake()
    {
        mainCamera = Camera.main;
    }
    
    /// <summary>
    /// Triggers the selected visual effect
    /// </summary>
    public void TriggerEffect(System.Action onComplete = null)
    {
        switch (effectType)
        {
            case EffectType.FadeToBlack:
                StartCoroutine(FadeToBlackEffect(onComplete));
                break;
                
            case EffectType.ScreenGlitch:
                StartCoroutine(ScreenGlitchEffect(onComplete));
                break;
                
            case EffectType.EverythingFalls:
                StartCoroutine(EverythingFallsEffect(onComplete));
                break;
                
            case EffectType.AllEffects:
                StartCoroutine(CombinedEffects(onComplete));
                break;
        }
    }
    
    /// <summary>
    /// Fades screen to black
    /// </summary>
    IEnumerator FadeToBlackEffect(System.Action onComplete)
    {
        // Create fade overlay
        CreateFadeCanvas();
        
        float elapsed = 0f;
        Color color = fadeImage.color;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }
        
        color.a = 1f;
        fadeImage.color = color;
        
        Debug.Log("Fade to black complete");
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Creates a screen glitch effect
    /// </summary>
    IEnumerator ScreenGlitchEffect(System.Action onComplete)
    {
        // Create glitch overlay
        CreateGlitchCanvas();
        
        float elapsed = 0f;
        
        while (elapsed < glitchDuration)
        {
            elapsed += Time.deltaTime;
            
            // Random glitch displacement
            float glitchAmount = Random.Range(-glitchIntensity, glitchIntensity) * 0.1f;
            fadeImage.rectTransform.anchoredPosition = new Vector2(
                glitchAmount * Screen.width,
                Random.Range(-glitchIntensity, glitchIntensity) * 100f
            );
            
            // Random color distortion
            fadeImage.color = new Color(
                Random.Range(0.5f, 1f),
                Random.Range(0.5f, 1f),
                Random.Range(0.5f, 1f),
                Random.Range(0.3f, 0.7f) * glitchIntensity
            );
            
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }
        
        Debug.Log("Screen glitch complete");
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Makes everything in the scene fall by enabling gravity on all rigidbodies
    /// </summary>
    IEnumerator EverythingFallsEffect(System.Action onComplete)
    {
        yield return new WaitForSeconds(fallDelay);
        
        // Find all objects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int objectsAffected = 0;
        
        foreach (GameObject obj in allObjects)
        {
            // Skip the player and UI
            if (obj.CompareTag("Player") || obj.layer == LayerMask.NameToLayer("UI"))
                continue;
            
            // Add rigidbody if it doesn't have one
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = obj.AddComponent<Rigidbody>();
                rb.mass = 10f;
                objectsAffected++;
            }
            else if (rb.isKinematic)
            {
                // Make kinematic objects dynamic
                rb.isKinematic = false;
                objectsAffected++;
            }
            
            // Ensure gravity is enabled
            rb.useGravity = true;
            
            // Add random force for chaos
            rb.AddForce(Random.insideUnitSphere * Random.Range(2f, 5f), ForceMode.Impulse);
        }
        
        Debug.Log($"Everything falls effect triggered - {objectsAffected} objects affected");
        
        // Wait a bit for things to fall
        yield return new WaitForSeconds(2f);
        
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Combines multiple effects for maximum impact
    /// </summary>
    IEnumerator CombinedEffects(System.Action onComplete)
    {
        // Start glitch immediately
        StartCoroutine(ScreenGlitchEffect(null));
        
        // Wait a moment, then make everything fall
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(EverythingFallsEffect(null));
        
        // Wait a bit more, then fade to black
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FadeToBlackEffect(null));
        
        Debug.Log("Combined effects complete");
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Resets all visual effects
    /// </summary>
    public void ResetEffects()
    {
        if (fadeCanvas != null)
        {
            Destroy(fadeCanvas.gameObject);
            fadeCanvas = null;
            fadeImage = null;
        }
    }
    
    /// <summary>
    /// Creates a canvas for fade effects
    /// </summary>
    void CreateFadeCanvas()
    {
        if (fadeCanvas != null) return;
        
        GameObject canvasObj = new GameObject("FadeCanvas");
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999; // Make sure it's on top
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // Start transparent
        
        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }
    
    /// <summary>
    /// Creates a canvas for glitch effects
    /// </summary>
    void CreateGlitchCanvas()
    {
        if (fadeCanvas != null) return;
        
        GameObject canvasObj = new GameObject("GlitchCanvas");
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        
        GameObject imageObj = new GameObject("GlitchImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(1, 1, 1, 0.5f);
        
        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }
}