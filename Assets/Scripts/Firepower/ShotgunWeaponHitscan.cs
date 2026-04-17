using UnityEngine;

/// <summary>
/// Hitscan shotgun — multiple pellets with wide spread.
/// </summary>
public class ShotgunWeaponHitscan : HitscanWeapon
{
    [Header("Shotgun")]
    [Range(4, 12)]
    public int pelletCount = 8;
    public float spreadCone = 40f;
    
    private void Awake()
    {
        weaponName = "Дробовик";
        damage = 10f;           // per pellet
        fireRate = 1.2f;
        baseSpread = 0f;
        movementSpreadBonus = 0f;
        turretRotationSpeed = 200f;
        energyCost = 0;
    }
    
    protected override void FireHitscan(Transform firePoint, float spreadMultiplier)
    {
        float totalSpread = spreadCone * spreadMultiplier;
        float step = totalSpread / (pelletCount - 1);
        
        for (int i = 0; i < pelletCount; i++)
        {
            float angle = -totalSpread / 2f + step * i;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * (Vector2)firePoint.right;
            CastHitscanRay((Vector2)firePoint.position, direction, 1f);
        }
    }
}