using UnityEngine;

[CreateAssetMenu(fileName = "New Turret Config", menuName = "Tank/Turret Config")]
public class TurretConfig : ScriptableObject
{
    public enum WeaponType { Bullet, Projectile, Experimental, Beam, Launcher }
    
    [Header("Aiming")]
    public float turretRotationSpeed = 45f;
    
    [Header("Weapon Type")]
    public WeaponType weaponType = WeaponType.Bullet;
    
    [Header("Firing")]
    public float fireRate = 0.1f;
    public float bulletSpeed = 20f;
    public int bulletsPerShot = 1;
    
    [Header("Damage")]
    public float bulletDamage = 10f;
    public float splashRadius = 0f;
    
    [Header("Accuracy & Spread")]
    public float baseSpreadAngle = 2f;
    public float maxSpreadAngle = 25f;
    public int bulletsUntilMaxSpread = 20;
    public float movementSpreadMultiplier = 1.5f;
    public float accuracyRecoveryRate = 5f; // Degrees per second
    
    [Header("Visual")]
    public Sprite turretSprite;
    public Sprite barrelSprite;
    public Vector3 turretScale = Vector3.one;
    public Color turretColor = Color.white;
}