using UnityEngine;

/// <summary>
/// Manages swapping chassis, weapons, and locomotion at runtime.
/// Useful for menu systems or mid-game loadout changes.
/// </summary>
public class ModuleSwapper : MonoBehaviour
{
    public ChassisController chassisController;
    
    // Prefab references
    public ChassisStats[] availableChassis;
    public WeaponBase[] availableWeapons;
    public LocomotionBase[] availableLocomotions;
    
    /// <summary>Change the chassis (also resets health/armor)</summary>
    public void ChangeChassis(int index)
    {
        if (index >= 0 && index < availableChassis.Length)
            chassisController.SwitchChassis(availableChassis[index]);
    }
    
    /// <summary>Change the weapon</summary>
    public void ChangeWeapon(int index)
    {
        if (index >= 0 && index < availableWeapons.Length)
            chassisController.SwitchWeapon(availableWeapons[index]);
    }
    
    /// <summary>Change locomotion type</summary>
    public void ChangeLocomotion(int index)
    {
        if (index >= 0 && index < availableLocomotions.Length)
            chassisController.SwitchLocomotion(availableLocomotions[index]);
    }
}