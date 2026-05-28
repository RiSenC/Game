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
    public float pickupDelay = 0.2f;
    public int scrapCost;
    public ModuleRarity rarity = ModuleRarity.Common;

    private float spawnedAt;

    public System.Action<ModulePickup> PickedUp;

    private void Awake()
    {
        spawnedAt = Time.time;

        Collider2D collider2D = GetComponent<Collider2D>();
        collider2D.isTrigger = true;

        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = gameObject.AddComponent<SpriteRenderer>();

        renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        renderer.color = GetModuleColor();
        transform.localScale = new Vector3(0.45f, 0.45f, 1f);
        EnsureLabel();
    }

    private void Reset()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        collider2D.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < spawnedAt + pickupDelay)
            return;

        OnTriggeredPickup(other);
    }

    public bool OnTriggeredPickup(Collider2D other)
    {
        if (Time.time < spawnedAt + pickupDelay)
            return false;

        PlayerModuleController playerModules = other.GetComponentInParent<PlayerModuleController>();
        if (playerModules == null)
            return false;

        PlayerRunStats runStats = playerModules.GetRunStats();
        bool paid = false;
        if (scrapCost > 0)
        {
            if (runStats == null || !runStats.SpendScrap(scrapCost))
                return false;
            paid = true;
        }

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

        if (!pickedUp && paid && runStats != null)
            runStats.AddScrap(scrapCost);

        if (pickedUp && dropPreviousModule)
            SpawnPreviousModule(previousHull, previousTurret, previousLocomotion, previousWeapon);

        if (pickedUp)
            PickedUp?.Invoke(this);

        if (pickedUp && destroyAfterPickup)
            Destroy(gameObject);

        return pickedUp;
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
        pickup.rarity = rarity;

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

        pickup.RefreshVisuals();
    }

    private Color GetModuleColor()
    {
        Color rarityTint = rarity switch
        {
            ModuleRarity.Common => new Color(0.9f, 0.9f, 0.9f, 1f),
            ModuleRarity.Rare => new Color(0.35f, 0.8f, 1f, 1f),
            ModuleRarity.Epic => new Color(0.85f, 0.45f, 1f, 1f),
            ModuleRarity.Legendary => new Color(1f, 0.7f, 0.2f, 1f),
            _ => Color.white
        };

        Color typeTint = moduleType switch
        {
            ModuleType.Hull => new Color(0.75f, 0.95f, 1f, 1f),
            ModuleType.Turret => new Color(1f, 0.85f, 0.45f, 1f),
            ModuleType.Locomotion => new Color(0.55f, 1f, 0.85f, 1f),
            ModuleType.Weapon => new Color(1f, 0.55f, 0.45f, 1f),
            _ => Color.white
        };

        return Color.Lerp(typeTint, rarityTint, 0.55f);
    }

    private void EnsureLabel()
    {
        TextMesh label = GetComponentInChildren<TextMesh>();
        if (label == null)
        {
            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            label = labelObject.AddComponent<TextMesh>();
            label.characterSize = 0.1f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
        }

        string costText = scrapCost > 0 ? $"\n{scrapCost} scrap" : string.Empty;
        label.text = $"{rarity} {moduleType}{costText}";
    }
}
