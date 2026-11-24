using UnityEngine;

/// <summary>
/// Hitscan rifle — instant hit, single shot per fire.
/// </summary>
public class RifleWeapon : HitscanWeapon
{
    private void Awake()
    {
        weaponName = "Rifle";
        damage = 25f;
        fireRate = 3f;          // 3 shots/sec
        baseSpread = 2f;
        movementSpreadBonus = 3f;
        turretRotationSpeed = 250f;
    }
    
    protected override void FireHitscan(Transform firePoint, float spreadMultiplier)
    {
        float totalSpread = (baseSpread + movementSpreadBonus) * spreadMultiplier;
        CastHitscanRay((Vector2)firePoint.position, firePoint.right, totalSpread);
    }
}
