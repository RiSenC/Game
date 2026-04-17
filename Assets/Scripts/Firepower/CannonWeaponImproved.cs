using UnityEngine;

/// <summary>
/// Cannon — fast projectile that gradually decelerates.
/// Explodes on impact with area damage.
/// </summary>
public class CannonWeaponImproved : ProjectileWeapon
{
    [Header("Cannon")]
    public float explosionRadius = 2.5f;
    public float explosionDamage = 70f;
    public float knockbackForce = 5f;
    public LayerMask explosionLayers;
    
    private void Awake()
    {
        weaponName = "Пушка";
        damage = 30f;           // direct hit
        fireRate = 0.8f;
        baseSpread = 1f;
        movementSpreadBonus = 5f;
        turretRotationSpeed = 100f;
        projectileSpeed = 25f;  // fast initial speed
        energyCost = 0;
    }
    
    protected override void FireProjectile(Transform firePoint, float spreadMultiplier)
    {
        GameObject proj = SpawnProjectile(firePoint, spreadMultiplier);
        
        if (proj.TryGetComponent<CannonShell>(out var shell))
        {
            shell.explosionRadius = explosionRadius;
            shell.explosionDamage = explosionDamage;
            shell.explosionLayers = explosionLayers;
        }
        
        // Recoil
        Rigidbody2D shooterRb = GetComponentInParent<Rigidbody2D>();
        if (shooterRb != null)
            shooterRb.AddForce(-firePoint.right * knockbackForce, ForceMode2D.Impulse);
    }
}