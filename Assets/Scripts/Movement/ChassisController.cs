using UnityEngine;

/// <summary>
/// Manages chassis type, applies stat modifiers, and coordinates module swapping.
/// Attach to the root [Player] GameObject.
/// </summary>
public class ChassisController : MonoBehaviour
{
    [Header("Chassis Configuration")]
    public ChassisStats chassisStats;
    
    private HealthSystem healthSystem;
    private LocomotionBase locomotion;
    private WeaponSystem weaponSystem;
    private float baseMaxHP;
    private float baseArmorFlat;
    private float baseArmorPercent;
    
    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        locomotion = GetComponentInChildren<LocomotionBase>();
        weaponSystem = GetComponentInChildren<WeaponSystem>();

        if (healthSystem != null)
        {
            baseMaxHP = healthSystem.maxHP;
            baseArmorFlat = healthSystem.armorFlat;
            baseArmorPercent = healthSystem.armorPercent;
        }
        
        if (chassisStats != null)
            ApplyChassisModifiers();
    }
    
    private void ApplyChassisModifiers()
    {
        locomotion = GetComponentInChildren<LocomotionBase>();
        weaponSystem = GetComponentInChildren<WeaponSystem>();

        // Apply health & armor mods
        if (healthSystem != null)
        {
            healthSystem.maxHP = baseMaxHP * chassisStats.maxHealthModifier;
            healthSystem.currentHP = healthSystem.maxHP;
            healthSystem.armorFlat = baseArmorFlat * chassisStats.armorFlatModifier;
            healthSystem.armorPercent = Mathf.Clamp(baseArmorPercent * chassisStats.armorPercentModifier, 0f, 0.99f);
        }
        
        // Apply locomotion mods
        if (locomotion != null)
        {
            locomotion.ApplyChassisModifiers(chassisStats);
        }
    }

    public void SetBaseHullStats(float maxHP, float armorFlat)
    {
        baseMaxHP = maxHP;
        baseArmorFlat = armorFlat;

        if (chassisStats != null)
            ApplyChassisModifiers();
        else if (healthSystem != null)
        {
            healthSystem.maxHP = maxHP;
            healthSystem.currentHP = maxHP;
            healthSystem.armorFlat = armorFlat;
        }
    }
    
    /// <summary>Swap chassis at runtime</summary>
    public void SwitchChassis(ChassisStats newChassis)
    {
        chassisStats = newChassis;
        ApplyChassisModifiers();
    }
    
    /// <summary>Swap locomotion at runtime</summary>
    public void SwitchLocomotion(LocomotionBase newLocomotion)
    {
        if (newLocomotion == null)
            return;

        if (locomotion != null)
            Destroy(locomotion.gameObject);
        
        locomotion = Instantiate(newLocomotion, transform);
        locomotion.chassis = this;
        if (chassisStats != null)
            locomotion.ApplyChassisModifiers(chassisStats);
    }
    
    /// <summary>Swap weapon at runtime</summary>
    public void SwitchWeapon(WeaponBase newWeapon)
    {
        if (weaponSystem == null)
            weaponSystem = GetComponentInChildren<WeaponSystem>();

        if (weaponSystem != null)
            weaponSystem.EquipWeapon(newWeapon);
    }
}
