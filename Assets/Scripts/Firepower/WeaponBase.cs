using UnityEngine;

/// <summary>
/// Shared base class for modular weapons.
/// Provides common stats plus projectile/effect helpers used by concrete weapons.
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Core")]
    public string weaponName = "Weapon";
    public float damage = 10f;
    public float fireRate = 1f;
    public float baseSpread = 0f;
    public float movementSpreadBonus = 0f;
    public float turretRotationSpeed = 180f;
    public float energyCost = 0f;

    [Header("Prefabs")]
    public GameObject projectilePrefab;
    public GameObject hitEffectPrefab;
    public float projectileSpeed = 20f;

    protected WeaponSystem owner;
    protected ChassisController chassis;
    protected float lastFireTime = float.NegativeInfinity;

    public virtual void Initialize(WeaponSystem ownerSystem)
    {
        owner = ownerSystem;
        chassis = ownerSystem != null ? ownerSystem.GetComponentInParent<ChassisController>() : null;
    }

    protected virtual void Update()
    {
    }

    public bool TryFire(Transform firePoint, float spreadMultiplier = 1f)
    {
        if (firePoint == null)
            return false;

        if (!CanFire(chassis))
            return false;

        float cooldown = fireRate > 0f ? 1f / fireRate : 0f;
        if (Time.time < lastFireTime + cooldown)
            return false;

        lastFireTime = Time.time;
        Fire(firePoint, spreadMultiplier);
        return true;
    }

    protected virtual bool CanFire(ChassisController chassisController)
    {
        return true;
    }

    protected abstract void Fire(Transform firePoint, float spreadMultiplier);

    public virtual void OnFireReleased()
    {
    }

    public virtual void OnUnequipped()
    {
    }

    protected GameObject SpawnProjectile(Transform firePoint, float spreadMultiplier)
    {
        if (projectilePrefab == null || firePoint == null)
            return null;

        float spreadAngle = (baseSpread + movementSpreadBonus) * Mathf.Max(0f, spreadMultiplier);
        float spreadOffset = Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f);
        Quaternion rotation = firePoint.rotation * Quaternion.Euler(0f, 0f, spreadOffset);

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, rotation);
        if (projectile.TryGetComponent<ProjectileBase>(out var projectileBase))
            projectileBase.Init(damage, projectileSpeed);

        return projectile;
    }

    protected void SpawnHitEffect(Vector2 position, Quaternion rotation)
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, position, rotation);
    }

    protected void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, transform.rotation);
    }
}
