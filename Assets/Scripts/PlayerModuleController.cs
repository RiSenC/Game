using UnityEngine;

/// <summary>
/// Applies and swaps player modules using the updated hierarchy.
/// </summary>
public class PlayerModuleController : MonoBehaviour
{
    [Header("Current Modules")]
    public HullConfig currentHull;
    public TurretConfig currentTurret;
    public LocomotionBase currentLocomotion;
    public WeaponBase currentWeapon;

    [Header("Scene References")]
    public SpriteRenderer hullRenderer;
    public TurretController turretController;
    public HealthSystem healthSystem;
    public ChassisController chassisController;

    private void Awake()
    {
        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();
        if (chassisController == null)
            chassisController = GetComponent<ChassisController>();
        if (turretController == null)
            turretController = GetComponentInChildren<TurretController>();
        if (hullRenderer == null)
            hullRenderer = GetComponentInChildren<SpriteRenderer>();
        if (currentLocomotion == null)
            currentLocomotion = GetComponentInChildren<LocomotionBase>();
    }

    private void Start()
    {
        if (currentHull != null)
            ApplyHull(currentHull);

        if (currentTurret != null && turretController != null)
            turretController.SetTurretConfig(currentTurret);
    }

    public bool SwapHull(HullConfig newHull)
    {
        if (newHull == null)
            return false;

        currentHull = newHull;
        ApplyHull(newHull);
        return true;
    }

    public bool SwapTurret(TurretConfig newTurret)
    {
        if (newTurret == null || turretController == null)
            return false;

        currentTurret = newTurret;
        turretController.SetTurretConfig(newTurret);
        return true;
    }

    public bool SwapLocomotion(LocomotionBase locomotionPrefab)
    {
        if (locomotionPrefab == null || chassisController == null)
            return false;

        currentLocomotion = locomotionPrefab;
        chassisController.SwitchLocomotion(locomotionPrefab);
        return true;
    }

    public bool SwapWeapon(WeaponBase weaponPrefab)
    {
        if (weaponPrefab == null || chassisController == null)
            return false;

        currentWeapon = weaponPrefab;
        chassisController.SwitchWeapon(weaponPrefab);
        return true;
    }

    public HullConfig GetCurrentHull() => currentHull;
    public TurretConfig GetCurrentTurret() => currentTurret;
    public LocomotionBase GetCurrentLocomotion() => currentLocomotion;
    public WeaponBase GetCurrentWeapon() => currentWeapon;

    private void ApplyHull(HullConfig hull)
    {
        if (hullRenderer != null && hull.hullSprite != null)
            hullRenderer.sprite = hull.hullSprite;

        if (healthSystem != null)
        {
            healthSystem.maxHP = hull.maxHealth;
            healthSystem.currentHP = hull.maxHealth;
            healthSystem.armorFlat = hull.armor;
        }

        if (chassisController != null)
            chassisController.SetBaseHullStats(hull.maxHealth, hull.armor);
    }
}
