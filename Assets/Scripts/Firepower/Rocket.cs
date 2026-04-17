using UnityEngine;

/// <summary>
/// Rocket projectile — slow at start, accelerates over time.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Rocket : ProjectileBase
{
    [Header("Rocket")]
    public float acceleration = 15f;     // units per sec^2
    public float maxSpeed = 20f;
    public float explosionRadius = 3.5f;
    public float explosionDamage = 90f;
    public LayerMask explosionLayers;
    public bool isHoming = false;
    public LayerMask targetLayer;
    
    private Vector2 velocity;
    private Transform homingTarget;
    
    public override void Init(float damage, float speed)
    {
        base.Init(damage, speed);
        velocity = (Vector2)transform.right * speed;
        
        if (isHoming)
            FindNearestTarget();
    }
    
    private void FixedUpdate()
    {
        // Accelerate
        if (velocity.magnitude < maxSpeed)
            velocity += (Vector2)transform.right * acceleration * Time.deltaTime;
        
        // Homing
        if (isHoming && homingTarget != null)
        {
            Vector2 toTarget = ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;
            float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0, 0, targetAngle),
                120f * Time.deltaTime
            );
        }
        
        rb.linearVelocity = velocity.normalized * Mathf.Min(velocity.magnitude, maxSpeed);
    }
    
    protected override void OnHit(Collider2D other)
    {
        Explode();
    }
    
    private void Explode()
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        
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
    
    private void FindNearestTarget()
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, 30f, targetLayer);
        float closest = float.MaxValue;
        
        foreach (var t in targets)
        {
            float d = Vector2.Distance(transform.position, t.transform.position);
            if (d < closest)
            {
                closest = d;
                homingTarget = t.transform;
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}