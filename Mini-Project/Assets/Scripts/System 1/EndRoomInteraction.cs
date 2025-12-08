using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class EndRoomInteraction : MonoBehaviour
{
    public RoomManager roomManager;
    public GameObject uiPromptPrefab; // Optional UI prefab for "Press E"
    private GameObject uiPromptInstance;
    private bool playerInRange = false;

    void Start()
    {
        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
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
        if (playerInRange && Keyboard.current[Key.E].wasPressedThisFrame)
        {
            Debug.Log("E pressed! Regenerating level...");
            ShowPrompt(false);
            roomManager.GenerateLevel();
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