using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class EndRoomInteraction : MonoBehaviour
{
    public RoomManager roomManager;
    public GameObject uiPromptPrefab; // Optional UI prefab for "Press E"
    
    [Header("Visual Effects")]
    [Tooltip("Reference to visual effects component (optional - will auto-find if not set)")]
    public EndRoomVisualEffects visualEffects;
    
    private GameObject uiPromptInstance;
    private bool playerInRange = false;
    private bool isRegenerating = false;

    void Start()
    {
        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
        
        // Auto-find visual effects if not assigned
        if (visualEffects == null)
        {
            visualEffects = GetComponent<EndRoomVisualEffects>();
            if (visualEffects == null)
            {
                visualEffects = gameObject.AddComponent<EndRoomVisualEffects>();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Player entered interaction zone.");
            ShowPrompt(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Player left interaction zone.");
            ShowPrompt(false);
        }
    }

    void Update()
    {
        if (playerInRange && !isRegenerating && Keyboard.current[Key.E].wasPressedThisFrame)
        {
            Debug.Log("E pressed! Triggering end room sequence...");
            ShowPrompt(false);
            isRegenerating = true;
            
            // Trigger visual effects IMMEDIATELY
            if (visualEffects != null)
            {
                visualEffects.TriggerEffect(OnEffectsComplete);
            }
            else
            {
                OnEffectsComplete();
            }
            
            // Start audio IMMEDIATELY (fades out background music in parallel)
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayEndRoomAudio(null); // No callback needed, visual effects handle timing
            }
        }
    }
    
    /// <summary>
    /// Called when visual effects complete (which waits for audio)
    /// </summary>
    void OnEffectsComplete()
    {
        Debug.Log("Effects complete - regenerating level...");
        
        // Reset visual effects
        if (visualEffects != null)
        {
            visualEffects.ResetEffects();
        }
        
        // Regenerate level
        roomManager.GenerateLevel();
        isRegenerating = false;
        
        // Resume background music
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ResumeBackgroundMusic();
        }
    }

    void ShowPrompt(bool show)
    {
        if (uiPromptPrefab == null) return;

        if (show && uiPromptInstance == null)
        {
            uiPromptInstance = Instantiate(uiPromptPrefab);
        }
        else if (!show && uiPromptInstance != null)
        {
            Destroy(uiPromptInstance);
        }
    }
}