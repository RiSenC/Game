using UnityEngine;

/// <summary>
/// Universal health system — attach to Player AND enemies.
/// Handles HP, armor (flat reduction + % reduction),
/// and zone-based armor (front/side/back).
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Health")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("Armor — from design doc")]
    [Tooltip("Flat damage reduction per hit")]
    public float armorFlat = 10f;

    [Tooltip("Percentage damage reduction (0-1). 0.25 = 25% reduction")]
    [Range(0f, 0.99f)]
    public float armorPercent = 0.25f;

    [Tooltip("Minimum damage always dealt (25% of incoming per doc)")]
    [Range(0f, 1f)]
    public float minDamagePercent = 0.25f;

    [Header("Zonal Armor (War Thunder style)")]
    public float frontArmorMultiplier = 0.5f;   // tanky from front
    public float sideArmorMultiplier = 1f;       // normal from sides
    public float rearArmorMultiplier = 1.5f;     // weak from behind

    // Events
    public System.Action<float, float> OnHealthChanged;  // current, max
    public System.Action OnDeath;

    private bool isDead = false;

    private void Awake()
    {
        currentHP = maxHP;
    }

    /// <summary>Take damage with directional armor check.</summary>
    public void TakeDamage(float rawDamage, Vector2? hitDirection = null)
    {
        if (isDead) return;

        float zonalMultiplier = GetZonalMultiplier(hitDirection);
        float damage = CalculateDamage(rawDamage * zonalMultiplier);

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);

        OnHealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0f) Die();
    }

    private float CalculateDamage(float rawDamage)
    {
        // Per design doc: -flat armor, then -% armor, but always deal min 25%
        float afterFlat = Mathf.Max(0f, rawDamage - armorFlat);
        float afterPercent = afterFlat * (1f - armorPercent);

        // Minimum damage guarantee
        float minDamage = rawDamage * minDamagePercent;

        return Mathf.Max(afterPercent, minDamage);
    }

    private float GetZonalMultiplier(Vector2? hitDirection)
    {
        if (hitDirection == null) return sideArmorMultiplier;

        // Check angle between hit direction and this object's forward (right in Unity 2D)
        float angle = Vector2.Angle(transform.right, hitDirection.Value);

        if (angle < 60f) return frontArmorMultiplier;       // front arc
        if (angle > 120f) return rearArmorMultiplier;       // rear arc
        return sideArmorMultiplier;                          // sides
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void SetMaxHP(float newMax, bool healToFull = false)
    {
        maxHP = newMax;
        if (healToFull) currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke();
        // Don't destroy here — let a DeathHandler component decide what to do
        // (enemy drops loot, player triggers game over, etc.)
    }

    public float HPPercent => currentHP / maxHP;
    public bool IsDead => isDead;
}