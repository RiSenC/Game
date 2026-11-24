using UnityEngine;

/// <summary>
/// Owns the currently equipped modular weapon and its fire point.
/// </summary>
public class WeaponSystem : MonoBehaviour
{
    public Transform firePoint;
    public WeaponBase currentWeapon;

    public void EquipWeapon(WeaponBase weaponPrefab)
    {
        if (weaponPrefab == null)
            return;

        if (currentWeapon != null)
        {
            currentWeapon.OnUnequipped();
            Destroy(currentWeapon.gameObject);
        }

        currentWeapon = Instantiate(weaponPrefab, transform);
        currentWeapon.Initialize(this);
    }

    public bool TryFire(float spreadMultiplier = 1f)
    {
        return currentWeapon != null && currentWeapon.TryFire(firePoint, spreadMultiplier);
    }

    public void ReleaseFire()
    {
        if (currentWeapon != null)
            currentWeapon.OnFireReleased();
    }
}
