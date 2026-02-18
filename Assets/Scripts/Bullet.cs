using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 direction;
    private float speed;
    private float damage;
    public float lifetime = 5f;
    private bool hasInitialized = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        Destroy(gameObject, lifetime);
    }
    
    void FixedUpdate()
    {
        if (hasInitialized)
        {
            rb.linearVelocity = direction * speed;
        }
    }
    
    public void Initialize(Vector2 fireDirection, float bulletSpeed, float bulletDamage)
    {
        direction = fireDirection.normalized;
        speed = bulletSpeed;
        damage = bulletDamage;
        hasInitialized = true;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            return;
        
        // if (collision.CompareTag("Enemy"))
        // {
        //     Enemy enemy = collision.GetComponent<Enemy>();
        //     if (enemy != null)
        //     {
        //         enemy.TakeDamage(damage);
        //     }
        //     Destroy(gameObject);
        // }
        // else if (!collision.CompareTag("Player"))
        // {
        //     Destroy(gameObject);
        // }
    }
}