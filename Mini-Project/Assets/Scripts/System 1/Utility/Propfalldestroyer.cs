using UnityEngine;

/// <summary>
/// Destroys props that fall off the map (spawned outside room bounds)
/// </summary>
public class PropFallDestroyer : MonoBehaviour
{
    [Tooltip("Absolute Y position below which props are destroyed")]
    public float destroyBelowY = -50f;
    
    [Tooltip("How long to wait before starting checks (gives prop time to settle)")]
    public float initialDelay = 1f;
    
    private float startCheckTime;
    private bool isChecking = false;
    private Rigidbody rb;
    
    void Start()
    {
        startCheckTime = Time.time + initialDelay;
        rb = GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        // Start checking after initial delay
        if (!isChecking && Time.time >= startCheckTime)
        {
            isChecking = true;
        }
        
        if (isChecking)
        {
            // If prop fell below the threshold, destroy it
            if (transform.position.y < destroyBelowY)
            {
                Debug.LogWarning($"Prop {gameObject.name} fell to Y={transform.position.y:F1} - destroying (spawned outside room)");
                Destroy(gameObject);
            }
            // If prop has settled (stopped moving), remove this component
            else if (rb != null && rb.linearVelocity.magnitude < 0.1f && Time.time > startCheckTime + 2f)
            {
                // Prop landed safely - remove this component to save performance
                Destroy(this);
            }
        }
    }
}