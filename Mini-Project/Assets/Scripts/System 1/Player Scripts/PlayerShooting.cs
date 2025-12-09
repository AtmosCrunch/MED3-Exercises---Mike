using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    [Tooltip("Damage per shot")]
    public float damage = 25f;
    
    [Tooltip("Maximum shooting range")]
    public float range = 100f;
    
    [Tooltip("Time between shots (seconds)")]
    public float fireRate = 0.5f;
    
    [Tooltip("Force applied to rigidbodies when hit")]
    public float hitForce = 10f;
    
    [Header("References")]
    [Tooltip("The gun object (for muzzle flash position)")]
    public Transform gunTransform;
    
    [Tooltip("Camera for aiming (uses main camera if not set)")]
    public Camera playerCamera;
    
    [Header("Visual Effects")]
    [Tooltip("Muzzle flash particle effect (optional)")]
    public ParticleSystem muzzleFlash;
    
    [Tooltip("Impact effect prefab (optional)")]
    public GameObject impactEffect;
    
    [Tooltip("Impact effect lifetime")]
    public float impactEffectLifetime = 2f;
    
    [Header("Audio")]
    [Tooltip("Gunshot sound effect (optional)")]
    public AudioClip shootSound;
    
    [Tooltip("Volume for shoot sound")]
    [Range(0f, 1f)]
    public float shootVolume = 0.7f;
    
    private float nextFireTime = 0f;
    private AudioSource audioSource;
    
    void Start()
    {
        // Get camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Create audio source for shooting sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }
    
    void Update()
    {
        // Check for shoot input (left mouse button or left trigger)
        bool shootInput = Mouse.current.leftButton.isPressed;
        
        // Alternative: Use new Input System actions if you have them set up
        // bool shootInput = Keyboard.current[Key.Space].isPressed;
        
        if (shootInput && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }
    
    void Shoot()
    {
        // Always play muzzle flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        // Always play shoot sound
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound, shootVolume);
        }
        
        // Raycast from camera center
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.Log($"Hit: {hit.collider.name}");
            
            // Check if hit has health component (enemy)
            Health targetHealth = hit.collider.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
                
                // Spawn impact effect ONLY when hitting enemy
                if (impactEffect != null)
                {
                    GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impact, impactEffectLifetime);
                }
            }
            
            // Apply force to rigidbodies (props/enemies)
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(-hit.normal * hitForce, ForceMode.Impulse);
            }
            
            // Debug line to show hit
            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
        }
        else
        {
            // Show ray even if nothing hit
            Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow, 1f);
        }
    }
    
    // Optional: Draw crosshair in editor
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        // Simple crosshair
        float size = 10f;
        float thickness = 2f;
        
        // Center of screen
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;
        
        // Draw crosshair lines
        GUI.color = Color.white;
        
        // Horizontal line
        GUI.DrawTexture(new Rect(centerX - size, centerY - thickness/2, size * 2, thickness), Texture2D.whiteTexture);
        
        // Vertical line
        GUI.DrawTexture(new Rect(centerX - thickness/2, centerY - size, thickness, size * 2), Texture2D.whiteTexture);
    }
}