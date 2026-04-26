using UnityEngine;

/// <summary>
/// Updated Enemy to use new HealthSystem and modular architecture.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Enemy Config")]
    public float maxHealth = 30f;
    public float speed = 4f;
    public float rotationSpeed = 120f;
    public float stopDistance = 2f;
    
    [Header("Visual")]
    public Sprite enemySprite;
    public Color damageFlashColor = Color.red;
    public float damageFlashDuration = 0.1f;
    
    private HealthSystem healthSystem;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    private Color originalColor;
    private float flashTimer = 0f;
    private bool isFlashing = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        healthSystem = GetComponent<HealthSystem>();
        
        // Setup health system if not present
        if (healthSystem == null)
        {
            healthSystem = gameObject.AddComponent<HealthSystem>();
            healthSystem.maxHP = maxHealth;
            healthSystem.currentHP = maxHealth;
        }
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            if (enemySprite != null)
                spriteRenderer.sprite = enemySprite;
        }
        
        // Find player
        PlayerMovement playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerTransform = playerMovement.transform;
        }
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        // Subscribe to death event
        if (healthSystem != null)
            healthSystem.OnDeath += Die;
    }
    
    void FixedUpdate()
    {
        if (playerTransform == null || healthSystem.IsDead)
            return;
        
        // Get direction to player
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        
        // Rotate to face player
        float targetAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = transform.eulerAngles.z;
        float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);
        float newAngle = currentAngle + Mathf.Clamp(angleDiff, -rotationSpeed * Time.fixedDeltaTime, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newAngle);
        
        // Move towards player if not in stop distance
        Vector2 velocity = Vector2.zero;
        if (distanceToPlayer > stopDistance)
        {
            velocity = directionToPlayer * speed;
        }
        
        rb.linearVelocity = velocity;
    }
    
    void Update()
    {
        // Update damage flash
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0)
            {
                isFlashing = false;
                if (spriteRenderer != null)
                    spriteRenderer.color = originalColor;
            }
        }
    }
    
    // Handle damage flash when taking damage
    private void OnEnable()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged += HandleDamage;
    }
    
    private void OnDisable()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged -= HandleDamage;
    }
    
    private void HandleDamage(float current, float max)
    {
        // Flash red on damage
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageFlashColor;
            isFlashing = true;
            flashTimer = damageFlashDuration;
        }
    }
    
    void Die()
    {
        Debug.Log("Enemy destroyed!");
        Destroy(gameObject);
    }
}