using UnityEngine;

/// <summary>
/// Run-scoped player stats such as temporary currency.
/// </summary>
public class PlayerRunStats : MonoBehaviour
{
    [SerializeField] private int scrap;
    [SerializeField] private int hullUpgradeLevel;
    [SerializeField] private int turretUpgradeLevel;
    [SerializeField] private int weaponUpgradeLevel;
    [SerializeField] private int mobilityUpgradeLevel;

    public int Scrap => scrap;
    public float HullHealthMultiplier => 1f + hullUpgradeLevel * 0.18f;
    public float HullArmorMultiplier => 1f + hullUpgradeLevel * 0.12f;
    public float TurretFireRateMultiplier => 1f + turretUpgradeLevel * 0.14f;
    public float AccuracyRecoveryMultiplier => 1f + turretUpgradeLevel * 0.18f;
    public float WeaponDamageMultiplier => 1f + weaponUpgradeLevel * 0.16f;
    public float MobilitySpeedMultiplier => 1f + mobilityUpgradeLevel * 0.15f;
    public float MobilityAccelerationMultiplier => 1f + mobilityUpgradeLevel * 0.18f;
    public float MobilityTurnMultiplier => 1f + mobilityUpgradeLevel * 0.12f;

    public void AddScrap(int amount)
    {
        scrap = Mathf.Max(0, scrap + amount);
    }

    public bool SpendScrap(int amount)
    {
        if (amount <= 0)
            return true;
        if (scrap < amount)
            return false;

        scrap -= amount;
        return true;
    }

    public void ApplyEngineerUpgrade(EngineerUpgradePickup.UpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case EngineerUpgradePickup.UpgradeType.Hull:
                hullUpgradeLevel++;
                break;
            case EngineerUpgradePickup.UpgradeType.Turret:
                turretUpgradeLevel++;
                break;
            case EngineerUpgradePickup.UpgradeType.Weapon:
                weaponUpgradeLevel++;
                break;
            case EngineerUpgradePickup.UpgradeType.Mobility:
                mobilityUpgradeLevel++;
                break;
        }
    }
}
