using UnityEngine;

/// <summary>
/// Manages chassis type, applies stat modifiers, and coordinates module swapping.
/// Attach to the root [Player] GameObject.
/// </summary>
public class ChassisController : MonoBehaviour
{
    [Header("Chassis Configuration")]
    public ChassisStats chassisStats;
    
    [Header("Energy System")]
    public float maxEnergy = 100f;
    [HideInInspector] public float currentEnergy;
    public float energyRegenPerSecond = 5f;
    
    private HealthSystem healthSystem;
    private LocomotionBase locomotion;
    private WeaponSystem weaponSystem;
    
    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        locomotion = GetComponentInChildren<LocomotionBase>();
        weaponSystem = GetComponentInChildren<WeaponSystem>();
        
        if (chassisStats != null)
            ApplyChassisModifiers();
        
        currentEnergy = maxEnergy;
    }
    
    private void Update()
    {
        // Regenerate energy
        currentEnergy = Mathf.Clamp(currentEnergy + energyRegenPerSecond * Time.deltaTime, 0f, maxEnergy);
    }
    
    private void ApplyChassisModifiers()
    {
        // Apply health & armor mods
        if (healthSystem != null)
        {
            healthSystem.maxHP *= chassisStats.maxHealthModifier;
            healthSystem.currentHP = healthSystem.maxHP;
            healthSystem.armorFlat *= chassisStats.armorFlatModifier;
            healthSystem.armorPercent *= chassisStats.armorPercentModifier;
        }
        
        // Apply locomotion mods
        if (locomotion != null)
        {
            locomotion.ApplyChassisModifiers(chassisStats);
        }
        
        // Apply energy mods
        maxEnergy *= chassisStats.maxEnergyModifier;
        energyRegenPerSecond *= chassisStats.energyRegenModifier;
        currentEnergy = maxEnergy;
    }
    
    public bool TryConsumeEnergy(float amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            return true;
        }
        return false;
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
        if (locomotion != null)
            Destroy(locomotion.gameObject);
        
        locomotion = Instantiate(newLocomotion, transform);
        locomotion.chassis = this;
    }
    
    /// <summary>Swap weapon at runtime</summary>
    public void SwitchWeapon(WeaponBase newWeapon)
    {
        if (weaponSystem != null)
            weaponSystem.EquipWeapon(newWeapon);
    }
}