using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health")]
    public float maxHealth = 100f;
    
    [Tooltip("Current health")]
    public float currentHealth;
    
    [Header("Events")]
    [Tooltip("Called when damaged")]
    public UnityEvent<float> onDamaged;
    
    [Tooltip("Called when health reaches zero")]
    public UnityEvent onDeath;
    
    [Header("Hit Feedback")]
    [Tooltip("Flash red when taking damage")]
    public bool flashOnDamage = true;
    
    [Tooltip("How long the flash lasts")]
    public float flashDuration = 0.1f;
    
    [Tooltip("Color to flash when hit")]
    public Color flashColor = Color.red;
    
    [Header("Death Settings")]
    [Tooltip("Destroy object on death")]
    public bool destroyOnDeath = true;
    
    [Tooltip("Delay before destroying (seconds)")]
    public float destroyDelay = 2f;
    
    [Tooltip("Disable components on death instead of destroying")]
    public bool disableOnDeath = false;
    
    [Header("Player Death Effects")]
    [Tooltip("Trigger visual effects on player death (only works if tagged as Player)")]
    public bool triggerDeathEffects = false;
    
    [Tooltip("Reference to visual effects (optional - will auto-find)")]
    public EndRoomVisualEffects visualEffects;
    
    [Tooltip("Regenerate level after death effects complete")]
    public bool regenerateLevelOnDeath = true;
    
    public RoomManager roomManager;
    
    [Header("Audio")]
    [Tooltip("Sound to play when this entity dies")]
    public AudioClip deathSound;
    
    [Range(0f, 1f)]
    public float deathSoundVolume = 0.7f;
    
    [Header("Death Particles")]
    [Tooltip("Particle effect to spawn on death")]
    public GameObject deathParticleEffect;
    
    [Tooltip("How long before destroying particle effect")]
    public float particleLifetime = 3f;
    
    private bool isDead = false;
    private Renderer[] renderers;
    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
    private AudioSource audioSource;
    
    void Start()
    {
        currentHealth = maxHealth;
        
        // Cache all renderers for hit flash effect
        renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r.material.HasProperty("_Color"))
            {
                originalColors[r] = r.material.color;
            }
        }
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
    }
    
    /// <summary>
    /// Apply damage to this entity
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // Trigger damage event
        onDamaged?.Invoke(damage);
        
        // Flash red on damage
        if (flashOnDamage)
        {
            StartCoroutine(FlashEffect());
        }
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Heal this entity
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        
        Debug.Log($"{gameObject.name} healed {amount}. Health: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// Flashes the entity red when taking damage
    /// </summary>
    IEnumerator FlashEffect()
    {
        // Set all materials to flash color
        foreach (Renderer r in renderers)
        {
            if (r != null && r.material.HasProperty("_Color"))
            {
                r.material.color = flashColor;
            }
        }
        
        // Wait
        yield return new WaitForSeconds(flashDuration);
        
        // Restore original colors
        foreach (Renderer r in renderers)
        {
            if (r != null && originalColors.ContainsKey(r) && r.material.HasProperty("_Color"))
            {
                r.material.color = originalColors[r];
            }
        }
    }
    
    /// <summary>
    /// Handle death
    /// </summary>
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log($"{gameObject.name} died!");
        
        // Play death sound
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound, deathSoundVolume);
        }
        
        // Spawn death particle effect
        if (deathParticleEffect != null)
        {
            GameObject particles = Instantiate(deathParticleEffect, transform.position, Quaternion.identity);
            Destroy(particles, particleLifetime);
            Debug.Log($"Spawned death particles for {gameObject.name}");
        }
        
        // Trigger death event
        onDeath?.Invoke();
        
        // Check if this is the player and should trigger death effects
        if (triggerDeathEffects && CompareTag("Player"))
        {
            TriggerPlayerDeathEffects();
            return; // Don't do normal death handling
        }
        
        if (disableOnDeath)
        {
            // Disable movement/shooting components
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != this) // Don't disable Health itself
                {
                    script.enabled = false;
                }
            }
            
            // Disable collider if it exists
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
        else if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }
    
    /// <summary>
    /// Triggers visual effects on player death
    /// </summary>
    void TriggerPlayerDeathEffects()
    {
        Debug.Log("Player died - triggering visual effects!");
        
        // Disable player controls
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this) // Don't disable Health itself
            {
                script.enabled = false;
            }
        }
        
        // Find or create visual effects
        if (visualEffects == null)
        {
            // Try to find existing visual effects in scene
            visualEffects = FindObjectOfType<EndRoomVisualEffects>();
            
            if (visualEffects == null)
            {
                // Create new visual effects
                GameObject vfxObj = new GameObject("PlayerDeathEffects");
                visualEffects = vfxObj.AddComponent<EndRoomVisualEffects>();
            }
        }
        
        // Trigger effects, then handle respawn/regeneration
        visualEffects.TriggerEffect(() => OnDeathEffectsComplete());
    }
    
    /// <summary>
    /// Called when death effects complete
    /// </summary>
    void OnDeathEffectsComplete()
    {
        Debug.Log("Death effects complete");
        
        if (regenerateLevelOnDeath && roomManager != null)
        {
            // Reset player health
            currentHealth = maxHealth;
            isDead = false;
            
            // Regenerate level
            roomManager.GenerateLevel();
            
            // Re-enable player controls
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                script.enabled = true;
            }
            
            // Reset visual effects
            if (visualEffects != null)
            {
                visualEffects.ResetEffects();
            }
        }
        else
        {
            // Just reset effects without regenerating
            if (visualEffects != null)
            {
                visualEffects.ResetEffects();
            }
        }
    }
    
    /// <summary>
    /// Check if entity is dead
    /// </summary>
    public bool IsDead()
    {
        return isDead;
    }
    
    /// <summary>
    /// Get health percentage (0-1)
    /// </summary>
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
}