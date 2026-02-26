using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Rigidbody2D rb;
    private float damage;
    public float lifetime = 5f;
    private bool initialized = false;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Bullet missing Rigidbody2D!");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        if (!initialized)
        {
            Destroy(gameObject);
            return;
        }
        
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        Destroy(gameObject, lifetime);
    }
    
    void FixedUpdate()
    {
        // Keep bullets rotated to face direction of movement
        if (rb != null && rb.linearVelocity.magnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }
    
    public void Initialize(Vector2 bulletVelocity, float bulletDamage)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        
        damage = bulletDamage;
        initialized = true;
        
        // Set velocity
        rb.linearVelocity = bulletVelocity;
        
        // Rotate to face direction
        if (bulletVelocity.magnitude > 0.01f)
        {
            float angle = Mathf.Atan2(bulletVelocity.y, bulletVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            return;
        
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (!collision.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}