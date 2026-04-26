using UnityEngine;

/// <summary>
/// Rocket launcher — slow initial speed, gradually accelerates.
/// </summary>
public class RocketWeaponImproved : ProjectileWeapon
{
    [Header("Rocket")]
    public float explosionRadius = 3.5f;
    public float explosionDamage = 90f;
    public bool isHoming = false;
    public LayerMask explosionLayers;
    public LayerMask targetLayer;
    
    private void Awake()
    {
        weaponName = "Ракетомёт";
        damage = 25f;           // direct hit
        fireRate = 0.5f;
        baseSpread = 2f;
        movementSpreadBonus = 4f;
        turretRotationSpeed = 120f;
        projectileSpeed = 8f;   // slow initial speed
        energyCost = 2;
    }
    
    protected override void FireProjectile(Transform firePoint, float spreadMultiplier)
    {
        GameObject proj = SpawnProjectile(firePoint, spreadMultiplier);
        
        if (proj.TryGetComponent<Rocket>(out var rocket))
        {
            rocket.explosionRadius = explosionRadius;
            rocket.explosionDamage = explosionDamage;
            rocket.explosionLayers = explosionLayers;
            rocket.isHoming = isHoming;
            rocket.targetLayer = targetLayer;
        }
    }
    
    protected override bool CanFire(ChassisController chassis)
    {
        return chassis != null && chassis.TryConsumeEnergy(energyCost);
    }
}