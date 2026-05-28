using UnityEngine;

/// <summary>
/// Base for instant-hit weapons (bullets, rays).
/// </summary>
public abstract class HitscanWeapon : WeaponBase
{
    [Header("Hitscan")]
    public LayerMask hitLayers;
    public int pelletsPerShot = 1;  // 1 for rifle, multiple for shotgun
    
    protected abstract void FireHitscan(Transform firePoint, float spreadMultiplier);
    
    protected override void Fire(Transform firePoint, float spreadMultiplier)
    {
        FireHitscan(firePoint, spreadMultiplier);
    }
    
    /// <summary>Cast a ray with spread and apply damage</summary>
    protected void CastHitscanRay(Vector2 origin, Vector2 direction, float spreadAngle)
    {
        float spreadOffset = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
        Vector2 finalDir = Quaternion.Euler(0, 0, spreadOffset) * direction;
        
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, finalDir, 100f, hitLayers);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            HealthSystem health = hit.collider.GetComponentInParent<HealthSystem>();
            if (health != null)
                health.TakeDamage(damage, -finalDir);

            SpawnHitEffect();
            return;
        }
    }
}
