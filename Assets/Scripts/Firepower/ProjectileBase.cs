using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ProjectileBase : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float lifetime = 3f;
    public GameObject hitEffectPrefab;

    protected float damage;
    protected float speed;
    protected Rigidbody2D rb;

    public virtual void Init(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.right * speed;

        Destroy(gameObject, lifetime);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // Don't hit the shooter
        if (other.CompareTag("Player")) return;

        // Apply damage if target has health
        if (other.TryGetComponent<HealthSystem>(out var health))
            health.TakeDamage(damage, -rb.linearVelocity.normalized);

        SpawnHitEffect();
        OnHit(other);
        Destroy(gameObject);
    }

    /// <summary>Override for custom on-hit behavior (e.g. explosion).</summary>
    protected virtual void OnHit(Collider2D other) { }

    protected void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
    }
}