using UnityEngine;

public class Health : MonoBehaviour
{
    private float currentHealth;
    private float maxHealth;
    public float armor = 10f;
    
    void Start()
    {
        maxHealth = GetComponent<HullConfig>()?.maxHealth ?? 100f;
        currentHealth = maxHealth;
    }
    
    public void SetMaxHealth(float newMax)
    {
        maxHealth = newMax;
        currentHealth = newMax;
    }
    
    public void TakeDamage(float damage)
    {
        // Armor reduces damage by flat amount first, then percentage
        float reducedDamage = Mathf.Max(damage - armor, damage * 0.3f); // Minimum 30% damage
        currentHealth -= reducedDamage;
        
        if (currentHealth <= 0)
            Die();
    }
    
    void Die()
    {
        Destroy(gameObject);
    }
    
    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
}