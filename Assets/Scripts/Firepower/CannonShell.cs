using UnityEngine;

/// <summary>
/// Cannon shell projectile — fast, gradually slows down.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CannonShell : ProjectileBase
{
    [Header("Cannon Shell")]
    public float deceleration = 5f;  // units per sec^2
    public float explosionRadius = 2.5f;
    public float explosionDamage = 70f;
    public LayerMask explosionLayers;
    
    private Vector2 velocity;
    
    public override void Init(float damage, float speed)
    {
        base.Init(damage, speed);
        velocity = (Vector2)transform.right * speed;
    }
    
    private void FixedUpdate()
    {
        // Gradual deceleration
        velocity = Vector2.Lerp(velocity, Vector2.zero, deceleration * Time.deltaTime);
        rb.linearVelocity = velocity;
    }
    
    protected override void OnHit(Collider2D other)
    {
        Explode();
    }
    
    private void Explode()
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        
        // Area damage
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, explosionLayers);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<HealthSystem>(out var health))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
                health.TakeDamage(explosionDamage * falloff, -((Vector2)hit.transform.position - (Vector2)transform.position).normalized);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}