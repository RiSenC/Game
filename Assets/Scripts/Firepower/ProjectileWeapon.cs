using UnityEngine;

/// <summary>
/// Base for projectile-based weapons.
/// </summary>
public abstract class ProjectileWeapon : WeaponBase
{
    protected abstract void FireProjectile(Transform firePoint, float spreadMultiplier);
    
    protected override void Fire(Transform firePoint, float spreadMultiplier)
    {
        FireProjectile(firePoint, spreadMultiplier);
    }
}