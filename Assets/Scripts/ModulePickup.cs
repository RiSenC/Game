using UnityEngine;

/// <summary>
/// Simple floor pickup that swaps one player module on contact.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ModulePickup : MonoBehaviour
{
    public enum ModuleType
    {
        Hull,
        Turret,
        Locomotion,
        Weapon
    }

    public ModuleType moduleType;
    public HullConfig hullModule;
    public TurretConfig turretModule;
    public LocomotionBase locomotionModule;
    public WeaponBase weaponModule;
    public bool destroyAfterPickup = true;
    public bool dropPreviousModule = true;
    public Vector3 dropOffset = new Vector3(0.6f, 0f, 0f);

    private void Reset()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        collider2D.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerModuleController playerModules = other.GetComponentInParent<PlayerModuleController>();
        if (playerModules == null)
            return;

        HullConfig previousHull = playerModules.GetCurrentHull();
        TurretConfig previousTurret = playerModules.GetCurrentTurret();
        LocomotionBase previousLocomotion = playerModules.GetCurrentLocomotion();
        WeaponBase previousWeapon = playerModules.GetCurrentWeapon();

        bool pickedUp = moduleType switch
        {
            ModuleType.Hull => playerModules.SwapHull(hullModule),
            ModuleType.Turret => playerModules.SwapTurret(turretModule),
            ModuleType.Locomotion => playerModules.SwapLocomotion(locomotionModule),
            ModuleType.Weapon => playerModules.SwapWeapon(weaponModule),
            _ => false
        };

        if (pickedUp && dropPreviousModule)
            SpawnPreviousModule(previousHull, previousTurret, previousLocomotion, previousWeapon);

        if (pickedUp && destroyAfterPickup)
            Destroy(gameObject);
    }

    private void SpawnPreviousModule(
        HullConfig previousHull,
        TurretConfig previousTurret,
        LocomotionBase previousLocomotion,
        WeaponBase previousWeapon)
    {
        switch (moduleType)
        {
            case ModuleType.Hull when previousHull == null:
            case ModuleType.Turret when previousTurret == null:
            case ModuleType.Locomotion when previousLocomotion == null:
            case ModuleType.Weapon when previousWeapon == null:
                return;
        }

        GameObject droppedPickup = new GameObject("Dropped Module Pickup");
        droppedPickup.transform.position = transform.position + dropOffset;

        CircleCollider2D collider2D = droppedPickup.AddComponent<CircleCollider2D>();
        collider2D.isTrigger = true;
        collider2D.radius = 0.5f;

        ModulePickup pickup = droppedPickup.AddComponent<ModulePickup>();
        pickup.moduleType = moduleType;
        pickup.destroyAfterPickup = true;
        pickup.dropPreviousModule = false;

        switch (moduleType)
        {
            case ModuleType.Hull:
                pickup.hullModule = previousHull;
                break;
            case ModuleType.Turret:
                pickup.turretModule = previousTurret;
                break;
            case ModuleType.Locomotion:
                pickup.locomotionModule = previousLocomotion;
                break;
            case ModuleType.Weapon:
                pickup.weaponModule = previousWeapon;
                break;
        }
    }
}
