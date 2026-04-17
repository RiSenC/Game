using UnityEngine;

/// <summary>
/// Flamethrower — cone of fire, applies damage-over-time to targets.
/// </summary>
public class FlamethrowerWeapon : WeaponBase
{
    [Header("Flamethrower")]
    public float coneAngle = 60f;
    public float range = 8f;
    public float dotDuration = 3f;
    public float dotDamagePerSecond = 5f;
    public LayerMask targetLayers;
    
    private void Awake()
    {
        weaponName = "Огнемёт";
        damage = 15f;
        fireRate = 2f;
        baseSpread = 0f;
        turretRotationSpeed = 200f;
        energyCost = 1;
    }
    
    protected override void Fire(Transform firePoint, float spreadMultiplier)
    {
        Vector2 fireDir = (Vector2)firePoint.right;
        Collider2D[] targets = Physics2D.OverlapCircleAll((Vector2)firePoint.position, range, targetLayers);
        
        foreach (var target in targets)
        {
            Vector2 toTarget = ((Vector2)target.transform.position - (Vector2)firePoint.position).normalized;
            float angle = Vector2.Angle(fireDir, toTarget);
            
            if (angle < coneAngle / 2f)
            {
                if (target.TryGetComponent<HealthSystem>(out var health))
                {
                    health.TakeDamage(damage, -toTarget);
                    
                    // Apply DoT
                    if (target.TryGetComponent<DamageOverTimeComponent>(out var dot))
                        dot.ApplyDamage(dotDamagePerSecond, dotDuration);
                }
            }
        }
    }
    
    protected override bool CanFire(ChassisController chassis)
    {
        return chassis != null && chassis.TryConsumeEnergy(energyCost);
    }
}