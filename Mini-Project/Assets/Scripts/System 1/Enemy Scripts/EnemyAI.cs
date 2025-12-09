using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [Header("AI Behavior")]
    [Tooltip("Detection range for player")]
    public float detectionRange = 20f;
    
    [Tooltip("Range at which enemy stops moving and shoots")]
    public float shootingRange = 15f;
    
    [Tooltip("How close enemy gets before stopping")]
    public float stopDistance = 10f;
    
    [Header("Combat")]
    [Tooltip("Damage per shot")]
    public float damage = 10f;
    
    [Tooltip("Time between shots")]
    public float fireRate = 1.5f;
    
    [Tooltip("Shooting accuracy (0-1, 0=terrible, 1=perfect)")]
    [Range(0f, 1f)]
    public float accuracy = 0.7f;
    
    [Tooltip("Maximum inaccuracy offset (meters)")]
    public float maxInaccuracy = 2f;
    
    [Header("Movement")]
    [Tooltip("Enemy movement speed")]
    public float moveSpeed = 3.5f;
    
    [Tooltip("Enemy rotation speed")]
    public float rotationSpeed = 5f;
    
    [Header("References")]
    [Tooltip("Gun transform for shooting from")]
    public Transform gunTransform;
    
    [Tooltip("Muzzle flash particle effect")]
    public ParticleSystem muzzleFlash;
    
    [Tooltip("Shoot sound effect")]
    public AudioClip shootSound;
    
    [Range(0f, 1f)]
    public float shootVolume = 0.5f;
    
    private Transform player;
    private Health playerHealth;
    private Health myHealth;
    private float nextFireTime = 0f;
    private AudioSource audioSource;
    private Rigidbody rb;
    
    public enum AIState
    {
        Idle,
        Chasing,
        Shooting
    }
    
    private AIState currentState = AIState.Idle;
    
    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<Health>();
        }
        else
        {
            Debug.LogError("EnemyAI: No player found with 'Player' tag!");
        }
        
        // Get components
        myHealth = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        
        // Auto-setup rigidbody if missing or not configured properly
        if (rb == null)
        {
            Debug.Log($"Adding Rigidbody to {gameObject.name}");
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody for proper enemy movement
        rb.mass = 50f;
        rb.linearDamping = 2f;
        rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Ensure enemy has a capsule collider
        if (GetComponent<CapsuleCollider>() == null)
        {
            Debug.Log($"Adding CapsuleCollider to {gameObject.name}");
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.5f;
            col.center = new Vector3(0, 1f, 0);
        }
        
        // Create audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.maxDistance = 50f;
        
        Debug.Log($"EnemyAI initialized on {gameObject.name} - Player found: {player != null}");
    }
    
    void Update()
    {
        if (myHealth != null && myHealth.IsDead()) return;
        if (player == null)
        {
            // Try to find player again if lost
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerHealth = playerObj.GetComponent<Health>();
                Debug.Log($"{gameObject.name} found player!");
            }
            return;
        }
        if (playerHealth != null && playerHealth.IsDead()) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        AIState previousState = currentState;
        
        // Update state based on distance
        if (distanceToPlayer <= shootingRange)
        {
            currentState = AIState.Shooting;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = AIState.Chasing;
        }
        else
        {
            currentState = AIState.Idle;
        }
        
        // Log state changes
        if (currentState != previousState)
        {
            Debug.Log($"{gameObject.name}: State changed to {currentState} (distance: {distanceToPlayer:F1}m)");
        }
        
        // Execute behavior based on state
        switch (currentState)
        {
            case AIState.Idle:
                // Do nothing or patrol
                break;
                
            case AIState.Chasing:
                ChasePlayer(distanceToPlayer);
                LookAtPlayer();
                break;
                
            case AIState.Shooting:
                if (distanceToPlayer > stopDistance)
                {
                    ChasePlayer(distanceToPlayer);
                }
                LookAtPlayer();
                TryShoot();
                break;
        }
    }
    
    void ChasePlayer(float distance)
    {
        if (distance <= stopDistance) return;
        
        // Move towards player
        Vector3 direction = (player.position - transform.position).normalized;
        Vector3 movement = direction * moveSpeed * Time.deltaTime;
        
        if (rb != null)
        {
            rb.MovePosition(transform.position + movement);
        }
        else
        {
            transform.position += movement;
        }
    }
    
    void LookAtPlayer()
    {
        // Rotate to face player (only on Y axis)
        Vector3 direction = player.position - transform.position;
        direction.y = 0; // Keep rotation horizontal
        
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    void TryShoot()
    {
        if (Time.time < nextFireTime) return;
        
        Shoot();
        nextFireTime = Time.time + fireRate;
    }
    
    void Shoot()
    {
        if (gunTransform == null)
        {
            gunTransform = transform;
        }
        
        // Play muzzle flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        // Play sound
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound, shootVolume);
        }
        
        // Calculate shoot direction with inaccuracy
        Vector3 targetPosition = player.position + Vector3.up * 1f; // Aim at player center
        
        // Add inaccuracy
        float inaccuracyAmount = (1f - accuracy) * maxInaccuracy;
        Vector3 inaccuracy = new Vector3(
            Random.Range(-inaccuracyAmount, inaccuracyAmount),
            Random.Range(-inaccuracyAmount, inaccuracyAmount),
            Random.Range(-inaccuracyAmount, inaccuracyAmount)
        );
        targetPosition += inaccuracy;
        
        Vector3 shootDirection = (targetPosition - gunTransform.position).normalized;
        
        // Raycast
        RaycastHit hit;
        if (Physics.Raycast(gunTransform.position, shootDirection, out hit, shootingRange))
        {
            Debug.DrawLine(gunTransform.position, hit.point, Color.red, 0.5f);
            
            // Check if hit player
            if (hit.collider.CompareTag("Player"))
            {
                Health targetHealth = hit.collider.GetComponent<Health>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(damage);
                    Debug.Log($"Enemy hit player for {damage} damage!");
                }
            }
        }
        else
        {
            Debug.DrawRay(gunTransform.position, shootDirection * shootingRange, Color.yellow, 0.5f);
        }
    }
    
    // Visualize detection ranges in editor
    void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Shooting range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
        
        // Stop distance
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}